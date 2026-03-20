# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for optimal layer caching.
# Restore runs before copying source so that package layers are cached
# as long as .csproj files are unchanged.
COPY ["src/MarketCore.Domain/MarketCore.Domain.csproj",          "src/MarketCore.Domain/"]
COPY ["src/MarketCore.Application/MarketCore.Application.csproj", "src/MarketCore.Application/"]
COPY ["src/MarketCore.Infrastructure/MarketCore.Infrastructure.csproj", "src/MarketCore.Infrastructure/"]
COPY ["src/MarketCore.Api/MarketCore.Api.csproj",                "src/MarketCore.Api/"]

RUN dotnet restore "src/MarketCore.Api/MarketCore.Api.csproj"

# Copy full source after restore to keep the restore layer cached.
COPY . .

RUN dotnet build "src/MarketCore.Api/MarketCore.Api.csproj" \
    -c Release \
    -o /app/build \
    /p:TreatWarningsAsErrors=false \
    /p:EnforceCodeStyleInBuild=false

# ── Stage 2: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "src/MarketCore.Api/MarketCore.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:TreatWarningsAsErrors=false \
    /p:EnforceCodeStyleInBuild=false

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user for security.
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Create log directory owned by appuser.
RUN mkdir -p logs

EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet MarketCore.Api.dll"]
