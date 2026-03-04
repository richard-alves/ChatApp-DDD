# ChatApp Solution Structure

## Projects
- ChatApp.Domain (Class Library) - Entities, Value Objects, Domain Events, Interfaces
- ChatApp.Application (Class Library) - Use Cases, DTOs, Interfaces, CQRS
- ChatApp.Infrastructure (Class Library) - EF Core, RabbitMQ, Outbox, External Services
- ChatApp.Api (ASP.NET Core Web API) - Controllers, Hubs, Auth
- ChatApp.Bot (Worker Service) - RabbitMQ Consumer, Stock API
- ChatApp.Tests (xUnit) - Unit Tests
