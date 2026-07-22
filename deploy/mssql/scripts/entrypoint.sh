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

echo "Permissions ready. Starting sqlservr..."

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

# Chạy sqlservr bằng chính user mssql (giữ PR_SET_DUMPABLE), forward signal qua trap ở trên.
/opt/mssql/bin/sqlservr &
pid=$!
wait "$pid"
