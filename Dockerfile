# Use the correct ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 8080

# Build stage - includes SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy everything (including .sln if applicable)
COPY . . 

# Restore dependencies
RUN dotnet restore "ProRota.csproj"

# Build application
RUN dotnet build "ProRota.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ProRota.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Final runtime image (must include .NET runtime!)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Ensure the app listens on the correct port for Cloud Run
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080

# Start the application
ENTRYPOINT ["dotnet", "ProRota.dll"]
