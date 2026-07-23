#!/bin/bash
# Entrypoint cho SQL Server trên Railway.
# Chạy bằng user mssql (UID 10001), dùng sudo (đã cấp trong Dockerfile) để sửa quyền
# /.system và volume /var/opt/mssql TẠI RUNTIME — build-time chown vô dụng vì Railway
# mount volume (root-owned) đè lên sau khi build.

echo "Configuring runtime permissions for SQL Server..."

# Tạo sẵn thư mục hệ thống /.system (ở gốc /) và các thư mục dữ liệu trên volume.
sudo /usr/bin/mkdir -p /.system \
    /var/opt/mssql/data \
    /var/opt/mssql/log \
    /var/opt/mssql/secrets \
    /var/opt/mssql/backup

# Trả quyền sở hữu về user mssql (10001) group root (0), quyền 770.
sudo /usr/bin/chown -R 10001:0 /.system /var/opt/mssql
sudo /usr/bin/chmod -R 770 /.system /var/opt/mssql

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

# Ghim CPU affinity trước khi start: SQLPAL thấy 32 CPU của host Railway nên tạo 32
# scheduler + hàng trăm worker thread, vượt quota thật của container -> pthread_create
# trả EAGAIN (errno 11) -> fatal "Stack Overflow". taskset ép SQLOS chỉ thấy N core.
cpu_count=${TSMS_CPU_COUNT:-2}
cpu_mask="0-$((cpu_count - 1))"
echo "Pinning sqlservr to CPU ${cpu_mask} (host reports $(nproc) CPUs)."

# Chạy sqlservr bằng chính user mssql (giữ PR_SET_DUMPABLE), forward signal qua trap ở trên.
taskset -c "$cpu_mask" /opt/mssql/bin/sqlservr &
pid=$!
wait "$pid"
