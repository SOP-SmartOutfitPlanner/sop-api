# AI Agent Instructions for SOPServer API

This document provides guidance for AI coding agents to effectively contribute to the SOPServer API codebase.

## Architecture Overview

The SOPServer API follows a standard N-tier architecture, composed of three main projects:

- **SOPServer.API**: The presentation layer, built with ASP.NET Core. Handles HTTP requests, responses, and API documentation (Swagger). All controllers inherit from `BaseController` to use the standardized `ValidateAndExecute` method.
- **SOPServer.Service**: The business logic layer. Core application logic resides here. Services are defined with interfaces and injected into controllers.
- **SOPServer.Repository**: The data access layer. Uses Entity Framework Core with repository and unit of work patterns to interact with SQL Server database.

The data flow is unidirectional: `API` -> `Service` -> `Repository`.

## Key Technologies

- **.NET 8.0**: Core framework
- **Entity Framework Core**: Data access with soft delete support
- **ASP.NET Core**: Web API framework
- **JWT Bearer Tokens**: Authentication mechanism
- **Redis**: Caching layer
- **Minio**: Object storage
- **Firebase**: Push notifications
- **Qdrant**: Vector search for AI features
- **Docker**: Application containerization

## Developer Workflow

### Building and Running

```bash
dotnet build SOPServer.sln
dotnet run --project SOPServer.API
```

Check `SOPServer.API/Properties/launchSettings.json` for available URLs.

### Testing API Endpoints

Use the `.http` file in `SOPServer.API/SOPServer.API.http` with VS Code REST Client extension.

### Configuration

- Settings: `appsettings.json` and `appsettings.Development.json`
- DI Setup: `SOPServer.API/DependencyInjection.cs`
- Feature Configs: `SOPServer.API/Configurations/` directory

## Project Standards & Patterns

### Controllers

- **Inherit from `BaseController`** - provides `ValidateAndExecute()` method for consistent error handling
- **Wrap all service calls** in `ValidateAndExecute()` for automatic validation and response formatting
- **Extract JWT claims** using `User.FindFirst()` for role-based operations
- **Example**:
  ```csharp
  [HttpPost("{id}/resolve-with-action")]
  [Authorize(Roles = "ADMIN")]
  public Task<IActionResult> ResolveWithAction(long id, [FromBody] ResolveWithActionModel model)
  {
      var adminIdClaim = User.FindFirst("UserId")?.Value;
      return ValidateAndExecute(async () =>
          await _reportCommunityService.ResolveWithActionAsync(id, long.Parse(adminIdClaim), model));
  }
  ```

### Dependency Injection

- **Register in `DependencyInjection.cs`** using `AddInfractstructure` extension method
- **Organize by feature domain** with section comments: `// ========== USER MANAGEMENT ==========`
- **Always register interface + implementation**: `services.AddScoped<IRepository, Repository>();`
- **Use constructor injection** - never use service locator pattern
- **Example**:
  ```csharp
  // ========== REPORT COMMUNITY ==========
  services.AddScoped<IReportCommunityRepository, ReportCommunityRepository>();
  services.AddScoped<IReportCommunityService, ReportCommunityService>();
  services.AddScoped<IUserSuspensionRepository, UserSuspensionRepository>();
  ```

### Services

- **Define interface first** in `SOPServer.Service/Services/Interfaces/`
- **Constructor inject dependencies**:
  - `IUnitOfWork` for data access
  - `IMapper` for object mapping
  - `IConfiguration` for settings
  - Other service interfaces as needed
- **Use async/await** for all I/O operations
- **Return `BaseResponseModel`** from all public methods with:
  - `StatusCode`: HTTP status code
  - `Message`: Message constant from `MessageConstants.cs`
  - `Data`: Response payload (can be null)

### Error Handling

- **Use custom exceptions** from `SOPServer.Service/Exceptions/`:
  - `NotFoundException`: Resource doesn't exist (404)
  - `BadRequestException`: Invalid input or logic violation (400)
  - `UnauthorizedException`: Auth failures (401)
  - `ForbiddenException`: Permission denied (403)
