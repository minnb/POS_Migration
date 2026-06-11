# Stage 1: build bằng SDK image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy csproj trước để cache layer restore (build lại nhanh khi chỉ đổi code)
COPY src/POS.Common/POS.Common.csproj           src/POS.Common/
COPY src/POS.Infrastructure/POS.Infrastructure.csproj src/POS.Infrastructure/
COPY src/POS.Application/POS.Application.csproj src/POS.Application/
COPY src/POS.Api/POS.Api.csproj                 src/POS.Api/
RUN dotnet restore src/POS.Api/POS.Api.csproj
COPY src/ src/
RUN dotnet publish src/POS.Api/POS.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: runtime image gọn
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV TZ=Asia/Ho_Chi_Minh \
    ASPNETCORE_URLS=http://+:80

# curl chỉ dùng cho HEALTHCHECK (image aspnet không có sẵn curl/wget)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Non-root (Docker 20.10+ cho phép non-root bind port 80 trong container)
USER $APP_UID
EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "POS.Api.dll"]
