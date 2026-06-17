# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/POS.Common/POS.Common.csproj           src/POS.Common/
COPY src/POS.Infrastructure/POS.Infrastructure.csproj src/POS.Infrastructure/
COPY src/POS.Application/POS.Application.csproj src/POS.Application/
COPY src/POS.Web/POS.Web.csproj                 src/POS.Web/
RUN dotnet restore src/POS.Web/POS.Web.csproj
COPY src/ src/
RUN dotnet publish src/POS.Web/POS.Web.csproj -c Release -o /app/publish --no-restore

# Stage 2: runtime image gọn
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV TZ=Asia/Ho_Chi_Minh \
    ASPNETCORE_URLS=http://+:8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet

USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "POS.Web.dll"]