- **All inherit from `BaseErrorResponseException`** with `HttpStatusCode` and optional `Data`
- **Examples**:
  ```csharp
  throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
  throw new BadRequestException("Email already exists");
  ```

### Response Models & Status Codes

- **All services return `BaseResponseModel`**:
  ```csharp
  return new BaseResponseModel
  {
      StatusCode = StatusCodes.Status200OK,
      Message = MessageConstants.ITEM_CREATE_SUCCESS,
      Data = itemModel
  };
  ```
- **Paginated responses use `ModelPaging`**:
  ```csharp
  return new BaseResponseModel
  {
      StatusCode = StatusCodes.Status200OK,
      Message = MessageConstants.GET_LIST_ITEM_SUCCESS,
      Data = new ModelPaging
      {
          Data = pagination,
          MetaData = new { pagination.TotalCount, pagination.PageSize, pagination.CurrentPage }
      }
  };
  ```
- **Standard HTTP status codes**:
  - `200 OK`: Successful retrieval or operation
  - `201 Created`: Resource created
  - `400 Bad Request`: Invalid input or logic violation
  - `401 Unauthorized`: Auth failed
  - `403 Forbidden`: Permission denied
  - `404 Not Found`: Resource not found
  - `409 Conflict`: Resource conflict (duplicate)

### Data Models

- **Organized by feature** in `SOPServer.Service/BusinessModels/`
- **Separate models by operation**:
  - `*CreateModel`: POST requests with validation
  - `*UpdateModel`: PUT/PATCH requests with ID
  - `*Model`: Response/display models
  - `*FilterModel`: Query/filter parameters
  - `*DetailModel`: Detailed response with related data
- **Always validate request models** with DataAnnotations:
  ```csharp
  public class ItemCreateModel
  {
      [Required(ErrorMessage = "Name is required")]
      [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
      public string Name { get; set; }
  }
  ```

### AutoMapper

- **Domain-specific profiles** in `SOPServer.Service/Mappers/` (e.g., `UserMapperProfile.cs`)
- **All profiles auto-discovered** by `MapperConfigProfile`
- **Inject `IMapper`** into services
- **Example**:
  ```csharp
  public class UserMapperProfile : Profile
  {
      public UserMapperProfile()
      {
          CreateMap<User, UserModel>();
          CreateMap<UserCreateModel, User>();
      }
  }
  ```

### Constants & Messages

- **Message constants**: `SOPServer.Service/Constants/MessageConstants.cs`
- **Redis key patterns**: `SOPServer.Service/Constants/RedisKeyConstants.cs`
- **Always use constants** instead of hard-coded strings
- **Example**:
  ```csharp
  public const string USER_NOT_EXIST = "User is not exist";
  public const string ITEM_CREATE_SUCCESS = "Item created successfully";
  ```

### Repository Pattern & Data Access

- **All repositories inherit** from `IGenericRepository<TEntity>`
- **Soft delete pattern**: `IsDeleted` flag automatically checked in queries
- **Generic queries exclude deleted**: `.Where(x => !x.IsDeleted)`
- **Custom methods must check**: `!entity.IsDeleted` in WHERE clauses
- **Repository interfaces**: `SOPServer.Repository/Repositories/Interfaces/`
- **Implementations**: `SOPServer.Repository/Repositories/Implements/`
- **UnitOfWork lazy initialization**:
  ```csharp
  private IUserRepository _userRepository;
  public IUserRepository UserRepository
  {
      get { return _userRepository ??= new UserRepository(_context); }
  }
  ```

### Async/Await

- **Use async/await** for all I/O operations (database, external services)
- **Method names end with `Async`**: `GetUserAsync()`, `CreateReportAsync()`
- **Never use `.Result` or `.Wait()`**

## Code Quality Standards

