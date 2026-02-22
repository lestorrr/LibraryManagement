FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files
COPY ["LibraryManagement.sln", "."]
COPY ["src/LibraryManagement.Domain/LibraryManagement.Domain.csproj", "src/LibraryManagement.Domain/"]
COPY ["src/LibraryManagement.Application/LibraryManagement.Application.csproj", "src/LibraryManagement.Application/"]
COPY ["src/LibraryManagement.Infrastructure/LibraryManagement.Infrastructure.csproj", "src/LibraryManagement.Infrastructure/"]
COPY ["src/LibraryManagement.API/LibraryManagement.API.csproj", "src/LibraryManagement.API/"]

# Restore dependencies
RUN dotnet restore "LibraryManagement.sln"

# Copy all source code
COPY src/. ./src/

# Build and publish the API project
WORKDIR "/src/src/LibraryManagement.API"
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy published files
COPY --from=build /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "LibraryManagement.API.dll"]
