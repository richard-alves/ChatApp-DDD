# ChatApp

A real-time browser-based chat application built with .NET 8, Clean Architecture, DDD, CQRS, and the Outbox Pattern.

---

## Getting Started

### Prerequisites
- Visual Studio 2022 or later
- Docker Desktop

### 1. Start RabbitMQ
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management
```

### 2. Configure Multiple Startup Projects
- Right click the Solution in Solution Explorer
- Properties → Common Properties → Startup Project
- Select **Multiple Startup Projects**
- Set `ChatApp.Api` → **Start**
- Set `ChatApp.Bot` → **Start**
- Click OK

### 3. Run
Press **F5** — both API and Bot will start automatically.

The browser will open with the chat application ready to use.

---

## Running Tests

```bash
cd tests/ChatApp.Tests
dotnet test
```

---

## Mandatory Features

- [x] Registered users can log in and talk in a chatroom
- [x] Users can post stock commands in the format `/stock=stock_code`
- [x] Decoupled bot that calls the stooq.com API and parses the CSV response
- [x] Bot sends the stock quote back to the chatroom via RabbitMQ
- [x] Messages ordered by timestamp — last 50 only
- [x] Unit tests

## Bonus Features

- [x] Multiple chatrooms (General, Tech Talk, Random — seeded automatically)
- [x] .NET Identity for user authentication
- [x] Bot error handling — graceful messages when stock is unavailable or an exception occurs

---

## Architecture

```
ChatApp/
├── src/
│   ├── ChatApp.Domain/           # Entities, Interfaces, Exceptions
│   ├── ChatApp.Application/      # CQRS Commands/Queries, Handlers, Validators
│   ├── ChatApp.Infrastructure/   # EF Core, RabbitMQ, Outbox, Identity, JWT
│   ├── ChatApp.Api/              # ASP.NET Core 8 REST API + SignalR Hub
│   └── ChatApp.Bot/              # Worker Service — RabbitMQ consumer + Stock API
├── tests/
│   └── ChatApp.Tests/            # xUnit + Moq + FluentAssertions
└── frontend/
    └── index.html                # Simple browser client
```

### Patterns and Practices

| Pattern | Where |
|---------|-------|
| Clean Architecture | Domain → Application → Infrastructure → API |
| DDD | `ChatRoom` and `Message` entities with business rules |
| CQRS | Separate Commands and Queries via MediatR |
| Outbox Pattern | Stock commands saved to `OutboxMessages` table before publishing to RabbitMQ |
| Repository Pattern | `IChatRoomRepository`, `IMessageRepository`, `IOutboxRepository` |
| Pipeline Behavior | `ValidationBehavior<T>` runs FluentValidation before every handler |
| Result Pattern | `Result<T>` for explicit success/failure without exceptions |

---

## Stock Command Flow

```
User types /stock=aapl.us
        ↓
API saves to OutboxMessages table
        ↓
OutboxProcessor (every 3s) reads and publishes to RabbitMQ
        ↓
StockBot consumes the message
        ↓
Fetches CSV from stooq.com and parses the price
        ↓
Calls API with the result
        ↓
API saves the bot message and notifies via SignalR
        ↓
All users in the room see: "AAPL.US quote is $152.50 per share."
```

> The stock command is **not saved** to the database — only the bot's response is.

---

## Prerequisites

- .NET 8 SDK
- RabbitMQ (via Docker)

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
| POST | `/api/chatrooms/{id}/messages/bot` | None | Bot response endpoint |