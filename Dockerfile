# Dockerfile — build & chạy TSMS.Api (backend)
#
# Trên Railway: service Backend connect GitHub repo này, để Root Directory TRỐNG
# (mặc định) — Railway sẽ tự detect Dockerfile ở root. Build context là cả repo,
# nhưng .dockerignore loại bỏ client/ và tests/ nên không bị copy thừa vào image.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ src/

RUN dotnet restore src/Api/TSMS.Api/TSMS.Api.csproj
RUN dotnet publish src/Api/TSMS.Api/TSMS.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Bind rõ ràng cổng 8080 để Railway detect đúng. TLS do Railway terminate ở edge,
# container chỉ cần lắng nghe HTTP nội bộ. Nếu Railway cấp PORT khác, set biến
# ASPNETCORE_HTTP_PORTS ở dashboard để override.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TSMS.Api.dll"]