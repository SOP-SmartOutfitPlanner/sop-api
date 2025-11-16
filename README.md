# SOPServer API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://docs.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![Redis](https://img.shields.io/badge/Redis-Caching-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)

**Smart Outfit Planner (SOP)** backend API - A modern ASP.NET Core 8.0 REST API for intelligent wardrobe management, outfit planning, and fashion community features with AI-powered recommendations.

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Architecture](#-architecture)
- [Key Features](#-key-features)
- [Technology Stack](#-technology-stack)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Running the Application](#running-the-application)
- [Project Structure](#-project-structure)
- [API Documentation](#-api-documentation)
- [Database](#-database)
- [Authentication & Authorization](#-authentication--authorization)
- [Development Guidelines](#-development-guidelines)
- [Docker Deployment](#-docker-deployment)
- [External Services](#-external-services)
- [Contributing](#-contributing)

---

## 🎯 Overview

SOPServer API is a comprehensive backend solution for a smart outfit planning application. It provides endpoints for managing wardrobes, creating outfits, sharing fashion collections, social interactions, and AI-powered clothing recommendations based on weather, occasions, and personal style preferences.

The API follows clean architecture principles with clear separation of concerns across three main layers:

- **API Layer** - Controllers and HTTP handling
- **Service Layer** - Business logic and orchestration
- **Repository Layer** - Data access and persistence

---

## 🏗️ Architecture

### N-Tier Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    SOPServer.API                         │
│  Controllers | Middleware | Configuration | Swagger      │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                   SOPServer.Service                      │
│  Business Logic | Services | Mappers | Exceptions       │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                 SOPServer.Repository                     │
│  EF Core | Repositories | UnitOfWork | Entities         │
└────────────────────────┬────────────────────────────────┘
                         │
                   ┌─────▼──────┐
                   │ SQL Server  │
                   └─────────────┘
```

### Design Patterns

- **Repository Pattern** - Abstracts data access logic
- **Unit of Work Pattern** - Manages database transactions
- **Dependency Injection** - Promotes loose coupling and testability
- **Soft Delete Pattern** - Preserves data integrity with `IsDeleted` flag
- **Service Layer Pattern** - Encapsulates business logic

---

## ✨ Key Features

### 👤 User Management

- **Authentication**: Google OAuth 2.0 and email/password registration
- **Email Verification**: OTP-based email verification system
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Role-Based Access Control**: USER, STYLIST, and ADMIN roles
- **User Profiles**: Customizable profiles with avatar, bio, preferences, and job information
- **Premium Subscriptions**: Subscription plans with transaction management

### 👔 Wardrobe Management

- **Smart Item Cataloging**: Add clothing items with AI-powered image analysis
- **Categorization**: Organize items by categories, styles, seasons, and occasions
- **Color Management**: Track preferred and avoided colors
- **Item Metadata**: Store purchase date, brand, price, and usage history
- **Vector Search**: Qdrant integration for similarity-based item discovery

### 👗 Outfit Planning

- **Outfit Creation**: Combine multiple items into cohesive outfits
- **Weather-Aware Suggestions**: Outfit recommendations based on current weather
- **Occasion-Based Planning**: Outfit suggestions for specific events (work, casual, formal, etc.)
- **Usage History**: Track when and where outfits were worn
- **Calendar Integration**: Plan outfits for future dates

### 📱 Social & Community Features

- **Posts & Feed**: Share fashion posts with images and hashtags
- **Collections**: Curate and publish outfit collections
- **Likes & Comments**: Engage with posts and collections
- **Following System**: Follow other users and stylists
- **Save Collections**: Bookmark collections for later reference
- **Reporting System**: Report inappropriate content with admin moderation
- **User Suspensions**: Automated violation tracking and account suspension

### 🤖 AI-Powered Features

- **Image Analysis**: Gemini AI integration for clothing item recognition
- **Smart Recommendations**: Context-aware outfit suggestions
- **Vector Search**: Semantic search using Qdrant for similar items
- **Weather Integration**: Real-time weather data for outfit planning

### 🔔 Notifications

- **Push Notifications**: Firebase Cloud Messaging integration
- **Real-time Alerts**: Notifications for likes, comments, follows, and reports
- **Multi-device Support**: Manage notification preferences per device

### 👨‍💼 Admin & Stylist Features

- **Stylist Dashboard**: Analytics and insights for fashion stylists
- **Report Management**: Review and resolve community reports
- **User Moderation**: Suspend users, track violations, and enforce community guidelines
- **Content Management**: Manage categories, styles, occasions, and seasons

---

## 🛠️ Technology Stack

### Core Framework

- **.NET 8.0** - Latest LTS version with improved performance
- **ASP.NET Core** - Web API framework
- **C# 12** - Modern language features

### Data Access

- **Entity Framework Core 8.0** - ORM with Code First approach
- **SQL Server** - Primary database
- **Repository & Unit of Work Patterns** - Data access abstraction

### Caching & Storage

- **Redis** - Distributed caching with StackExchange.Redis
- **Minio** - S3-compatible object storage for images

### Authentication & Security

- **JWT Bearer Tokens** - Stateless authentication
- **Google OAuth 2.0** - Social login integration
- **BCrypt.Net** - Password hashing

### External Services

- **Firebase Admin SDK** - Push notifications
- **Gemini AI (Google GenerativeAI)** - Image analysis and recommendations
- **Qdrant** - Vector database for semantic search
- **PayOS** - Payment processing for subscriptions
- **MailKit** - Email sending with SMTP

### API Documentation

- **Swagger/OpenAPI** - Interactive API documentation
- **XML Documentation** - Detailed endpoint descriptions

### Development Tools

- **AutoMapper** - Object-to-object mapping
- **Newtonsoft.Json** - JSON serialization

---

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (Local, Express, or Azure SQL) - [Download](https://www.microsoft.com/sql-server/sql-server-downloads)
- **Redis Server** (Optional but recommended) - [Download](https://redis.io/download)
- **Visual Studio 2022** or **Visual Studio Code** (Recommended)
- **Docker** (Optional, for containerized deployment)

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/SOP-SmartOutfitPlanner/sop-api.git
   cd sop-api
   ```

2. **Restore NuGet packages**

   ```bash
   dotnet restore SOPServer.sln
   ```

3. **Build the solution**
   ```bash
   dotnet build SOPServer.sln --configuration Release
   ```

### Configuration

1. **Update Connection Strings**

   Edit `SOPServer.API/appsettings.json` or create `appsettings.Development.json`:

   ```json
   {
     "ConnectionStrings": {
       "SOPServerLocal": "Data Source=.;Initial Catalog=SOPServer;Integrated Security=True;TrustServerCertificate=True"
     }
   }
   ```

2. **Configure JWT Settings**

   ```json
   {
     "JWT": {
       "ValidAudience": "SOPServer",
       "ValidIssuer": "http://SOP.io.vn",
       "SecretKey": "your-super-secret-key-min-32-characters",
       "TokenValidityInMinutes": 301,
       "RefreshTokenValidityInDays": 7
     }
   }
   ```

3. **Configure Redis (Optional)**

   ```json
   {
     "RedisSettings": {
       "RedisConnectionString": "localhost:6379",
       "InstanceName": "SOPServer",
       "DefaultExpiryMinutes": 30
     }
   }
   ```

4. **Configure External Services**

   Update the following sections in `appsettings.json`:

   - **MinioStorage**: Object storage configuration
   - **Gemini**: Google AI API key and model
   - **GoogleCredential**: OAuth client ID
   - **MailSettings**: SMTP email configuration
   - **QDrantSettings**: Vector database connection
   - **PayOSSettings**: Payment gateway credentials

5. **Add Firebase Admin SDK**

   Place your Firebase service account JSON file in `SOPServer.API/firebase-adminsdk.json`

6. **Apply Database Migrations**

   ```bash
   cd SOPServer.API
   dotnet ef database update
   ```

   Or using Package Manager Console in Visual Studio:

   ```powershell
   Update-Database
   ```

### Running the Application

#### Using .NET CLI

```bash
cd SOPServer.API
dotnet run
```

The API will be available at:

- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `http://localhost:5000/swagger` or `https://localhost:5001/swagger`

#### Using Visual Studio

1. Open `SOPServer.sln` in Visual Studio 2022
2. Set `SOPServer.API` as the startup project
3. Press `F5` or click **Run**

#### Using Visual Studio Code

1. Open the workspace folder
2. Press `F5` or use the **Run and Debug** panel
3. Select `.NET Core Launch (web)` configuration

---

## 📁 Project Structure

```
SOPServer/
├── SOPServer.API/                      # Presentation Layer
│   ├── Controllers/                     # API Controllers
│   │   ├── AuthController.cs           # Authentication endpoints
│   │   ├── UserController.cs           # User management
│   │   ├── ItemController.cs           # Wardrobe items
│   │   ├── OutfitController.cs         # Outfit management
│   │   ├── CollectionController.cs     # Collections
│   │   ├── PostController.cs           # Social posts
│   │   ├── NotificationController.cs   # Notifications
│   │   ├── ReportCommunityController.cs # Moderation
│   │   └── ...                         # Other controllers
│   ├── Middlewares/                     # Custom middleware
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── AuthenHandlingMiddleware.cs
│   ├── Configurations/                  # Startup configurations
│   │   ├── ApiConfiguration.cs
│   │   ├── AuthenticationConfiguration.cs
│   │   ├── DatabaseConfiguration.cs
│   │   ├── RedisConfiguration.cs
│   │   └── ...
│   ├── Templates/                       # Email templates
│   │   └── Emails/
│   ├── appsettings.json                 # Application settings
│   ├── DependencyInjection.cs           # Service registration
│   ├── Program.cs                       # Application entry point
│   └── SOPServer.API.http               # HTTP test requests
│
├── SOPServer.Service/                   # Business Logic Layer
│   ├── Services/                        # Service implementations
│   │   ├── Interfaces/                  # Service contracts
│   │   └── Implements/                  # Service implementations
│   ├── BusinessModels/                  # DTOs and view models
│   │   ├── AuthenModels/
│   │   ├── UserModels/
│   │   ├── ItemModels/
│   │   ├── OutfitModels/
│   │   ├── CollectionModels/
│   │   └── ...
│   ├── Mappers/                         # AutoMapper profiles
│   ├── Constants/                       # Constants and messages
│   │   ├── MessageConstants.cs
│   │   └── RedisKeyConstants.cs
│   ├── Exceptions/                      # Custom exceptions
│   │   ├── BaseErrorResponseException.cs
│   │   ├── NotFoundException.cs
│   │   ├── BadRequestException.cs
│   │   ├── UnauthorizedException.cs
│   │   └── ForbiddenException.cs
│   └── Utils/                           # Utility classes
│
├── SOPServer.Repository/                # Data Access Layer
│   ├── Repositories/                    # Repository implementations
│   │   ├── Interfaces/                  # Repository contracts
│   │   ├── Implements/                  # Repository implementations
│   │   └── Generic/                     # Base repository
│   ├── Entities/                        # EF Core entities
│   │   ├── User.cs
│   │   ├── Item.cs
│   │   ├── Outfit.cs
│   │   ├── Collection.cs
│   │   ├── Post.cs
│   │   └── ...
│   ├── Enums/                          # Enumeration types
│   ├── DBContext/                      # DbContext
│   │   └── SOPServerContext.cs
│   ├── Migrations/                     # EF Core migrations
│   ├── UnitOfWork/                     # Unit of Work pattern
│   └── Commons/                        # Common utilities
│
├── SOPServer.Service.Tests/            # Unit Tests
│   └── Services/
│
├── Dockerfile                          # Docker configuration
├── SOPServer.sln                       # Solution file
└── README.md                           # This file
```

---

## 📚 API Documentation

### Swagger UI

Once the application is running, access the interactive API documentation:

**URL**: `https://localhost:5001/swagger` (or your configured port)

Swagger provides:

- Complete endpoint documentation with parameters and response schemas
- Try-it-out functionality for testing endpoints directly
- JWT authentication integration (click **Authorize** button)
- Request/response examples

### Main API Endpoints

#### Authentication (`/api/v1/auth`)

- `POST /login/google/oauth` - Google OAuth login
- `POST /register` - Register with email/password
- `POST /otp/verify` - Verify OTP code
- `POST /refresh-token` - Refresh JWT token

#### Users (`/api/v1/user`)

- `GET /` - Get all users (Admin only)
- `GET /{id}` - Get user by ID
- `GET /current` - Get current authenticated user
- `PUT /` - Update user profile
- `POST /premium` - Upgrade to premium

#### Items (`/api/v1/items`)

- `GET /` - Get all items (Admin)
- `GET /user/{userId}` - Get user's wardrobe items
- `GET /{id}` - Get item by ID
- `POST /` - Create new item with AI analysis
- `POST /analysis` - Analyze clothing image
- `PUT /{id}` - Update item
- `DELETE /{id}` - Delete item

#### Outfits (`/api/v1/outfits`)

- `GET /` - Get all outfits (Admin)
- `GET /user/{userId}` - Get user's outfits
- `GET /{id}` - Get outfit by ID
- `POST /` - Create outfit
- `POST /ai-generate` - AI-generated outfit suggestions
- `POST /{id}/mark-worn` - Mark outfit as worn
- `PUT /{id}` - Update outfit
- `DELETE /{id}` - Delete outfit

#### Collections (`/api/v1/collections`)

- `GET /` - Get published collections
- `GET /user/{userId}` - Get user's collections
- `GET /{id}` - Get collection by ID
- `POST /` - Create collection
- `PUT /{id}` - Update collection
- `POST /{id}/toggle-publish` - Publish/unpublish collection
- `DELETE /{id}` - Delete collection

#### Posts (`/api/v1/posts`)

- `GET /` - Get all posts (feed)
- `GET /user/{userId}` - Get user's posts
- `GET /{id}` - Get post by ID
- `POST /` - Create post
- `PUT /{id}` - Update post
- `DELETE /{id}` - Delete post

#### Social Interactions

- **Like Collection**: `POST /api/v1/like-collections/{id}`
- **Comment Collection**: `POST /api/v1/comment-collections`
- **Save Collection**: `POST /api/v1/save-collections/{id}`
- **Like Post**: `POST /api/v1/like-posts/{id}`
- **Comment Post**: `POST /api/v1/comment-posts`
- **Follow User**: `POST /api/v1/followers/{userId}`

#### Reports & Moderation (`/api/v1/reports`)

- `GET /` - Get all reports (Admin)
- `POST /` - Submit report
- `POST /{id}/resolve` - Resolve report (Admin)
- `POST /{id}/resolve-with-action` - Resolve with user action (Admin)

#### Notifications (`/api/v1/notifications`)

- `GET /` - Get user notifications
- `POST /{id}/read` - Mark as read
- `POST /read-all` - Mark all as read

### Authentication

Most endpoints require JWT authentication. Include the token in the `Authorization` header:

```
Authorization: Bearer <your-jwt-token>
```

### Testing with `.http` File

Use the provided `SOPServer.API/SOPServer.API.http` file with the **REST Client** extension in VS Code for manual testing.

---

## 💾 Database

### Entity Framework Core

The project uses **Code First** approach with EF Core 8.0.

#### Key Entities

- **User** - User accounts with roles and preferences
- **Item** - Clothing items with metadata and AI analysis
- **Category** - Item categories (Tops, Bottoms, Shoes, etc.)
- **Style** - Fashion styles (Casual, Formal, Sporty, etc.)
- **Season** - Seasonal tags (Spring, Summer, Fall, Winter)
- **Occasion** - Event types (Work, Party, Gym, etc.)
- **Outfit** - Combinations of items
- **Collection** - Curated sets of outfits
- **Post** - Social media posts
- **Notification** - User notifications
- **ReportCommunity** - Content reports
- **UserSuspension** - Account suspension records

#### Relationships

- Users can have multiple Items, Outfits, Posts, and Collections
- Items can be associated with multiple Styles, Seasons, and Occasions (many-to-many)
- Outfits contain multiple Items through OutfitItems junction table
- Collections contain multiple Outfits through CollectionOutfit junction table
- Posts can have multiple Images, Hashtags, Likes, and Comments

#### Migrations

Create a new migration after entity changes:

```bash
dotnet ef migrations add MigrationName --project SOPServer.Repository --startup-project SOPServer.API
```

Apply pending migrations:

```bash
dotnet ef database update --project SOPServer.API
```

#### Soft Delete

All entities inherit from `BaseEntity` which includes:

- `Id` (long) - Primary key
- `CreatedDate` (DateTime) - Creation timestamp
- `UpdatedDate` (DateTime) - Last update timestamp
- `IsDeleted` (bool) - Soft delete flag

Entities are never permanently deleted; instead, `IsDeleted` is set to `true`.

---

## 🔐 Authentication & Authorization

### JWT Token Configuration

- **Access Token Validity**: 301 minutes (configured in `appsettings.json`)
- **Refresh Token Validity**: 7 days
- **Token Claims**: UserId, Email, Role, DisplayName

### User Roles

1. **USER** - Standard users

   - Manage personal wardrobe and outfits
   - Create posts and collections
   - Interact with community content

2. **STYLIST** - Fashion stylists

   - All USER permissions
   - Access to stylist dashboard with analytics

3. **ADMIN** - Administrators
   - All system access
   - User moderation and content management
   - System configuration

### Role-Based Endpoints

Use the `[Authorize(Roles = "ADMIN,STYLIST")]` attribute on controllers/actions:

```csharp
[HttpPost("{id}/resolve-with-action")]
[Authorize(Roles = "ADMIN")]
public Task<IActionResult> ResolveWithAction(long id, [FromBody] ResolveWithActionModel model)
{
    // Admin-only action
}
```

### Password Security

- Passwords are hashed using **BCrypt** with salt rounds
- Never store plain-text passwords
- Password reset via email OTP

---

## 👨‍💻 Development Guidelines

### Code Standards

Following the project's coding instructions (`.github/copilot-instructions.md`):

#### 1. Controller Development

- **Inherit from `BaseController`** for standardized response handling
- **Use `ValidateAndExecute()`** wrapper for all service calls
- **Extract JWT claims** for role-based operations

```csharp
[HttpPost("{id}")]
[Authorize(Roles = "USER")]
public Task<IActionResult> CreateItem(ItemCreateModel model)
{
    var userIdClaim = User.FindFirst("UserId")?.Value;
    return ValidateAndExecute(async () =>
        await _itemService.AddNewItem(model, long.Parse(userIdClaim)));
}
```

#### 2. Service Development

- **Define interface first** in `Services/Interfaces/`
- **Return `BaseResponseModel`** from all public methods
- **Use constructor injection** for dependencies
- **Throw custom exceptions** for error handling

```csharp
public async Task<BaseResponseModel> GetItemById(long id)
{
    var item = await _unitOfWork.ItemRepository.GetByIdAsync(id);
    if (item == null)
        throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);

    return new BaseResponseModel
    {
        StatusCode = StatusCodes.Status200OK,
        Message = MessageConstants.ITEM_GET_SUCCESS,
        Data = _mapper.Map<ItemModel>(item)
    };
}
```

#### 3. Repository Pattern

- **All repositories inherit from `IGenericRepository<TEntity>`**
- **Use soft delete** - never hard delete entities
- **Check `IsDeleted` flag** in custom queries

```csharp
public async Task<Item?> GetActiveItemAsync(long id)
{
    return await _context.Items
        .Where(x => x.Id == id && !x.IsDeleted)
        .FirstOrDefaultAsync();
}
```

#### 4. Data Models

- **Separate models by operation**:
  - `*CreateModel` - POST requests with validation attributes
  - `*UpdateModel` - PUT/PATCH requests
  - `*Model` - Response/display models
  - `*FilterModel` - Query parameters

```csharp
public class ItemCreateModel
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    public IFormFile Image { get; set; }

    [Required]
    public long CategoryId { get; set; }
}
```

#### 5. Dependency Injection

Register all services in `DependencyInjection.cs`:

```csharp
// ========== ITEM MANAGEMENT ==========
services.AddScoped<IItemRepository, ItemRepository>();
services.AddScoped<IItemService, ItemService>();
```

#### 6. AutoMapper Profiles

Create domain-specific mapper profiles:

```csharp
public class ItemMapperProfile : Profile
{
    public ItemMapperProfile()
    {
        CreateMap<Item, ItemModel>();
        CreateMap<ItemCreateModel, Item>();
        CreateMap<Item, ItemDetailModel>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category.Name));
    }
}
```

#### 7. Error Handling

Use custom exceptions from `SOPServer.Service/Exceptions/`:

```csharp
throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
throw new BadRequestException("Invalid input data");
throw new UnauthorizedException("Token expired");
throw new ForbiddenException("Access denied");
```

#### 8. Constants

Always use constants instead of magic strings:

```csharp
// In MessageConstants.cs
public const string ITEM_CREATE_SUCCESS = "Item created successfully";

// Usage
return new BaseResponseModel
{
    Message = MessageConstants.ITEM_CREATE_SUCCESS
};
```

### Adding a New Feature

Follow this checklist when adding new features:

1. ✅ Define repository interface in `Repositories/Interfaces/`
2. ✅ Implement repository in `Repositories/Implements/`
3. ✅ Register in UnitOfWork with lazy initialization
4. ✅ Create service interface in `Services/Interfaces/`
5. ✅ Create service implementation with business logic
6. ✅ Create mapper profile in `Mappers/`
7. ✅ Create request/response models in `BusinessModels/`
8. ✅ Create controller inheriting from `BaseController`
9. ✅ Register DI in `DependencyInjection.cs`
10. ✅ Add message constants to `MessageConstants.cs`
11. ✅ Update Swagger documentation with XML comments
12. ✅ Test endpoints using `.http` file

---

## 🐳 Docker Deployment

### Build Docker Image

```bash
docker build -t sopserver-api:latest .
```

### Run Container

```bash
docker run -d \
  -p 8386:8386 \
  -e ConnectionStrings__SOPServerLocal="your-connection-string" \
  -e JWT__SecretKey="your-secret-key" \
  --name sopserver-api \
  sopserver-api:latest
```

### Docker Compose (Recommended)

Create a `docker-compose.yml`:

```yaml
version: "3.8"

services:
  api:
    build: .
    ports:
      - "8386:8386"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__SOPServerLocal=Server=db;Database=SOPServer;User=sa;Password=YourPassword123!
    depends_on:
      - db
      - redis

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"

volumes:
  sqldata:
```

Run with:

```bash
docker-compose up -d
```

---

## 🔌 External Services

### Required Services

1. **SQL Server**

   - Primary database for all entities
   - Configure connection string in `appsettings.json`

2. **Redis** (Recommended)

   - Caching layer for improved performance
   - Session management and distributed locking

3. **Minio** (S3-Compatible Storage)

   - Image and file storage
   - Configure endpoint, bucket, and credentials

4. **Firebase**

   - Push notifications via FCM
   - Place service account JSON in `firebase-adminsdk.json`

5. **Gemini AI** (Google GenerativeAI)

   - Clothing image analysis
   - Outfit recommendations
   - Requires API key in `appsettings.json`

6. **Qdrant**

   - Vector database for semantic search
   - Similar item discovery

7. **SMTP Server** (Optional)

   - Email verification and password reset
   - Configure MailKit settings

8. **PayOS** (Optional)
   - Premium subscription payments
   - Requires merchant credentials

---

## 🤝 Contributing

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Follow coding standards and patterns
4. Commit changes (`git commit -m 'Add some AmazingFeature'`)
5. Push to branch (`git push origin feature/AmazingFeature`)
6. Open a Pull Request

### Code Review Checklist

- ✅ Follows N-tier architecture
- ✅ Uses Repository and Unit of Work patterns
- ✅ All services return `BaseResponseModel`
- ✅ Custom exceptions for error handling
- ✅ Proper validation on request models
- ✅ AutoMapper profiles configured
- ✅ Constants used instead of magic strings
- ✅ Soft delete pattern maintained
- ✅ JWT authorization on protected endpoints
- ✅ XML documentation on public APIs
- ✅ Swagger examples provided

---

## 📝 License

This project is proprietary software owned by **SOP - Smart Outfit Planner**. All rights reserved.

---

## 📧 Contact & Support

- **Repository**: [SOP-SmartOutfitPlanner/sop-api](https://github.com/SOP-SmartOutfitPlanner/sop-api)
- **Issues**: [GitHub Issues](https://github.com/SOP-SmartOutfitPlanner/sop-api/issues)
- **Documentation**: [Swagger UI](https://localhost:5001/swagger)

---

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- Entity Framework Core by Microsoft
- AutoMapper by Jimmy Bogard
- Firebase Admin SDK by Google
- Gemini AI by Google
- Qdrant Vector Database
- Redis by Redis Labs

---

**Version**: 1.0.0  
**Last Updated**: November 2025  
**Framework**: .NET 8.0
