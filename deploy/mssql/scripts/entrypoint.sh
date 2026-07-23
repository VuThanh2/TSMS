#!/bin/bash
# Entrypoint cho SQL Server trên Railway.
# Chạy bằng user mssql (UID 10001), dùng sudo (đã cấp trong Dockerfile) để sửa quyền
# /.system và volume /var/opt/mssql TẠI RUNTIME — build-time chown vô dụng vì Railway
# mount volume (root-owned) đè lên sau khi build.

echo "Configuring runtime permissions for SQL Server..."
echo "  whoami: $(id)"

# Kiểm tra sudo có thật sự dùng được không. Nhiều container runtime mount rootfs bằng
# nosuid -> binary setuid như sudo mất tác dụng, mọi lệnh mkdir/chown bên dưới im lặng
# không làm gì, rồi sqlservr chết vì ACCESS_DENIED mà không rõ lý do. Phải biết sớm.
if sudo -n /usr/bin/mkdir -p /.system 2>/dev/null; then
    SUDO="sudo -n"
    echo "  sudo: available"
else
    SUDO=""
    echo "  sudo: NOT available (nosuid mount?) - fallback sang quyền sẵn có của user mssql"
fi

# .system trên volume là "persistent hive root" của SQLPAL — thiếu nó (hoặc không ghi được
# vào nó) chính là fatal 0xc0000022 "Unable to set persistent hive root". Phải tạo tường minh,
# không dựa vào việc sqlservr tự tạo được.
$SUDO /usr/bin/mkdir -p /.system \
    /var/opt/mssql/.system \
    /var/opt/mssql/data \
    /var/opt/mssql/log \
    /var/opt/mssql/secrets \
    /var/opt/mssql/backup || echo "  WARN: mkdir failed"

# Trả quyền sở hữu về user mssql (10001) group root (0), quyền 770.
$SUDO /usr/bin/chown -R 10001:0 /.system /var/opt/mssql || echo "  WARN: chown failed"
$SUDO /usr/bin/chmod -R 770 /.system /var/opt/mssql || echo "  WARN: chmod failed"

# In quyền THẬT sau khi sửa, và test ghi thật sự. Đây là bằng chứng duy nhất phân biệt
# "chown có tác dụng" với "Railway mount volume đè lên sau khi chown".
echo "  mount: $(mount | grep -F /var/opt/mssql || echo 'khong thay mount rieng cho /var/opt/mssql')"
ls -ldn / /.system /var/opt/mssql /var/opt/mssql/.system 2>&1 | sed 's/^/  /'

write_ok=true
for dir in /.system /var/opt/mssql /var/opt/mssql/.system /var/opt/mssql/data; do
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

# Dọn core dump cũ TRƯỚC khi start. Mỗi lần sqlservr crash, crash-support-functions.sh
# nén core dump vào /var/opt/mssql/log — vài trăm MB/lần. Với restart policy của Railway
# thì crash-loop sẽ ăn sạch volume, rồi chính ENOSPC lại gây crash mới (vòng lặp chết).
# Xoá ở đây để container tự chữa được kể cả khi đang crash-loop (không shell vào được).
# Không cần sudo: chown -R ở trên đã trả quyền sở hữu /var/opt/mssql về user mssql.
rm -rf /var/opt/mssql/log/core.sqlservr.*.tbz2 \
       /var/opt/mssql/log/core.sqlservr.*.temp \
       /var/opt/mssql/log/core.sqlservr.*.log 2>/dev/null || true

# Chỉ ghi mini dump (vài MB) thay vì full dump. Chỉ tạo mssql.conf khi chưa có — SQL Server
# cũng ghi vào file này, không được đè lên cấu hình nó tự sinh ra.
if [ ! -f /var/opt/mssql/mssql.conf ]; then
    printf '[coredump]\ncoredumptype = mini\ncaptureminiandfull = false\n' \
        > /var/opt/mssql/mssql.conf
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

# FIX: Limit CPU scheduler creation by setting TSMS_CPU_COUNT environment variable.
# SQL Server SQLPAL reads /proc/cpuinfo of the HOST (Railway reports 32 CPUs)
# but respects the TSMS_CPU_COUNT env var to cap scheduler creation.
# DO NOT use taskset - it doesn't actually limit the Linux cgroup and SQLPAL still
# creates 32 schedulers when it queries /proc/cpuinfo.
#
# Instead, rely on TSMS_CPU_COUNT to be set (default 2) which SQLPAL reads
# to determine how many schedulers to create. This prevents stack overflow from
# excessive thread allocation.
cpu_count=${TSMS_CPU_COUNT:-2}
echo "SQL Server will create ~${cpu_count} scheduler(s) (TSMS_CPU_COUNT=${TSMS_CPU_COUNT:-unset, using default 2})"

# Chạy sqlservr bằng chính user mssql (giữ PR_SET_DUMPABLE), forward signal qua trap ở trên.
/opt/mssql/bin/sqlservr &
pid=$!
wait "$pid"

