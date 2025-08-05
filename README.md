# NphiesBridge - Healthcare ICD Code Mapping System

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![MIT License](https://img.shields.io/badge/License-MIT-green)

## Overview

NphiesBridge is a comprehensive healthcare middleware system designed to facilitate the mapping of hospital-specific ICD codes to NPHIES (National Platform for Health Information Exchange Services) standard codes. The system provides automated AI-powered fuzzy matching capabilities along with manual mapping interfaces for healthcare providers in Saudi Arabia.

## Architecture

The solution follows a clean architecture pattern with the following projects:

```
NphiesBridge/
├── NphiesBridge.API/                 # RESTful API (.NET 8)
├── NphiesBridge.Web/                 # Razor Pages Web Application
├── NphiesBridge.AdminPortal/         # Administrative Portal
├── NphiesBridge.Core/                # Domain Entities & Interfaces
├── NphiesBridge.Infrastructure/      # Data Access & Services
├── NphiesBridge.Shared/             # DTOs & Validators
└── NphiesBridge.Tests/              # Unit & Integration Tests
```

## Technology Stack

- **Framework**: .NET 8.0
- **Web Framework**: ASP.NET Core (Razor Pages)
- **API**: RESTful Web API with Swagger/OpenAPI
- **Database**: SQL Server with Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens
- **Fuzzy Matching**: FuzzierSharp library
- **Excel Processing**: ClosedXML
- **Logging**: Serilog
- **Testing**: xUnit, FluentAssertions, Moq
- **Validation**: FluentValidation
- **Caching**: In-Memory Caching

## Key Features

### 🔄 ICD Code Mapping
- **Bulk Excel Upload**: Support for Excel templates with hospital ICD codes
- **AI-Powered Matching**: Fuzzy string matching with confidence scoring
- **Manual Review Interface**: Web-based mapping interface for validation
- **Progress Tracking**: Real-time mapping session statistics

### 🤖 AI Fuzzy Matching Engine
- **Multiple Algorithms**: Levenshtein distance, keyword overlap, medical term recognition
- **Confidence Scoring**: Weighted similarity scoring (40% Levenshtein + 35% keywords + 15% medical terms + 10% length)
- **Medical Synonyms**: Built-in medical terminology mappings
- **Performance Optimized**: Parallel processing with memory caching

### 👥 Multi-Tenant Support
- **Health Provider Management**: Organization-based data isolation
- **Role-Based Access**: Admin and Provider user roles
- **User Management**: ASP.NET Core Identity integration

### 📊 Reporting & Export
- **Session Management**: Track mapping sessions and progress
- **Excel Export**: Export completed mappings to Excel format
- **Statistics Dashboard**: Completion percentages and mapping metrics

## Database Schema

### Core Entities

```csharp
// Health Provider
public class HealthProvider
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string LicenseNumber { get; set; }
    public string ContactPerson { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ICD Code Mapping
public class IcdCodeMapping : BaseEntity
{
    public Guid HospitalCodeId { get; set; }
    public string NphiesIcdCode { get; set; }
    public Guid MappedByUserId { get; set; }
    public DateTime MappedAt { get; set; }
    public bool IsAiSuggested { get; set; }
    public int? ConfidenceScore { get; set; }
}

// Hospital ICD Code
public class HospitalIcdCode : BaseEntity
{
    public Guid HealthProviderId { get; set; }
    public string HospitalCode { get; set; }
    public string DiagnosisName { get; set; }
    public string DiagnosisDescription { get; set; }
    public string SuggestedIcd10Am { get; set; }
    public bool IsMapped { get; set; }
    public Guid? MappingSessionId { get; set; }
}
```

## Installation & Setup

### Prerequisites
- .NET 8.0 SDK
- SQL Server 2019+ (or SQL Server Express)
- Visual Studio 2022 or VS Code
- IIS (for production deployment)

### Development Setup

#### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/NphiesBridge.git
cd NphiesBridge
```

#### 2. Database Configuration
Update connection strings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NphiesBridgeDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-here-make-it-at-least-32-characters-long",
    "Issuer": "NphiesBridge",
    "Audience": "NphiesBridge",
    "ExpirationMinutes": 60
  }
}
```

#### 3. Database Migration
```bash
# Navigate to API project
cd NphiesBridge.API

# Run migrations
dotnet ef database update
```

#### 4. Install Dependencies
```bash
# Restore NuGet packages
dotnet restore
```

#### 5. Run the Application
```bash
# Start API
cd NphiesBridge.API
dotnet run

# Start Web Application (separate terminal)
cd NphiesBridge.Web
dotnet run
```

## Key Dependencies

### API Project (NphiesBridge.API)
```xml
<PackageReference Include="ClosedXML" Version="0.102.2" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.1" />
<PackageReference Include="FuzzierSharp" Version="3.0.1" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.17" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.17" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
```

### Infrastructure Project (NphiesBridge.Infrastructure)
```xml
<PackageReference Include="FuzzierSharp" Version="3.0.1" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.17" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
```

## API Endpoints

### ICD Mapping Controller
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/IcdMapping/ai-suggestion` | Get AI suggestions for diagnosis |
| POST | `/api/IcdMapping/save-mapping` | Save approved mapping |
| POST | `/api/IcdMapping/create-session` | Create mapping session |
| GET | `/api/IcdMapping/session/{id}` | Get session details |
| POST | `/api/IcdMapping/bulk-match` | Process bulk AI matching |
| POST | `/api/IcdMapping/export` | Export mappings to Excel |
| GET | `/api/IcdMapping/statistics/{id}` | Get mapping statistics |

### NPHIES Codes Controller
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/NphiesCodes/search` | Search NPHIES codes |
| GET | `/api/NphiesCodes/{id}` | Get specific code details |

### Authentication Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Auth/login` | User authentication |
| POST | `/api/Auth/register` | User registration |
| POST | `/api/Auth/refresh` | Token refresh |

## Configuration

### Logging Configuration (Serilog)
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/general.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.File("Logs/errors.txt", rollingInterval: RollingInterval.Day, 
                  restrictedToMinimumLevel: LogEventLevel.Error)
    .CreateLogger();
```

### Caching Configuration
```csharp
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
});
```

### CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
```

