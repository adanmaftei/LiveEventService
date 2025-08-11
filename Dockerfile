# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy only the main application project files (exclude CDK project)
COPY src/LiveEventService.API/*.csproj ./src/LiveEventService.API/
COPY src/LiveEventService.Application/*.csproj ./src/LiveEventService.Application/
COPY src/LiveEventService.Core/*.csproj ./src/LiveEventService.Core/
COPY src/LiveEventService.Infrastructure/*.csproj ./src/LiveEventService.Infrastructure/

# Restore dependencies for API project (will restore all dependencies)
WORKDIR /app/src/LiveEventService.API
RUN dotnet restore

# Copy source code
WORKDIR /app
COPY src/LiveEventService.API/ ./src/LiveEventService.API/
COPY src/LiveEventService.Application/ ./src/LiveEventService.Application/
COPY src/LiveEventService.Core/ ./src/LiveEventService.Core/
COPY src/LiveEventService.Infrastructure/ ./src/LiveEventService.Infrastructure/

# Build and publish the application
RUN dotnet publish src/LiveEventService.API/LiveEventService.API.csproj -c Release -o /app/publish

# Use the official .NET 9 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "LiveEventService.API.dll"] 