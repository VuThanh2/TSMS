#!/bin/bash
# Entrypoint cho SQL Server trên Railway.
# Chạy bằng user mssql (UID 10001), dùng sudo (đã cấp trong Dockerfile) để sửa quyền
# /.system và volume /var/opt/mssql TẠI RUNTIME — build-time chown vô dụng vì Railway
# mount volume (root-owned) đè lên sau khi build.

# Tắt core dump của kernel NGAY từ đầu. Mỗi lần sqlservr crash, bộ thu dump của Microsoft
# spam hàng trăm dòng "find: /proc/<pid>/... Permission denied" (Railway chặn ptrace nên
# nó luôn thất bại), chạm rate limit 500 logs/sec của Railway -> log chẩn đoán thật sự
# bị VỨT ("Messages dropped: 783"). Dump này dù sao cũng không thu được, bỏ hẳn cho sạch.
ulimit -c 0 2>/dev/null || true

echo "Configuring runtime permissions for SQL Server..."
echo "  whoami: $(id)"

# Kiểm tra sudo có thật sự dùng được không. Nhiều container runtime mount rootfs bằng
# nosuid -> binary setuid như sudo mất tác dụng, mọi lệnh mkdir/chown bên dưới im lặng
# không làm gì, rồi sqlservr chết vì ACCESS_DENIED mà không rõ lý do. Phải biết sớm.
if sudo -n /usr/bin/mkdir -p /var/opt/mssql/.system 2>/dev/null; then
    SUDO="sudo -n"
    echo "  sudo: available"
else
    SUDO=""
    echo "  sudo: NOT available (nosuid mount?) - fallback sang quyen san co cua user mssql"
fi

# 1. CHỈ tạo các thư mục thật trên Volume được mount của Railway
$SUDO /usr/bin/mkdir -p     /var/opt/mssql/.system     /var/opt/mssql/data     /var/opt/mssql/log     /var/opt/mssql/secrets     /var/opt/mssql/backup || echo "  WARN: mkdir failed"

# 2. Xóa /.system nếu nó đang là thư mục thật (xóa tàn dư để tránh lỗi 0xc0000022 persistent hive root)
$SUDO rm -rf /.system

# 3. Tạo Symlink trỏ từ /.system tới Volume vĩnh viễn
$SUDO ln -sfn /var/opt/mssql/.system /.system || echo "  WARN: symlink failed"

# FIX: Trả quyền sở hữu về user mssql (10001) và GID mssql (10001), không root (0).
# Process chạy là uid=10001(mssql) gid=10001(mssql), vậy nên group phải là 10001,
# KHÔNG phải 0 (root). Nếu chown về 10001:0 thì SQLPAL không thể ghi vào /.system
# vì gid của nó là 10001, không phải 0 -> EAGAIN "Resource temporarily unavailable".
$SUDO /usr/bin/chown -R 10001:10001 /var/opt/mssql || echo "  WARN: chown failed"
$SUDO /usr/bin/chmod -R 770 /var/opt/mssql || echo "  WARN: chmod failed"

# Đổi quyền sở hữu riêng cho Symlink (dùng cờ -h để chỉ sửa bản thân symlink)
$SUDO /usr/bin/chown -h 10001:10001 /.system || echo "  WARN: chown symlink failed"

# In quyền THẬT sau khi sửa, và test ghi thật sự. Đây là bằng chứng duy nhất phân biệt
# "chown có tác dụng" với "Railway mount volume đè lên sau khi chown".
echo "  mount: $(mount | grep -F /var/opt/mssql || echo 'khong thay mount rieng cho /var/opt/mssql')"
ls -ldn / /.system /var/opt/mssql /var/opt/mssql/.system 2>&1 | sed 's/^/  /'

write_ok=true
# Chỉ test ghi trên volume, không cần test /.system nữa vì nó là symlink
for dir in /var/opt/mssql /var/opt/mssql/.system /var/opt/mssql/data; do
    if touch "$dir/.write-test" 2>/dev/null; then
        rm -f "$dir/.write-test"
    else
        echo "  FATAL: user $(id -u) KHONG ghi duoc vao $dir"
        write_ok=false
    fi
