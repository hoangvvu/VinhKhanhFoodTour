# Deployment Guide

## Pre-Deployment Checklist

- [ ] Update JWT SecretKey in appsettings.json to a strong value
- [ ] Update database connection string for production database
- [ ] Configure CORS to accept only trusted origins
- [ ] Enable HTTPS
- [ ] Set up SSL certificates
- [ ] Configure logging
- [ ] Set up backup strategy for database
- [ ] Create admin account(s)
- [ ] Test all endpoints
- [ ] Configure monitoring and alerting

## Environment Configuration

### Development (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "yourdomain.com",
  "ConnectionStrings": {
    "AdminConnection": "Server=<production-server>;Database=VinhKhanhFoodTourAdmin;User Id=<user>;Password=<password>;"
  },
  "JwtSettings": {
    "SecretKey": "<very-long-strong-secret-key-for-production>",
    "Issuer": "VinhKhanhFoodTourAdmin",
    "Audience": "VinhKhanhFoodTourAdminUsers",
    "ExpirationHours": "24"
  }
}
```

## IIS Deployment

### 1. Publish the Application
```bash
dotnet publish -c Release -o ./publish
```

### 2. Create IIS Application Pool
- Name: VinhKhanhFoodTourAdminPool
- .NET CLR version: No Managed Code
- Pipeline mode: Integrated

### 3. Create IIS Website
- Name: VinhKhanhFoodTourAdmin
- Physical path: C:\inetpub\wwwroot\VinhKhanhFoodTourAdmin
- Binding: https://yourdomain.com:443

### 4. Configure Application
- Copy published files to physical path
- Set application pool to VinhKhanhFoodTourAdminPool
- Configure SSL certificate

## Docker Deployment

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["VinhKhanhFoodTour.AdminWeb/VinhKhanhFoodTour.AdminWeb.csproj", "VinhKhanhFoodTour.AdminWeb/"]
RUN dotnet restore "VinhKhanhFoodTour.AdminWeb/VinhKhanhFoodTour.AdminWeb.csproj"
COPY . .
WORKDIR "/src/VinhKhanhFoodTour.AdminWeb"
RUN dotnet build "VinhKhanhFoodTour.AdminWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VinhKhanhFoodTour.AdminWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VinhKhanhFoodTour.AdminWeb.dll"]
```

### Docker Compose
```yaml
version: '3.8'
services:
  admin-web:
    build: .
    ports:
      - "5001:443"
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./certs:/app/certs
      - ./appsettings.Production.json:/app/appsettings.Production.json
    depends_on:
      - db

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: ${DB_PASSWORD}
      ACCEPT_EULA: Y
    ports:
      - "1433:1433"
    volumes:
      - dbdata:/var/opt/mssql

volumes:
  dbdata:
```

## Azure App Service Deployment

### 1. Create App Service
```bash
az appservice plan create \
  --name VinhKhanhFoodTourAdminPlan \
  --resource-group myResourceGroup \
  --sku B2

az webapp create \
  --resource-group myResourceGroup \
  --plan VinhKhanhFoodTourAdminPlan \
  --name VinhKhanhFoodTourAdmin
```

### 2. Configure App Service
- Set runtime stack to .NET 10
- Configure connection strings
- Configure application settings
- Enable logging

### 3. Deploy Application
```bash
dotnet publish -c Release
# Deploy the publish folder to Azure App Service
```

## Database Migration

### Before Deployment
```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Production Migration
```bash
dotnet ef database update --configuration Release
```

## SSL/TLS Configuration

### Generate Self-Signed Certificate (Development)
```bash
dotnet dev-certs https -ep %APPDATA%\ASP.NET\Https\aspnetcore.pfx -p <password>
```

### Install Certificate (Production)
- Obtain certificate from trusted CA
- Configure in Kestrel or IIS
- Update certificate path in configuration

## Monitoring and Logging

### Application Insights
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "<your-instrumentation-key>"
  }
}
```

### Logging Configuration
```json
{
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    },
    "Console": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

## Health Checks

### Add Health Check Endpoint
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AdminDbContext>();

app.MapHealthChecks("/health");
```

## Performance Optimization

### Response Caching
```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Compression
```csharp
builder.Services.AddResponseCompression();
app.UseResponseCompression();
```

### Database Connection Pooling
```json
{
  "ConnectionStrings": {
    "AdminConnection": "... Max Pool Size=100; ..."
  }
}
```

## Security Hardening

1. **HTTPS Only**
   ```csharp
   app.UseHttpsRedirection();
   app.UseHsts();
   ```

2. **Security Headers**
   ```csharp
   app.Use(async (context, next) =>
   {
       context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
       context.Response.Headers.Add("X-Frame-Options", "DENY");
       context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
       await next();
   });
   ```

3. **CORS Configuration**
   ```csharp
   options.AddPolicy("SecureCors", policy =>
   {
       policy.WithOrigins("https://yourdomain.com")
             .AllowAnyMethod()
             .AllowAnyHeader()
             .AllowCredentials();
   });
   ```

## Backup Strategy

1. **Database Backups**
   - Schedule daily backups
   - Store backups securely
   - Test restore procedures

2. **Application Files**
   - Backup source code
   - Version control all changes
   - Store backups in secure location

## Disaster Recovery

1. **Recovery Point Objective (RPO)**: 24 hours
2. **Recovery Time Objective (RTO)**: 4 hours
3. **Backup Retention**: 30 days
4. **Test recovery procedures monthly**

## Troubleshooting

### Application Won't Start
- Check logs in Application Event Viewer
- Verify database connection string
- Ensure SQL Server is running
- Check port availability

### High CPU Usage
- Monitor database queries
- Check for memory leaks
- Review application logs
- Scale up if needed

### Database Connection Issues
- Verify connection string
- Check SQL Server is running
- Verify authentication credentials
- Check firewall rules

### Performance Issues
- Enable Application Insights
- Monitor response times
- Check database performance
- Review logs for errors
