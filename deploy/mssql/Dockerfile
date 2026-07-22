# Dockerfile cho service DATABASE (SQL Server) trên Railway — TÁCH BIỆT với Dockerfile
# của backend ở root repo. Tạo 1 Railway service riêng, set Root Directory = deploy/mssql
#
# Vì sao cần file này thay vì deploy thẳng image mcr.microsoft.com/mssql/server:
# Image 2022 mặc định chạy bằng user non-root `mssql`, không có quyền tạo thư mục
# hệ thống `/.system` ở gốc `/` -> crash loop "Access Denied". Ép USER root để
# SQL Server ghi được cả `/.system` lẫn volume dữ liệu.
FROM mcr.microsoft.com/mssql/server:2022-latest

# Chạy như root: cấp quyền tạo /.system và ghi vào volume mount tại /var/opt/mssql.
USER root

# ACCEPT_EULA bắt buộc. MSSQL_SA_PASSWORD KHÔNG hardcode ở đây — set qua biến env
# của Railway (Settings -> Variables) để khớp với connection string của backend.
ENV ACCEPT_EULA=Y
ENV MSSQL_PID=Developer

EXPOSE 1433
