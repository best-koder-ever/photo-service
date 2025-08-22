# Photo Service - Dating App

## Overview

The **Photo Service** is a dedicated microservice for handling photo upload, storage, processing, and management in the Dating App ecosystem. It provides secure, scalable photo operations with automatic image processing, multiple size generation, and content moderation capabilities.

## 🏗️ Architecture

### Technology Stack
- **.NET 8.0** - Modern web API framework
- **Entity Framework Core** - ORM with MySQL support
- **ImageSharp** - High-performance image processing
- **JWT Authentication** - Secure API access
- **MySQL Database** - Photo metadata storage
- **Local File Storage** - Organized file system storage

### Design Patterns
- **Microservice Architecture** - Independent, focused service
- **Repository Pattern** - Data access abstraction
- **Service Layer Pattern** - Business logic separation
- **Dependency Injection** - Loose coupling and testability
- **DTO Pattern** - Data transfer objects for API contracts

## 🚀 Features

### Core Functionality
- ✅ **Photo Upload** - Multi-format support (JPEG, PNG, WebP)
- ✅ **Image Processing** - Automatic resize, optimization, quality analysis
- ✅ **Multiple Sizes** - Full, medium (400px), thumbnail (150px)
- ✅ **Metadata Management** - Display order, primary photo, timestamps
- ✅ **Content Moderation** - Quality scoring and approval workflow
- ✅ **Secure Storage** - User-organized directory structure

### Security Features
- 🔐 **JWT Authentication** - Secure API access
- 🔐 **Ownership Validation** - Users can only access their photos
- 🔐 **File Validation** - Format, size, and content verification
- 🔐 **Path Security** - Prevention of directory traversal attacks

### Performance Features
- ⚡ **Caching Headers** - Browser caching for image delivery
- ⚡ **Range Requests** - Efficient large file serving
- ⚡ **Optimized Processing** - Smart resizing algorithms
- ⚡ **Async Operations** - Non-blocking I/O operations

## 📁 Project Structure

```
photo-service/
├── Controllers/
│   └── PhotosController.cs        # REST API endpoints
├── Data/
│   └── PhotoContext.cs           # Entity Framework context
├── DTOs/
│   └── PhotoDTOs.cs              # Data transfer objects
├── Models/
│   └── Photo.cs                  # Photo entity model
├── Services/
│   ├── IServices.cs              # Service interfaces
│   ├── PhotoService.cs           # Core business logic
│   ├── ImageProcessingService.cs # Image manipulation
│   └── LocalStorageService.cs    # File storage operations
├── wwwroot/uploads/photos/       # Photo storage directory
├── appsettings.json              # Production configuration
├── appsettings.Development.json  # Development configuration
├── Dockerfile                    # Container configuration
├── PhotoService.csproj           # Project file
├── Program.cs                    # Application startup
└── run_photo_service.sh          # Development startup script
```

## 🔧 Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DatingApp_Photos;Uid=root;Pwd=password123;Port=3306;"
  }
}
```

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "your-secure-secret-key-here",
    "Issuer": "DatingApp",
    "Audience": "DatingApp-Users",
    "ExpirationMinutes": 60
  }
}
```

### Storage Configuration
```json
{
  "Storage": {
    "PhotosPath": "wwwroot/uploads/photos"
  }
}
```

## 🚀 Getting Started

### Prerequisites
- **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **MySQL Server** - Running on localhost:3306
- **Git** - For version control

### Quick Start

1. **Clone and Navigate**
   ```bash
   cd /home/m/development/DatingApp/photo-service
   ```

2. **Start the Service**
   ```bash
   ./run_photo_service.sh
   ```

3. **Access the API**
   - **API Base URL**: http://localhost:5003
   - **Swagger UI**: http://localhost:5003/swagger
   - **Health Check**: http://localhost:5003/health

### Manual Setup

1. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

2. **Build Application**
   ```bash
   dotnet build
   ```

3. **Run Service**
   ```bash
   dotnet run
   ```

## 📚 API Documentation

