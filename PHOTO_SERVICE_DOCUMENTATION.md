# Photo Service - Complete Technical Documentation

## Overview

The Photo Service is a dedicated microservice in the Dating App ecosystem that handles all photo-related operations including upload, processing, storage, moderation, and retrieval. It provides a complete solution for user profile photos with automatic image processing, multiple size generation, and content moderation capabilities.

## Architecture

### Technology Stack
- **Framework**: ASP.NET Core 8.0 Web API
- **Database**: MySQL 8.0 with Entity Framework Core
- **Image Processing**: SixLabors.ImageSharp 3.1.6
- **Authentication**: JWT Bearer tokens (shared with other services)
- **Storage**: Local file system with configurable paths
- **Containerization**: Docker with multi-stage builds

### Service Design Patterns
- **Repository Pattern**: Clean separation of data access logic
- **Service Layer Pattern**: Business logic encapsulation
- **DTO Pattern**: Data transfer objects for API contracts
- **Dependency Injection**: Loose coupling and testability
- **RESTful API**: Standard HTTP methods and status codes

## Database Schema

### Photos Table
```sql
CREATE TABLE Photos (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    FileName VARCHAR(500) NOT NULL,
    OriginalFileName VARCHAR(500) NOT NULL,
    FilePath VARCHAR(2000) NOT NULL,
    ContentType VARCHAR(100) NOT NULL,
    FileSizeBytes BIGINT NOT NULL,
    Width INT NOT NULL,
    Height INT NOT NULL,
    Description VARCHAR(1000),
    Tags VARCHAR(2000),
    DisplayOrder INT NOT NULL DEFAULT 1,
    IsProfilePhoto BOOLEAN NOT NULL DEFAULT FALSE,
    QualityScore INT NOT NULL DEFAULT 100,
    ModerationStatus ENUM('AUTO_APPROVED', 'PENDING_REVIEW', 'APPROVED', 'REJECTED') NOT NULL DEFAULT 'AUTO_APPROVED',
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    DeletedAt DATETIME NULL,
    
    -- Indexes for performance
    INDEX IX_Photos_UserId (UserId),
    INDEX IX_Photos_CreatedAt (CreatedAt),
    INDEX IX_Photos_ModerationStatus (ModerationStatus),
    INDEX IX_Photos_IsDeleted (IsDeleted),
    INDEX IX_Photos_QualityScore (QualityScore),
    INDEX IX_Photos_UserId_IsDeleted (UserId, IsDeleted),
    
    -- Business rule constraints
    CONSTRAINT CK_Photos_FileSizeBytes CHECK (FileSizeBytes > 0 AND FileSizeBytes <= 10485760),
    CONSTRAINT CK_Photos_Dimensions CHECK (Width > 0 AND Height > 0),
    CONSTRAINT CK_Photos_DisplayOrder CHECK (DisplayOrder > 0),
    CONSTRAINT CK_Photos_QualityScore CHECK (QualityScore >= 1 AND QualityScore <= 100),
    CONSTRAINT CK_Photos_Deletion_Logic CHECK (
        (IsDeleted = 0 AND DeletedAt IS NULL) OR 
        (IsDeleted = 1 AND DeletedAt IS NOT NULL)
    )
);
```

## API Endpoints

### Upload Photos
```http
POST /api/photos/upload
Content-Type: multipart/form-data
Authorization: Bearer {jwt-token}

FormData:
- files: IFormFile[] (Required) - Image files to upload
- description: string (Optional) - Photo description
- tags: string (Optional) - Comma-separated tags
- isProfilePhoto: boolean (Optional) - Mark as profile photo
```

