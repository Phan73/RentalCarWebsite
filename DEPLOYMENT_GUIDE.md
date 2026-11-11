# RentalCarWebsite Deployment & Operations Guide

This guide provides instructions for building, testing, configuring, and deploying the modernised **RentalCarWebsite** platform in development, staging, and production environments.

---

## 1. System Requirements & Prerequisites

- **SDK Runtime:** .NET 8.0 SDK or later (`dotnet --version` >= 8.0.100).
- **Supported Operating Systems:** Windows 10/11, Windows Server 2019+, Ubuntu 22.04 LTS+, macOS 13+ (Ventura).
- **Database Engine Options:**
  - **Development / Offline:** SQLite 3 (included automatically, zero installation required).
  - **Production / Enterprise:** Microsoft SQL Server 2019+, Azure SQL Database, or AWS RDS SQL Server.
- **Reverse Proxy / Web Server (Production):** Nginx, Apache, or Microsoft IIS.
- **SSL / TLS Certificate:** Required for production HTTPS.

---

## 2. Quickstart Local Development

### Step 1: Clone and Checkout Modernisation Branch
```bash
git clone https://github.com/Phan73/RentalCarWebsite.git
cd RentalCarWebsite
git checkout feature/professional-modernisation
```

### Step 2: Build Solution
```bash
dotnet build ProjectPartB.sln
```

### Step 3: Run Automated Test Suite
Verify that all 30 automated tests pass before launching:
```bash
dotnet test ProjectPartB.Tests/ProjectPartB.Tests.csproj
```

### Step 4: Run Application
```bash
dotnet run --project ProjectPartB/ProjectPartB.csproj
```
The application will launch and bind to `https://localhost:7082` (or `http://localhost:5082`).

### Step 5: Automatic Database Migration & Seeding
On first startup in `Development` mode, `DatabaseSeeder.SeedAsync` executes automatically:
- Creates the local SQLite database file `rentalcar.db` (if it does not exist).
- Seeds standard Identity roles (`Administrator`, `Manager`, `Staff`, `Customer`).
- Seeds 5 New Zealand branch depot locations (Auckland Airport, CBD, Wellington, Christchurch, Queenstown).
- Seeds vehicle categories and a realistic 8-car fleet with imagery and specifications.
- Seeds 2 default administrative accounts:

| Role | Username / Email | Default Password |
| :--- | :--- | :--- |
| **Administrator** | `administrator@example.com` | `Password@123` |
| **Fleet Manager** | `manager@example.com` | `Password@123` |

> [!IMPORTANT]
> Change the default passwords immediately upon deploying to any staging or production environment.

---

## 3. Configuration & Secrets Management

Configuration is handled through standard ASP.NET Core hierarchical providers.

### Configuration Precedence
1. Environment Variables (Highest priority)
2. Command-Line Arguments
3. `appsettings.Production.json` / `appsettings.Development.json`
4. `appsettings.json` (Lowest priority base configuration)

### Key Configuration Variables

