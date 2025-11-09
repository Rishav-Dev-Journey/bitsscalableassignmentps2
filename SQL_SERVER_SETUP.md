# SQL Server Setup for macOS

## Option 1: Using Docker (Recommended for macOS)

### Install Docker Desktop for Mac

Download from: https://www.docker.com/products/docker-desktop

### Run SQL Server in Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sqlserver --hostname sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

### Update your connection string in appsettings.json:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=PaymentDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
}
```

### Apply migrations to create the database:

```bash
dotnet ef database update
```

## Option 2: Use Azure SQL Database

1. Create an Azure SQL Database
2. Get the connection string from Azure Portal
3. Update appsettings.json with your Azure SQL connection string
4. Run: `dotnet ef database update`

## Option 3: Use LocalDB (Windows only)

Not available on macOS

## Verify SQL Server is running:

```bash
docker ps
```

## Connect to SQL Server:

```bash
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd"
```

## Stop SQL Server:

```bash
docker stop sqlserver
```

## Start SQL Server:

```bash
docker start sqlserver
```

## Remove SQL Server container:

```bash
docker rm -f sqlserver
```
