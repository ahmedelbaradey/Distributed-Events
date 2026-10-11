# Distributed Event Processing Reliability Lab

This repository contains a pragmatic distributed event-processing lab built with .NET 8, PostgreSQL, RabbitMQ, Docker Compose, and Azure-style operational patterns.

## Start the environment

```bash
docker compose up --build -d
```

## Run the generator

```bash
dotnet run --project src/EventGenerator/EventGenerator.csproj
```

## Run tests

```bash
dotnet test
```

## Health endpoints

- `GET /health`
- `POST /api/events`
- `GET /api/events/summary`

## Notes

This lab keeps the core architecture boundaries explicit while remaining runnable and testable in a local development environment.