**Response:**
```json
{
  "success": true,
  "message": "3 photos uploaded successfully",
  "data": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "fileName": "processed_image_1.jpg",
      "originalFileName": "my_photo.jpg",
      "contentType": "image/jpeg",
      "fileSizeBytes": 256000,
      "width": 800,
      "height": 600,
      "description": "Profile photo",
      "tags": ["selfie", "outdoor"],
      "displayOrder": 1,
      "isProfilePhoto": true,
      "qualityScore": 95,
      "moderationStatus": "AUTO_APPROVED",
      "urls": {
        "original": "/api/photos/123e4567-e89b-12d3-a456-426614174000/original",
        "large": "/api/photos/123e4567-e89b-12d3-a456-426614174000/large",
        "medium": "/api/photos/123e4567-e89b-12d3-a456-426614174000/medium",
        "thumbnail": "/api/photos/123e4567-e89b-12d3-a456-426614174000/thumbnail"
      },
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-01-15T10:30:00Z"
    }
  ],
  "processingInfo": {
    "totalFiles": 3,
    "successful": 3,
    "failed": 0,
    "warnings": [],
    "processingTimeMs": 1250
  }
}
```

### Get User Photos
```http
GET /api/photos/user/{userId}?includeDeleted=false&page=1&pageSize=20
Authorization: Bearer {jwt-token}
```

### Get Single Photo
```http
GET /api/photos/{id}
Authorization: Bearer {jwt-token}
```

### Download Photo (Different Sizes)
```http
GET /api/photos/{id}/original
GET /api/photos/{id}/large      # 1200x1200 max
GET /api/photos/{id}/medium     # 600x600 max  
GET /api/photos/{id}/thumbnail  # 150x150 max
```

### Update Photo
```http
PUT /api/photos/{id}
Content-Type: application/json
Authorization: Bearer {jwt-token}

{
  "description": "Updated description",
  "tags": ["updated", "tags"],
  "isProfilePhoto": true,
  "displayOrder": 1
}
```

### Delete Photo
```http
DELETE /api/photos/{id}
Authorization: Bearer {jwt-token}
```

### Batch Operations
```http
POST /api/photos/batch/reorder
Content-Type: application/json
Authorization: Bearer {jwt-token}

{
  "photoOrders": [
    { "photoId": "uuid1", "displayOrder": 1 },
    { "photoId": "uuid2", "displayOrder": 2 }
  ]
}
```

## Image Processing Features

### Automatic Processing Pipeline
1. **Validation**: File type, size, dimensions, content validation
2. **Quality Analysis**: Blur detection, brightness analysis, face detection
3. **Size Generation**: Creates 4 sizes automatically:
   - **Original**: Unchanged file
   - **Large**: 1200x1200px max (high quality)
   - **Medium**: 600x600px max (standard quality)  
   - **Thumbnail**: 150x150px max (optimized for lists)
4. **Format Optimization**: Converts to JPEG with optimized compression
5. **Metadata Extraction**: EXIF data removal for privacy

### Quality Scoring Algorithm
```csharp
// Quality factors (weights can be adjusted)
var qualityScore = Math.Max(1, Math.Min(100, 
    (int)((brightnessScore * 0.3) + 
          (sharpnessScore * 0.4) + 
          (colorScore * 0.2) + 
          (compositionScore * 0.1))
));
```

### Content Moderation
- **Automatic**: Basic validation (file type, size, dimensions)
- **Manual Review**: Flagged content goes to moderation queue
- **Status Tracking**: AUTO_APPROVED, PENDING_REVIEW, APPROVED, REJECTED

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3311;Database=photo_service;User=root;Password=root123;"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "DatingApp",
    "Audience": "DatingApp-Users",
    "ExpiryInMinutes": 60
  },
  "PhotoSettings": {
    "MaxFileSize": 10485760,
    "MaxFilesPerUpload": 10,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".webp"],
    "StoragePath": "./storage/photos",
    "TempPath": "./storage/temp",
    "EnableThumbnails": true,
    "EnableMediumSize": true,
    "EnableLargeSize": true,
    "JpegQuality": 85,
    "RequireModeration": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "PhotoService": "Debug"
    }
  }
}
```

### Environment Variables
```bash
# Database
ConnectionStrings__DefaultConnection="Server=photo-service-db;Port=3306;Database=PhotoServiceDb;User=photoservice_user;Password=photoservice_user_password;"

