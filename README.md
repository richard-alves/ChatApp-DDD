# ChatApp — .NET Chat Application

A real-time browser-based chat application built with **Clean Architecture**, **DDD**, **CQRS**, **Outbox Pattern**, and **.NET Identity**. Users can chat in multiple rooms and query live stock prices via a decoupled bot.

---

## ✅ Features Implemented

### Mandatory
- [x] User registration & login (JWT + .NET Identity)
- [x] Multiple chatrooms (bonus) with real-time messaging via **SignalR**
- [x] Stock command `/stock=stock_code` dispatched via **RabbitMQ**
- [x] Decoupled **StockBot** worker service that fetches CSV from stooq.com
- [x] Bot posts quote message back to chatroom: `"AAPL.US quote is $150.42 per share."`
- [x] Messages ordered by timestamp — last 50 only
- [x] Unit tests (Domain + Application + Infrastructure)

### Bonus
- [x] **Multiple chatrooms** (General, Tech Talk, Random — seeded automatically)
- [x] **.NET Identity** for authentication
- [x] **Bot error handling** — graceful messages when stock unavailable
- [x] **Docker Compose installer** for one-command startup

---

## Architecture

```
ChatApp/
├── src/
│   ├── ChatApp.Domain/           # Entities, Value Objects, Domain Events, Interfaces
│   ├── ChatApp.Application/      # CQRS Commands/Queries, MediatR, FluentValidation
│   ├── ChatApp.Infrastructure/   # EF Core, RabbitMQ, Outbox, Identity, JWT
│   ├── ChatApp.Api/              # ASP.NET Core 8 REST API + SignalR Hub
│   └── ChatApp.Bot/              # Worker Service — RabbitMQ consumer + Stock API
├── tests/
│   └── ChatApp.Tests/            # xUnit + Moq + FluentAssertions
└── frontend/
    └── index.html                # Simple browser client
```

### Key Patterns
| Pattern | Where Used |
|---------|-----------|
| **Clean Architecture** | Domain → Application → Infrastructure → API |
| **DDD Aggregates** | `ChatRoom` aggregate with `Message` child entities |
| **CQRS** | Separate Commands/Queries via MediatR |
| **Outbox Pattern** | Domain events serialized to `OutboxMessages` table in same transaction |
| **Repository Pattern** | `IChatRoomRepository`, `IMessageRepository` |
| **Pipeline Behaviors** | `ValidationBehavior<T>` for automatic FluentValidation |
| **Result Pattern** | `Result<T>` for explicit success/failure without exceptions |

---

## Prerequisites

- .NET 8 SDK
- Docker & Docker Compose (or: SQL Server + RabbitMQ locally)

---

## 🚀 Quick Start (Docker)

```bash
# 1. Clone / unzip the project
cd ChatApp

# 2. Start all dependencies + services
docker-compose up -d

# 3. Open frontend in browser
open frontend/index.html
# Or serve it: npx serve frontend/
```

The API will be at `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`  
RabbitMQ Management: `http://localhost:15672` (guest/guest)

---

## 🏃 Manual Start (without Docker)

### 1. Start dependencies
```bash
# SQL Server (Docker)
docker run -e SA_PASSWORD=ChatApp@123! -e ACCEPT_EULA=Y -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# RabbitMQ (Docker)
docker run -p 5672:5672 -p 15672:15672 -d rabbitmq:3.13-management
```

### 2. Update connection strings
Edit `src/ChatApp.Api/appsettings.json` and `src/ChatApp.Bot/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatAppDb;User Id=sa;Password=ChatApp@123!;TrustServerCertificate=True;"
  }
}
```

### 3. Run the API (auto-migrates DB)
```bash
cd src/ChatApp.Api
dotnet run
```

### 4. Run the Bot
```bash
cd src/ChatApp.Bot
dotnet run
```

### 5. Open frontend
Open `frontend/index.html` in your browser.

---

## Running Tests

```bash
cd tests/ChatApp.Tests
dotnet test
```

---

## Using the App

1. Open `frontend/index.html` in **two browser windows**
2. **Register** two different users (or login with existing)
3. Both users join the same room automatically
4. Send messages — they appear in real-time
5. Try the stock command: `/stock=aapl.us`
   - The message is sent to RabbitMQ (NOT saved to DB)
   - StockBot fetches the CSV, parses the price, posts: `"AAPL.US quote is $X.XX per share."`

---

## Security Notes

- JWT secret must be changed in production (environment variable / secrets manager)
- Connection strings should use environment variables in production
- The `appsettings.json` secrets are for development only
- Use `dotnet user-secrets` locally: `dotnet user-secrets set "JwtSettings:Secret" "your-secret"`

---

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | None | Register user |
| POST | `/api/auth/login` | None | Login, get JWT |
| GET | `/api/chatrooms` | JWT | List all rooms |
| POST | `/api/chatrooms` | JWT | Create room |
| GET | `/api/chatrooms/{id}/messages` | JWT | Get last 50 messages |
| POST | `/api/chatrooms/{id}/messages` | JWT | Post message |
| WS | `/hubs/chat?access_token=...` | JWT | SignalR hub |

---

## SignalR Hub Events

| Client → Server | Server → Client |
|----------------|----------------|
| `JoinRoom(roomId)` | `ReceiveMessage(notification)` |
| `LeaveRoom(roomId)` | `UserJoined(info)` |
| | `UserLeft(info)` |