### Authentication
All endpoints (except image serving) require JWT Bearer token:
```
Authorization: Bearer <jwt_token>
```

### Core Endpoints

#### Upload Photo
```http
POST /api/photos
Content-Type: multipart/form-data

{
  "photo": <file>,
  "displayOrder": 1,
  "isPrimary": true
}
```

#### Get User Photos
```http
GET /api/photos
```

#### Get Specific Photo
```http
GET /api/photos/{id}
```

#### Get Photo Image
```http
GET /api/photos/{id}/image?size=medium
```
- **Sizes**: `full`, `medium`, `thumbnail`
- **Anonymous access** allowed for public viewing

#### Update Photo
```http
PUT /api/photos/{id}
Content-Type: application/json

{
  "displayOrder": 2,
  "isPrimary": false
}
```

#### Delete Photo
```http
DELETE /api/photos/{id}
```

### Response Format

#### Success Response
```json
{
  "success": true,
  "photo": {
    "id": 1,
    "userId": 123,
    "originalFileName": "profile.jpg",
    "displayOrder": 1,
    "isPrimary": true,
    "createdAt": "2024-01-20T10:30:00Z",
    "width": 800,
    "height": 600,
    "fileSizeBytes": 245760,
    "moderationStatus": "AUTO_APPROVED",
    "qualityScore": 92,
    "urls": {
      "full": "/api/photos/1/image",
      "medium": "/api/photos/1/medium",
      "thumbnail": "/api/photos/1/thumbnail"
    }
  }
}
```

#### Error Response
```json
{
  "success": false,
  "errorMessage": "File size exceeds maximum limit of 10 MB"
}
```

## 🖼️ Image Processing

### Supported Formats
- **JPEG** (.jpg, .jpeg) - Most common format
- **PNG** (.png) - Transparency support
- **WebP** (.webp) - Modern compression

### Processing Pipeline
1. **Validation** - Format, size, content verification
2. **Optimization** - Smart resizing with aspect ratio preservation
3. **Quality Analysis** - Automated quality scoring (1-100)
4. **Multi-Size Generation** - Full, medium, thumbnail versions
5. **Format Conversion** - Optimization to best format

### Size Specifications
- **Full Size**: Max 800x800px, optimized quality
- **Medium Size**: Max 400x400px, balanced quality/size
- **Thumbnail**: Max 150x150px, optimized for lists

### Quality Scoring Factors
- **Resolution** - Higher resolution = better score
- **Aspect Ratio** - Standard ratios preferred
- **File Size** - Optimal size range considered
- **Sharpness** - Edge detection analysis
- **Format** - Modern formats preferred

## 💾 Storage Organization

### Directory Structure
```
wwwroot/uploads/photos/
├── 123/                    # User ID directory
│   ├── 123_20240120_a1b2c3d4.jpg      # Full size
│   ├── 123_20240120_a1b2c3d4_medium.jpg # Medium size
│   └── 123_20240120_a1b2c3d4_thumb.jpg  # Thumbnail
└── 456/                    # Another user
    └── ...
```

### File Naming Convention
- **Pattern**: `{userId}_{timestamp}_{uniqueId}[_{size}].{extension}`
- **Example**: `123_20240120_143052_a1b2c3d4_medium.jpg`
- **Benefits**: Prevents conflicts, enables cleanup, supports multiple sizes

## 🔒 Security

### Authentication & Authorization
- **JWT Bearer Tokens** - Industry standard authentication
- **User Ownership** - Photos accessible only by owners
- **Role-Based Access** - Admin endpoints for moderation

### File Security
- **Format Validation** - Only image files accepted
- **Size Limits** - 10MB maximum per file
- **Content Verification** - Actual image format validation
- **Path Security** - Directory traversal prevention

### Data Protection
- **Soft Deletion** - Photos marked as deleted, not immediately removed
- **Audit Trail** - Creation, update, deletion timestamps
- **Moderation Workflow** - Content review process

## 🔧 Configuration Options

