# ChatApp

Real-time chat application built with Blazor Server, SignalR, and EF Core (SQLite).

## Features
- Create and delete chats
- Send messages in a chat
- Real-time updates across all connected clients via SignalR
- SQLite persistence with EF Core
- UTC timestamps, DB schema auto-created at startup

## Tech Stack
- .NET 8, ASP.NET Core Blazor Server
- SignalR (server hub at `/chathub`)
- Entity Framework Core 8 + SQLite (`chatapp.db`)
- `IDbContextFactory<AppDbContext>` for safe DbContext usage

## Getting Started
### Prerequisites
- .NET 8 SDK

### Run
- Command line:
  - `dotnet restore`
  - `dotnet run`
- VS Code:
  - Task "Run ChatApp" runs `dotnet run`.

The app will print the listening URL (typically https://localhost:xxxx). Open it in the browser.

### Configuration
- `appsettings.json` connection string:
  ```json
  {
    "ConnectionStrings": {
      "Default": "Data Source=chatapp.db"
    }
  }
  ```
- On startup, the schema is created with `EnsureCreated` (no migrations required).

### Usage
- Home: `/`
- Chats list: `/chats` (create/delete chats, live updates)
- Chat detail: `/chat/{id}` (send messages, live updates)

## How It Works
- Models
  - `Chat { Id, Title, CreatedAtUtc, List<Message> Messages }`
  - `Message { Id, ChatId, Sender, Text, SentAtUtc, Chat? }`
- Data
  - `AppDbContext` configures keys, required fields, UTC DateTime conversion, cascade delete, and an index on `(ChatId, SentAtUtc)`.
- Service
  - `ChatService` performs data operations and emits hub events:
    - `ChatListChanged` after create/delete chat
    - `MessageAdded (chatId)` after sending a message
- Real-time
  - Clients connect to `/chathub`. Pages subscribe to relevant events and reload their data.

## Project Structure (key)
```
Data/           AppDbContext.cs
Hubs/           ChatHub.cs
Models/         Chat.cs, Message.cs
Pages/          Index.razor, Chats.razor, Chat.razor, _Host.cshtml
Services/       ChatService.cs
wwwroot/        static files
Program.cs      app bootstrapping & endpoints
ChatApp.csproj  project file
```

## Development Notes
- SQLite write-ahead log files are ignored (`*.db-wal`, `*.db-shm`).
- Uses `EnsureCreated` for simplicity. For production, consider EF Core migrations.
- Timestamps are stored as UTC and converted on read.

## Roadmap / Limitations
- No authentication/authorization
- Minimal validation
- No pagination/virtualization for large message lists
- Consider Dockerfile, CI, tests, and i18n

## License
Not specified.
