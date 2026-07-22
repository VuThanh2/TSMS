# Dockerfile cho service DATABASE (SQL Server) trên Railway — TÁCH BIỆT với Dockerfile
# backend ở root repo. Tạo 1 Railway service riêng, set Root Directory = deploy/mssql
#
# QUAN TRỌNG — vì sao KHÔNG dùng tag :2022-latest:
# Image :2022-latest dựa trên Ubuntu 22.04, dính bug sqlpal IO fallback khi chạy trên
# storage của Railway -> đúng lỗi "The system directory [/.system] could not be created
# ... Access denied" mà container bị crash-loop. Tag CU trên Ubuntu 20.04 KHÔNG dính bug này.
FROM mcr.microsoft.com/mssql/server:2022-CU23-ubuntu-20.04

# Tạo sẵn thư mục dữ liệu và cấp quyền cho user mssql (UID 10001, GID 0).
# SQL Server 2022 chạy non-root (user mssql) -> phải chown trước bằng root rồi mới trả lại.
USER root
RUN mkdir -p /var/opt/mssql/data /var/opt/mssql/log /var/opt/mssql/secrets /var/opt/mssql/backup \
 && chown -R 10001:0 /var/opt/mssql \
 && chmod -R 770 /var/opt/mssql

# ACCEPT_EULA bắt buộc. MSSQL_SA_PASSWORD set qua Variables của Railway (không hardcode).
# MSSQL_MEMORY_LIMIT_MB giới hạn RAM để container không bị Railway OOM-kill (SQL Server
# mặc định ăn tới 80% RAM). 2048 là mức tối thiểu an toàn cho SQL Server.
ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer \
    MSSQL_MEMORY_LIMIT_MB=2048

# Trả lại user non-root mssql (đúng cách image 2022 kỳ vọng). Railway KHÔNG được override USER.
USER 10001
EXPOSE 1433