## Testing

The solution includes comprehensive test coverage:

### Test Projects
- **Unit Tests**: Service layer testing with mocked dependencies
- **Integration Tests**: Database and API endpoint testing
- **Real Data Tests**: Fuzzy matching algorithm validation

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test NphiesBridge.Tests
```

### Test Dependencies
```xml
<PackageReference Include="xunit" Version="2.5.3" />
<PackageReference Include="FluentAssertions" Version="8.5.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.17" />
```

## Performance Optimization

### Fuzzy Matching Performance
- **Memory Caching**: NPHIES codes cached for 12 hours
- **Parallel Processing**: Multi-threaded similarity calculations
- **Pre-filtering**: Keyword-based candidate selection
- **Optimized Algorithms**: Weighted scoring system

### Database Optimization
- **Indexes**: Strategic indexing on frequently queried columns
- **No-Tracking Queries**: Read-only operations use `AsNoTracking()`
- **Batch Operations**: Bulk inserts for mapping sessions

## Security Features

### Authentication & Authorization
- **JWT Bearer Tokens**: Stateless authentication
- **Role-Based Access Control**: Admin/Provider roles
- **Password Requirements**: Configurable complexity rules
- **Token Expiration**: Configurable token lifetime

### Data Protection
- **HTTPS Enforcement**: Secure communication
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **Input Validation**: FluentValidation rules
- **CORS Policy**: Configurable cross-origin requests

## Deployment

### Production Deployment Steps

#### 1. Build for Production
```bash
dotnet publish -c Release -o ./publish
```

#### 2. Database Setup
```bash
# Update production connection string
# Run migrations in production environment
dotnet ef database update --environment Production
```

#### 3. IIS Configuration
- Install ASP.NET Core Hosting Bundle
- Configure application pool (.NET CLR Version: No Managed Code)
- Set environment variables

#### 4. Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<production-connection-string>
JwtSettings__SecretKey=<production-secret-key>
```

## Contributing

### Development Guidelines
1. Follow Clean Architecture principles
2. Implement comprehensive unit tests
3. Use FluentValidation for input validation
4. Add logging for debugging and monitoring
5. Document API endpoints with XML comments

### Code Style
- Use C# 12.0 features where applicable
- Follow Microsoft C# coding conventions
- Implement async/await patterns for I/O operations
- Use dependency injection for service registration

## Support & Documentation

### Additional Resources
- **API Documentation**: Available via Swagger UI at `/swagger`
- **Database Schema**: Entity Framework migrations in `/Migrations`
- **Excel Templates**: Available for download in the web interface

### Troubleshooting
- Check logs in the `Logs/` directory
- Verify database connectivity
- Ensure proper JWT configuration
- Validate Excel template format

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