### Photo Limits
```csharp
public static class PhotoConstants
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;  // 10MB
    public const int MaxPhotosPerUser = 6;                   // 6 photos max
    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
}
```

### Image Sizes
```csharp
public static class ImageSizes
{
    public const int ThumbnailWidth = 150;   // Thumbnail size
    public const int ThumbnailHeight = 150;
    
    public const int MediumWidth = 400;      // Medium size
    public const int MediumHeight = 400;
    
    public const int LargeWidth = 800;       // Full size limit
    public const int LargeHeight = 800;
}
```

## 🐳 Docker Deployment

### Build Container
```bash
docker build -t dating-app/photo-service .
```

### Run Container
```bash
docker run -d -p 5003:5003 --name photo-service \
  -e ConnectionStrings__DefaultConnection="Server=mysql;Database=DatingApp_Photos;Uid=root;Pwd=password123;" \
  -e JwtSettings__SecretKey="your-production-secret-key" \
  dating-app/photo-service
```

### Environment Variables
- `ConnectionStrings__DefaultConnection` - Database connection
- `JwtSettings__SecretKey` - JWT signing key
- `Storage__PhotosPath` - Photo storage path
- `ASPNETCORE_ENVIRONMENT` - Environment (Production/Development)

## 🧪 Testing

### Unit Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests
```bash
# Test with real database
dotnet test --configuration Integration
```

### API Testing
Use the provided **Postman collection** or **Swagger UI** for API testing.

## 📊 Monitoring

### Health Checks
```http
GET /health
```

### Logging
- **Structured Logging** - JSON format in production
- **Log Levels** - Debug in development, Info in production
- **Performance Metrics** - Processing times, file sizes

### Metrics
- Photo upload success/failure rates
- Processing times by image size
- Storage usage by user
- Quality score distributions

## 🔄 Integration

### With Other Services
- **Auth Service** - JWT token validation
- **User Service** - User profile integration
- **Matchmaking Service** - Photo URLs for profiles
- **YARP Gateway** - Request routing and load balancing

### Frontend Integration
- **Flutter App** - Photo upload and gallery display
- **REST API** - Standard HTTP/JSON communication
- **Image URLs** - Direct browser access to images

## 🐛 Troubleshooting

### Common Issues

**Issue**: Photos not uploading
- Check file size (max 10MB)
- Verify file format (JPEG, PNG, WebP)
- Ensure JWT token is valid

**Issue**: Images not displaying
- Check photo URLs are accessible
- Verify storage directory permissions
- Check database photo records

**Issue**: Database connection errors
- Verify MySQL is running
- Check connection string configuration
- Ensure database exists

### Debug Mode
```bash
export ASPNETCORE_ENVIRONMENT=Development
./run_photo_service.sh
```

### Logs Location
- **Console Output** - Development environment
- **File Logs** - Production environment
- **Structured Logs** - JSON format for analysis

## 📈 Performance

### Optimization Features
- **Async Processing** - Non-blocking operations
- **Image Caching** - Browser cache headers
- **Optimized Resizing** - Lanczos3 resampling
- **Format Conversion** - WebP/JPEG optimization

### Scaling Considerations
- **Horizontal Scaling** - Multiple service instances
- **Load Balancing** - Through YARP gateway
- **Storage Scaling** - Cloud storage integration
- **CDN Integration** - Global image delivery

## 🤝 Contributing

### Code Standards
- **C# Conventions** - Microsoft guidelines
- **Documentation** - XML comments for public APIs
- **Testing** - Unit tests for business logic
- **Security** - Input validation, secure practices

### Development Workflow
1. Create feature branch
2. Implement with tests
3. Update documentation
4. Submit pull request
5. Code review and merge

## 📞 Support

### Documentation
- **API Documentation** - Swagger UI at `/swagger`
- **Code Comments** - Comprehensive inline documentation
- **README Files** - Service-specific guides

### Contact
- **Development Team** - Dating App developers
- **Issue Tracking** - GitHub issues
- **Documentation** - This README and inline comments

---

**Photo Service v1.0** - Part of the Dating App Microservices Architecture
