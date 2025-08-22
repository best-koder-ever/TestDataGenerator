FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy project definition files
# These paths are relative to the build context root (DatingApp/)
# The destinations are relative to WORKDIR /app
COPY TestDataGenerator/TestDataGenerator.csproj ./TestDataGenerator/
COPY auth-service/AuthService.csproj ./auth-service/
COPY matchmaking-service/MatchmakingService.csproj ./matchmaking-service/
COPY user-service/src/UserService/UserService.csproj ./user-service/src/UserService/
COPY dejting-yarp/src/dejting-yarp/dejting-yarp.csproj ./dejting-yarp/src/dejting-yarp/
COPY swipe-service/src/SwipeService/SwipeService.csproj ./swipe-service/src/SwipeService/
# If SharedKernel.csproj is a dependency and exists in a SharedKernel/ directory:
# COPY SharedKernel/SharedKernel.csproj ./SharedKernel/

# Restore dependencies for TestDataGenerator
# This will use the .csproj files copied above and resolve relative paths
RUN dotnet restore ./TestDataGenerator/TestDataGenerator.csproj

# Copy all source code for TestDataGenerator and its referenced projects
# These paths are also relative to the build context root
COPY TestDataGenerator/ ./TestDataGenerator/
COPY auth-service/ ./auth-service/
COPY matchmaking-service/ ./matchmaking-service/
COPY user-service/src/UserService/ ./user-service/src/UserService/
COPY dejting-yarp/src/dejting-yarp/ ./dejting-yarp/src/dejting-yarp/
COPY swipe-service/src/SwipeService/ ./swipe-service/src/SwipeService/
# If SharedKernel source code is needed:
# COPY SharedKernel/ ./SharedKernel/

# Publish TestDataGenerator
RUN dotnet publish ./TestDataGenerator/TestDataGenerator.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y bash iputils-ping curl && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/out .
# ENTRYPOINT ["dotnet", "TestDataGenerator.dll"]
# No ENTRYPOINT or CMD here; use docker-compose.yml to set the command
