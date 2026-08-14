# Build context is repository root (../.. from compose file location).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Commerce.sln ./
COPY src/Commerce/ src/Commerce/

RUN dotnet restore src/Commerce/Host/Commerce.Host/Commerce.Host.csproj
RUN dotnet publish src/Commerce/Host/Commerce.Host/Commerce.Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish ./
COPY deploy/docker/scripts/entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

RUN mkdir -p App_Data/media App_Data/backups Plugins \
    && chown -R app:app /app

USER app

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=45s --retries=5 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "Commerce.Host.dll"]