# JWT Authentication  
JwtSettings__SecretKey="your-super-secret-key-that-is-at-least-32-characters-long"
JwtSettings__Issuer="DatingApp"
JwtSettings__Audience="DatingApp-Users"

# Storage
Storage__PhotosPath="/app/wwwroot/uploads/photos"
PhotoSettings__MaxFileSize=10485760
PhotoSettings__MaxFilesPerUpload=10

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://*:5003
```

## Deployment

### Docker Configuration
```dockerfile
# Multi-stage build for optimization
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PhotoService.csproj", "."]
RUN dotnet restore "PhotoService.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "PhotoService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PhotoService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create required directories
RUN mkdir -p /app/wwwroot/uploads/photos/originals && \
    mkdir -p /app/wwwroot/uploads/photos/thumbnails && \
    mkdir -p /app/wwwroot/uploads/photos/medium && \
    mkdir -p /app/wwwroot/uploads/photos/large && \
    mkdir -p /app/wwwroot/uploads/temp

ENTRYPOINT ["dotnet", "PhotoService.dll"]
```

### Database Migration
```bash
# Create migration
dotnet ef migrations add InitialCreate --output-dir Migrations

# Apply migration
dotnet ef database update

# Production deployment
dotnet ef database update --connection "Server=prod-server;Database=PhotoServiceDb;User=user;Password=pass;"
```

## Development Workflow

### Local Development Setup
1. **Prerequisites**:
   ```bash
   # Install .NET 8.0 SDK
   # Install MySQL 8.0
   # Install Entity Framework tools
   dotnet tool install --global dotnet-ef
   ```

2. **Clone and Build**:
   ```bash
   cd /home/m/development/DatingApp/photo-service
   dotnet restore
   dotnet build
   ```

3. **Database Setup**:
   ```bash
   # Start MySQL (port 3311)
   # Run the startup script
   ./start_photo_service.sh
   ```

4. **Run Service**:
   ```bash
   # Development mode
   dotnet run --launch-profile https
   
   # Or use the startup script
   ./start_photo_service.sh
   ```

### Testing

#### Unit Tests Structure
```
photo-service/
├── PhotoService.Tests/
│   ├── Controllers/
│   │   └── PhotosControllerTests.cs
│   ├── Services/
│   │   ├── PhotoServiceTests.cs
│   │   ├── ImageProcessingServiceTests.cs
│   │   └── LocalStorageServiceTests.cs
│   ├── Data/
│   │   └── PhotoRepositoryTests.cs
│   └── TestUtilities/
│       ├── TestDbContext.cs
│       └── MockData.cs
```

#### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "PhotoServiceTests"
```

#### Integration Tests
```bash
# API integration tests
cd /home/m/development/DatingApp
python api_tests.py --service photo

# End-to-end tests
./test_photo_upload_flow.sh
```

## Security Considerations

### Authentication & Authorization
- **JWT Bearer Tokens**: Validates user identity on all protected endpoints
- **User Isolation**: Users can only access their own photos
- **Admin Override**: Special permissions for moderation actions

### Data Protection
- **EXIF Stripping**: Removes metadata that could contain location/device info
- **File Validation**: Prevents malicious file uploads
- **Path Traversal Protection**: Validates file paths to prevent directory traversal
- **Content Type Validation**: Ensures only image files are processed

### Storage Security
- **Isolated Storage**: Photos stored outside web root
- **Secure File Naming**: UUIDs prevent enumeration attacks
- **Size Limits**: Prevents storage exhaustion attacks
- **Rate Limiting**: Prevents abuse (configured in gateway)

## Performance Optimization

### Database Optimizations
- **Proper Indexing**: Optimized queries for common access patterns
- **Connection Pooling**: Efficient database connection management
- **Async Operations**: Non-blocking database operations

