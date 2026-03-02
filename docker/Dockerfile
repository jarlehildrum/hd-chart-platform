# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY src/HdPlatform/*.csproj ./HdPlatform/
RUN dotnet restore ./HdPlatform/HdPlatform.csproj

# Copy source and build
COPY src/HdPlatform/ ./HdPlatform/
WORKDIR /src/HdPlatform
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install fonts for SkiaSharp text rendering
RUN apt-get update && apt-get install -y \
    fonts-dejavu-core \
    fontconfig \
    && rm -rf /var/lib/apt/lists/* \
    && fc-cache -fv

# Create data directory for API keys and usage
RUN mkdir -p /app/data && chmod 755 /app/data

# Copy published app
COPY --from=build /app/publish .

# Copy ephemeris data files (required for chart calculations)
COPY ephe/ ./ephe/
RUN chmod 644 /app/ephe/*

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV DataDirectory=/app/data

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5000/api/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "HdPlatform.dll"]