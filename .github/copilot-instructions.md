# photo-service
.NET 8 photo storage and moderation service.
## Build & Test
```bash
dotnet restore PhotoService.csproj && dotnet build && dotnet test PhotoService.Tests/PhotoService.Tests.csproj
```
## Architecture
- Controllers: PhotosController, VoiceMessagesController, VoicePromptsController
- EF Core 8 with MySQL (PhotoContext)
- ImageSharp for image processing
- Tests use InMemoryDatabase + Moq
## Rules
- All new code must have unit tests
