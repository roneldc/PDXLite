# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY PDXLite/PDXLite.csproj PDXLite/
RUN dotnet restore PDXLite/PDXLite.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/PDXLite
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PDXLite.dll"]