| Configuration Key | Environment Variable Syntax | Description | Example / Default |
| :--- | :--- | :--- | :--- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Database connection string | `Data Source=rentalcar.db` (SQLite) or `Server=db.example.com;Database=RentalCarDb;...` |
| `Authentication:Google:ClientId` | `Authentication__Google__ClientId` | Google OAuth Client ID | `your-google-client-id.apps.googleusercontent.com` |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` | Google OAuth Client Secret | `your-google-client-secret` |
| `EmailSettings:SmtpServer` | `EmailSettings__SmtpServer` | Outbound SMTP host | `smtp.sendgrid.net` |
| `EmailSettings:SmtpPort` | `EmailSettings__SmtpPort` | Outbound SMTP port | `587` |
| `EmailSettings:SenderEmail` | `EmailSettings__SenderEmail` | Transactional email sender | `reservations@apexrentals.co.nz` |

> [!WARNING]
> Never commit actual passwords, secrets, or API keys into `appsettings.json`. In production, inject them via secure system environment variables or Azure Key Vault / AWS Secrets Manager.

---

## 4. Database Setup

### Option A: Local SQLite (Default for Development & Testing)
In `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=rentalcar.db"
  }
}
```
No external database installation is required.

### Option B: Microsoft SQL Server (Production)
In `appsettings.Production.json` or via environment variables:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:sql-server.domain.com,1433;Initial Catalog=RentalCarProduction;Persist Security Info=False;User ID=RentalAppUser;Password=StrongProductionPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

To apply Entity Framework Core migrations to an external SQL Server:
```bash
dotnet ef database update --project ProjectPartB --context CustomIdentityContext
dotnet ef database update --project ProjectPartB --context FS23_Group4_ProjectContext
```

---

## 5. Production Deployment Architectures

### Architecture 1: Vercel Container Runtime (Active Production)

The application is live on Vercel using Vercel's OCI container runtime (`framework: "container"`):
- **Live Production URL:** [https://rentalcarwebsite.vercel.app](https://rentalcarwebsite.vercel.app)
- **Deployment Inspector:** [https://vercel.com/zedkilltalon-2072/rentalcarwebsite](https://vercel.com/zedkilltalon-2072/rentalcarwebsite)

#### Setup & Deployment:
1. `Dockerfile.vercel` & `Dockerfile` at the repository root build the .NET 8.0 application and run the optimized ASP.NET Core runtime.
2. `vercel.json` configures `"framework": "container"`.
3. `Program.cs` listens on the dynamic `$PORT` environment variable injected by Vercel.
4. Deploy or update with:
```bash
npx vercel deploy --prod --yes
```

### Architecture 2: Standard Docker Containerization

Create the following `Dockerfile` in the root of the repository:

```dockerfile
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProjectPartB/ProjectPartB.csproj", "ProjectPartB/"]
RUN dotnet restore "ProjectPartB/ProjectPartB.csproj"
COPY . .
WORKDIR "/src/ProjectPartB"
RUN dotnet build "ProjectPartB.csproj" -c Release -o /app/build
RUN dotnet publish "ProjectPartB.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProjectPartB.dll"]
```

Build and run with Docker:
```bash
docker build -t rentalcarwebsite:latest .
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/rentalcar.db" \
  -v $(pwd)/data:/data \
  --name rentalcar-app \
  rentalcarwebsite:latest
```

---

### Architecture 2: Linux (Ubuntu 22.04 LTS) with Systemd + Nginx

#### 1. Publish Application
```bash
dotnet publish ProjectPartB/ProjectPartB.csproj -c Release -o /var/www/rentalcar
chown -R www-data:www-data /var/www/rentalcar
```

#### 2. Configure Systemd Service (`/etc/systemd/system/rentalcar.service`)
```ini
[Unit]
Description=Apex Rental Car ASP.NET Core Application
After=network.target

[Service]
WorkingDirectory=/var/www/rentalcar
ExecStart=/usr/bin/dotnet /var/www/rentalcar/ProjectPartB.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=rentalcar-app
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ConnectionStrings__DefaultConnection="Server=localhost;Database=RentalCarDb;User Id=sa;Password=YourPassword;"

[Install]
WantedBy=multi-user.target
```

Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable rentalcar.service
sudo systemctl start rentalcar.service
```

#### 3. Configure Nginx Reverse Proxy (`/etc/nginx/sites-available/rentalcar`)
```nginx
server {
    listen 80;
    server_name rentals.yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name rentals.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/rentals.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/rentals.yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```
Enable the site and reload Nginx:
```bash
sudo ln -s /etc/nginx/sites-available/rentalcar /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## 6. Pre-Production Verification Checklist

- [ ] Run `dotnet test ProjectPartB.Tests/ProjectPartB.Tests.csproj` and verify all 30 tests pass.
- [ ] Confirm no secrets or hardcoded passwords exist in git status.
- [ ] Configure `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Set complex passwords for `administrator@example.com` and `manager@example.com`.
- [ ] Enable TLS 1.3 / SSL on the reverse proxy with valid certificate.
- [ ] Verify that HTTP redirects automatically to HTTPS.
- [ ] Test booking creation end-to-end to verify transaction commits and email dispatch.
- [ ] Test self-service booking cancellation to verify fleet availability restoration.
- [ ] Verify database daily backup schedule.

---
*Deployment guide verified for .NET 8.0 LTS production environments.*
