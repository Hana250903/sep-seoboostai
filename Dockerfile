# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SEOBoostAI.API/SEOBoostAI.API.csproj", "SEOBoostAI.API/"]
COPY ["SEOBoostAI.Services/SEOBoostAI.Service.csproj", "SEOBoostAI.Services/"]
COPY ["SEOBoostAI.Repositories/SEOBoostAI.Repository.csproj", "SEOBoostAI.Repositories/"]
COPY ["SEP_SEOBoostAI.sln", "./"]

# Restore dependencies
RUN dotnet restore "SEP_SEOBoostAI.sln"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/SEOBoostAI.API"
RUN dotnet build "SEOBoostAI.API.csproj" -c Release -o /app/build
RUN dotnet publish "SEOBoostAI.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Set entry point
ENTRYPOINT ["dotnet", "SEOBoostAI.API.dll"]