### Image Processing
- **Parallel Processing**: Multiple images processed concurrently
- **Memory Management**: Proper disposal of image resources
- **Caching**: Generated sizes cached on disk
- **Lazy Loading**: Images loaded only when requested

### Storage Optimization
- **Directory Structure**: Organized by user and date for efficiency
- **Compression**: Optimized JPEG compression ratios
- **Cleanup**: Automatic cleanup of failed uploads

## Monitoring & Logging

### Structured Logging
```csharp
_logger.LogInformation("Photo uploaded successfully. UserId: {UserId}, PhotoId: {PhotoId}, FileSize: {FileSize}, ProcessingTime: {ProcessingTime}ms", 
    userId, photoId, fileSize, processingTime);
```

### Metrics Tracked
- Upload success/failure rates
- Processing times per image size
- Storage usage per user
- Quality score distributions
- Moderation queue sizes

### Health Checks
```http
GET /health
GET /health/ready
GET /health/live
```

## Integration Points

### With Other Services
- **Auth Service**: JWT token validation
- **User Service**: User profile integration
- **YARP Gateway**: Request routing and load balancing
- **Flutter App**: Direct API consumption

### External Dependencies
- **MySQL Database**: Primary data storage
- **File System**: Photo storage
- **ImageSharp**: Image processing
- **Entity Framework**: ORM

## API Documentation

### OpenAPI/Swagger
- Available at: `https://localhost:7003/swagger`
- Auto-generated from code annotations
- Interactive testing interface
- Export to Postman/Insomnia

### Response Format Standards
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* Response data */ },
  "errors": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Error Handling
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    {
      "field": "files",
      "message": "File size exceeds maximum allowed size of 10MB"
    }
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Future Enhancements

### Phase 2 Features
1. **Cloud Storage Integration**: AWS S3, Azure Blob Storage
2. **CDN Integration**: CloudFront, CloudFlare for global delivery
3. **Advanced Moderation**: AI-powered content analysis
4. **Batch Operations**: Bulk upload/delete operations
5. **Image Analytics**: View tracking, engagement metrics
6. **Real-time Processing**: WebSocket progress updates

### Scalability Improvements
1. **Horizontal Scaling**: Multiple service instances
2. **Database Sharding**: Partition by user or date
3. **Caching Layer**: Redis for metadata caching
4. **Queue System**: RabbitMQ for background processing
5. **Microservice Split**: Separate processing service

## Troubleshooting Guide

### Common Issues

1. **Build Failures**:
   ```bash
   # Clear NuGet cache
   dotnet nuget locals all --clear
   dotnet restore
   dotnet build
   ```

2. **Database Connection Issues**:
   ```bash
   # Check MySQL status
   mysql -h localhost -P 3311 -u root -proot123 -e "SELECT 1;"
   
   # Recreate database
   mysql -h localhost -P 3311 -u root -proot123 -e "DROP DATABASE IF EXISTS photo_service; CREATE DATABASE photo_service;"
   dotnet ef database update
   ```

3. **Storage Permission Issues**:
   ```bash
   # Fix storage permissions
   sudo chmod -R 755 /home/m/development/DatingApp/photo-service/storage
   sudo chown -R $USER:$USER /home/m/development/DatingApp/photo-service/storage
   ```

4. **Image Processing Errors**:
   - Check ImageSharp version compatibility
   - Verify image file formats are supported
   - Check available memory for large images

### Logs Location
- **Development**: Console output + `/home/m/development/DatingApp/photo-service/photo-service.log`
- **Docker**: `docker logs photo-service`
- **Production**: Configured log aggregation system

---

## Conclusion

The Photo Service provides a robust, scalable foundation for photo management in the Dating App ecosystem. It follows industry best practices for security, performance, and maintainability while providing comprehensive features for photo upload, processing, and management.

The service is designed to integrate seamlessly with the existing microservice architecture and can be easily extended with additional features as the application grows.

For questions or issues, refer to the troubleshooting guide or consult the development team.
