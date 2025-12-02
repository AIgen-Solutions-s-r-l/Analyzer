# ========================================
# Stage 1: Build
# ========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

# Copy solution and project files first for layer caching
COPY *.sln .
COPY src/AnalyzerCore.Domain/AnalyzerCore.Domain.csproj src/AnalyzerCore.Domain/
COPY src/AnalyzerCore.Application/AnalyzerCore.Application.csproj src/AnalyzerCore.Application/
COPY src/AnalyzerCore.Infrastructure/AnalyzerCore.Infrastructure.csproj src/AnalyzerCore.Infrastructure/
COPY src/AnalyzerCore.Api/AnalyzerCore.Api.csproj src/AnalyzerCore.Api/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build
WORKDIR /src/src/AnalyzerCore.Api
RUN dotnet build -c Release -o /app/build --no-restore

# ========================================
# Stage 2: Publish
# ========================================
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# ========================================
# Stage 3: Runtime
# ========================================
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS runtime

# Install required packages
RUN apk add --no-cache \
    icu-libs \
    tzdata

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs && \
    chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=UTC

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "AnalyzerCore.Api.dll"]
