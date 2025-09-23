# Use the official .NET 9 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files and restore dependencies
COPY ["ModelComparisonStudio/ModelComparisonStudio.csproj", "ModelComparisonStudio/"]
COPY ["ModelComparisonStudio.Core/ModelComparisonStudio.Core.csproj", "ModelComparisonStudio.Core/"]
COPY ["ModelComparisonStudio.Application/ModelComparisonStudio.Application.csproj", "ModelComparisonStudio.Application/"]
COPY ["ModelComparisonStudio.Infrastructure/ModelComparisonStudio.Infrastructure.csproj", "ModelComparisonStudio.Infrastructure/"]
COPY ["ModelComparisonStudio.ServiceDefaults/ModelComparisonStudio.ServiceDefaults.csproj", "ModelComparisonStudio.ServiceDefaults/"]
RUN dotnet restore "ModelComparisonStudio/ModelComparisonStudio.csproj"

# Copy the entire source code
COPY . .

# Build and publish the application
WORKDIR "/src/ModelComparisonStudio"
RUN dotnet build "ModelComparisonStudio.csproj" -c Release -o /app/build
RUN dotnet publish "ModelComparisonStudio.csproj" -c Release -o /app/publish

# Use the official .NET 9 ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose port 8080
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "ModelComparisonStudio.dll"]