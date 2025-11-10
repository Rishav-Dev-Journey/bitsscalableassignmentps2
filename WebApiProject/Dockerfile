# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["WebApiProject.csproj", "./"]
RUN dotnet restore "WebApiProject.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "WebApiProject.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WebApiProject.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy the published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "WebApiProject.dll"]
