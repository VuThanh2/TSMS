# Dockerfile cho service DATABASE (SQL Server) trên Railway — TÁCH BIỆT với Dockerfile
# backend ở root repo. Tạo 1 Railway service riêng, set Root Directory = deploy/mssql
#
# Vì sao tag :2022-CU23-ubuntu-20.04 (không dùng :2022-latest):
# :latest dựa trên Ubuntu 22.04, dính bug sqlpal IO fallback trên storage Railway.
FROM mcr.microsoft.com/mssql/server:2022-CU23-ubuntu-20.04

# Cài sudo + cấp cho user mssql (UID 10001) quyền chạy KHÔNG mật khẩu đúng 3 lệnh
# mkdir/chown/chmod. Cần vì:
#   1. SQL Server phải tạo thư mục hệ thống /.system ở gốc / -> user mssql không có quyền.
#   2. Volume Railway mount vào /var/opt/mssql thuộc root -> user mssql không ghi được.
# KHÔNG chạy cả tiến trình bằng root (mất cờ PR_SET_DUMPABLE -> hỏng quản lý bộ nhớ SQLOS).
# Chỉ sudo đúng 3 lệnh sửa quyền lúc runtime, rồi chạy sqlservr bằng chính user mssql.
USER root
RUN apt-get update \
 && apt-get install -y --no-install-recommends sudo \
 && rm -rf /var/lib/apt/lists/* \
 && echo 'mssql ALL=(ALL) NOPASSWD: /usr/bin/mkdir, /usr/bin/chown, /usr/bin/chmod' > /etc/sudoers.d/mssql \
 && chmod 0440 /etc/sudoers.d/mssql

COPY scripts/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# ACCEPT_EULA bắt buộc. MSSQL_SA_PASSWORD set qua Variables của Railway (không hardcode).
# MSSQL_MEMORY_LIMIT_MB chặn OOM-kill (SQL Server mặc định ăn ~80% RAM). Phải để THẤP HƠN
# RAM container ~500MB để chừa cho OS Linux. 1024 hợp cho container 2GB.
#
# FIX: Stack Overflow là do SQLPAL đọc /proc/cpuinfo của HOST (32 CPUs) chứ không đọc cgroup
# limit -> tạo 32 schedulers + hàng trăm worker threads, mỗi thread pre-allocate stack.
# Cách FIX:
#   1. MSSQL_MEMORY_LIMIT_MB = 1024 (thay vì 1536) để giảm memory pressure
#   2. Entrypoint sẽ ghi mssql.conf với maxdop=2 để SQL Server tự giới hạn parallelism
#   3. Không dùng taskset vì nó không thực sự limit cgroup
#
# TSMS_CPU_COUNT=2: Gợi ý cho entrypoint script về số CPUs cần dùng.
ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer \
    MSSQL_MEMORY_LIMIT_MB=1024 \
    TSMS_CPU_COUNT=2

# Chạy non-root đúng cách image 2022 kỳ vọng. Railway KHÔNG được override USER.
USER 10001
EXPOSE 1433

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

