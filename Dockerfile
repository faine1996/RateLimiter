# STEP 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY RateLimiter.sln ./
COPY RateLimiter.Core/ ./RateLimiter.Core/
COPY RateLimiter.Demo/ ./RateLimiter.Demo/

# Restore and build the project
RUN dotnet restore
RUN dotnet publish RateLimiter.Demo/RateLimiter.Demo.csproj -c Release -o out

# STEP 2: Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "RateLimiter.Demo.dll"]
COPY RateLimiter.sln ./ 
