# Dockerfile for OGame Clone App (Blazor Server .NET 8)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "myapp.csproj"
RUN dotnet build "myapp.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "myapp.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "myapp.dll"]