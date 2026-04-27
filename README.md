# photo-service

Media handling service for profile photos and voice/media-adjacent features in the DatingApp platform.

## What It Does

- Photo upload and retrieval
- Image processing and resizing
- Media metadata persistence
- Moderation and safety-related media controls

## Why It Is Interesting

This repo showcases:
- File and media workflows in a microservice context
- CPU-bound image processing integration in web APIs
- Data + file system consistency concerns
- Testable service layering around storage and processing

## Stack

- .NET 8
- ASP.NET Core Web API
- EF Core 8 + MySQL
- ImageSharp

## Project Layout

```text
photo-service/
  Controllers/
  Services/
  Data/
  Models/
  DTOs/
  PhotoService.Tests/
  wwwroot/uploads/photos/
```

## Build and Test

```bash
dotnet restore PhotoService.csproj
dotnet build PhotoService.csproj
dotnet test PhotoService.Tests/PhotoService.Tests.csproj
```

## Run Locally

```bash
dotnet run --project PhotoService.csproj
```

## Typical Endpoints

- Upload photo
- Get image by photo id
- Reorder/remove photos
- Voice prompt/media-related routes (if enabled)

## Related Repositories

- `best-koder-org/UserService`
- `best-koder-org/mobile_dejtingapp`
- `best-koder-org/dejting-yarp`

## Status

Active development repository used by the current client and backend platform.