- **Null Coalescing Assignment**: Use `??=` for lazy initialization
- **Soft Delete Only**: Use `IsDeleted` flag instead of permanent deletion
- **Entity Timestamps**: All entities inherit `BaseEntity` with `CreatedDate`, `UpdatedDate`, `IsDeleted`
- **Pagination**: Use `ToPaginationAsync()` from generic repository
- **Field Initialization**: Initialize collections with `new List<T>()`
- **Null-Conditional Operator**: Use `?.` for safe access
- **String Defaults**: Use `string.Empty` instead of null
- **Section Comments**: Organize code with `// ========== SECTION NAME ==========`
- **Validation Attributes**: Always validate request models
- **Property Naming**: Follow camelCase in JSON responses (handled by SerializerSettings)

## Common Patterns

### Creating a New Feature

1. **Define repository interface** in `Repositories/Interfaces/`
2. **Implement repository** in `Repositories/Implements/`
3. **Register in UnitOfWork** with lazy initialization
4. **Create service interface** in `Services/Interfaces/`
5. **Create service implementation** with DI for `IUnitOfWork`, `IMapper`
6. **Return `BaseResponseModel`** from all public methods
7. **Create mapper profile** in `Mappers/`
8. **Create controller** inheriting from `BaseController`
9. **Register DI** in `DependencyInjection.cs`
10. **Add message constants** to `MessageConstants.cs`

### Handling Errors

```csharp
// Validate resource exists
var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
if (user == null)
    throw new NotFoundException(MessageConstants.USER_NOT_EXIST);

// Validate input
if (model.Amount <= 0)
    throw new BadRequestException("Amount must be greater than zero");

// Check duplicate
var existing = await _unitOfWork.Repository.GetByNameAsync(model.Name);
if (existing != null)
    throw new BadRequestException(MessageConstants.ALREADY_EXISTS);
```

### Returning Responses

```csharp
// Single item
return new BaseResponseModel
{
    StatusCode = StatusCodes.Status200OK,
    Message = MessageConstants.GET_USER_SUCCESS,
    Data = _mapper.Map<UserModel>(user)
};

// List with pagination
var pagination = await _unitOfWork.UserRepository
    .ToPaginationAsync(paginationParameter);
return new BaseResponseModel
{
    StatusCode = StatusCodes.Status200OK,
    Message = MessageConstants.GET_LIST_USER_SUCCESS,
    Data = new ModelPaging { Data = pagination }
};

// Creation
return new BaseResponseModel
{
    StatusCode = StatusCodes.Status201Created,
    Message = MessageConstants.USER_CREATE_SUCCESS,
    Data = _mapper.Map<UserModel>(newUser)
};
```

## File Structure Quick Reference

```
SOPServer.API/
  ├── Controllers/          (inherit from BaseController)
  ├── Configurations/       (setup services)
  ├── Middlewares/          (exception handling)
  └── DependencyInjection.cs

SOPServer.Service/
  ├── Services/
  │   ├── Interfaces/       (service contracts)
  │   └── Implements/       (service implementations)
  ├── BusinessModels/       (organized by feature)
  ├── Mappers/             (AutoMapper profiles)
  ├── Constants/           (message & key constants)
  ├── Exceptions/          (custom exceptions)
  └── Utils/

SOPServer.Repository/
  ├── Repositories/
  │   ├── Interfaces/      (repository contracts)
  │   ├── Implements/      (repository implementations)
  │   └── Generic/         (base repository)
  ├── Entities/            (EF Core entities)
  ├── Enums/              (enumeration types)
  ├── DBContext/          (DbContext definition)
  └── UnitOfWork/         (UnitOfWork pattern)
```

## Quick Checklist for New Features

- [ ] Define repository interface with custom queries
- [ ] Implement repository with soft delete consideration
- [ ] Create service interface with clear contracts
- [ ] Implement service with error handling and validation
- [ ] Create mapper profile for data transformation
- [ ] Define request/response models with validation
- [ ] Add message constants
- [ ] Register DI with organized sections
- [ ] Create controller using `ValidateAndExecute`
- [ ] Add proper XML documentation comments
- [ ] Test with `.http` file
