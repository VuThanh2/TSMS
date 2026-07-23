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
 && apt-get install -y --no-install-recommends sudo util-linux \
 && rm -rf /var/lib/apt/lists/* \
 && echo 'mssql ALL=(ALL) NOPASSWD: /usr/bin/mkdir, /usr/bin/chown, /usr/bin/chmod' > /etc/sudoers.d/mssql \
 && chmod 0440 /etc/sudoers.d/mssql

COPY scripts/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# ACCEPT_EULA bắt buộc. MSSQL_SA_PASSWORD set qua Variables của Railway (không hardcode).
#
# MSSQL_MEMORY_LIMIT_MB chặn OOM-kill (SQL Server mặc định ăn ~80% RAM). Phải để THẤP HƠN
# RAM container ~500MB để chừa cho OS Linux. 1024 hợp cho container 1.5-2GB; nếu cấp nhiều
# RAM hơn thì override qua Variables của Railway (không cần rebuild).
# Luôn dùng biến tính bằng MB, KHÔNG dùng memory.memorylimitpercent trong mssql.conf:
# SQLPAL đọc RAM của HOST (346 GB) nên 80% = 277 GB, và nó ghi đè biến này.
#
# TSMS_CPU_COUNT: SQLPAL đọc /proc/cpuinfo của HOST (32-48 CPU trên Railway) chứ không đọc
# cgroup limit -> tạo từng ấy scheduler + hàng trăm worker thread, mỗi thread pre-allocate
# stack -> chạm quota thật của container -> pthread_create trả EAGAIN (errno 11) -> fatal
# 0x6 "Stack Overflow". Entrypoint dùng taskset (sched_setaffinity) để ép SQLPAL xuống N core.
# Đã kiểm chứng bằng header crash: có taskset -> "Processors: 2"; gỡ ra -> "Processors: 48".
# maxdop KHÔNG thay thế được taskset — nó chỉ giới hạn parallelism của query, không giới hạn
# số scheduler tạo lúc khởi động (và cũng không phải key hợp lệ của mssql.conf).
ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer \
    MSSQL_MEMORY_LIMIT_MB=1024 \
    TSMS_CPU_COUNT=2

# Chạy non-root đúng cách image 2022 kỳ vọng. Railway KHÔNG được override USER.
USER 10001
EXPOSE 1433

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