done

if [ "$write_ok" != true ]; then
    echo "FATAL: quyen ghi chua du -> sqlservr se chet voi 0xc0000022. Dung tai day."
    exit 1
fi

echo "Permissions ready."

# Dọn core dump cũ. Trước khi có ulimit -c 0 ở trên, mỗi lần crash ghi vài trăm MB vào
# /var/opt/mssql/log; crash-loop từng ăn sạch volume rồi chính ENOSPC lại gây crash mới.
# Giữ lại bước dọn để container tự chữa kể cả khi đang crash-loop (không shell vào được).
rm -rf /var/opt/mssql/log/core.sqlservr.* 2>/dev/null || true

# mssql.conf: CHỈ những cấu hình RUNTIME, KHÔNG query-level.
# KHÔNG đặt memory.memorylimitpercent — SQLPAL đọc RAM của HOST (346 GB), 80% = 277 GB,
# và nó ghi đè MSSQL_MEMORY_LIMIT_MB. Chỉ dùng biến môi trường tính bằng MB.
# KHÔNG đặt network.tlscert/tlskey — trỏ tới file không tồn tại thì SQL Server fail startup.
if [ ! -f /var/opt/mssql/mssql.conf ]; then
    cat > /var/opt/mssql/mssql.conf <<'EOF_CONF'
[coredump]
coredumptype = mini
captureminiandfull = false

[fileengine]
sectorsize = 4096
EOF_CONF
    echo "Created mssql.conf with forced 4KB sector size"
fi

df -h /var/opt/mssql | tail -1
echo "Starting sqlservr..."

# Tắt SQL Server an toàn khi Railway gửi SIGTERM (tránh hỏng database lúc redeploy/restart).
pid=0
graceful_shutdown() {
    if [ "$pid" -ne 0 ]; then
        kill -s TERM "$pid"
        wait "$pid"
    fi
    exit 0
}
trap graceful_shutdown SIGINT SIGTERM

# FIX: Use taskset to pin SQL Server to N CPUs.
# SQLPAL đọc CPU khả dụng qua sched_getaffinity, và nó đọc /proc/cpuinfo của HOST 
# chứ không đọc cgroup limit -> thấy 32-48 CPU, tạo từng ấy scheduler + hàng trăm 
# worker thread, vượt quota thật của container -> pthread_create trả EAGAIN (errno 11) 
# -> fatal 0x6 "Stack Overflow".
#
# taskset gọi sched_setaffinity, đổi affinity mask THẬT của tiến trình nên SQLPAL 
# bị ép xuống đúng N core. Đã kiểm chứng bằng header crash: 
# - Có taskset -> "Processors: 2" và HOẠT ĐỘNG BÌNH THƯỜNG
# - Gỡ taskset -> "Processors: 48" + Stack Overflow
# 
# TSMS_CPU_COUNT=2 (env var từ Dockerfile) -> taskset to CPUs 0-1
cpu_count=${TSMS_CPU_COUNT:-2}
cpu_mask="0-$((cpu_count - 1))"

# Dừng hẳn nếu thiếu taskset (gói util-linux trong Dockerfile). Chạy tiếp mà không ghim
# affinity thì chắc chắn crash 0x6, và crash đó rất khó lần ngược về nguyên nhân.
if ! command -v taskset >/dev/null 2>&1; then
    echo "FATAL: khong tim thay taskset. Kiem tra 'util-linux' da duoc cai trong Dockerfile."
    exit 1
fi

echo "Pinning sqlservr to CPUs ${cpu_mask} (TSMS_CPU_COUNT=${TSMS_CPU_COUNT}). Host reports $(nproc) CPUs."

# Chạy sqlservr bằng chính user mssql (giữ PR_SET_DUMPABLE), forward signal qua trap ở trên.
# taskset sẽ ép SQLPAL chỉ thấy CPUs 0-$((cpu_count-1)).
taskset -c "$cpu_mask" /opt/mssql/bin/sqlservr &
pid=$!
wait "$pid"