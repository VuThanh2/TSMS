# TSMS — Teaching Schedule Management System

> A modular-monolith academic scheduling platform for Admin, Lecturer, and Student roles — built with Domain-Driven Design, Clean Architecture, and CQRS on top of ASP.NET Core and React.

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
  * [Bounded Contexts](#bounded-contexts)
  * [Cross-Context Communication](#cross-context-communication)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Backend Setup](#backend-setup)
  * [Frontend Setup](#frontend-setup)
- [Configuration](#configuration)
- [Testing](#testing)
- [Deployment](#deployment)

---

## Overview

Most academic scheduling tools split into two extremes: a spreadsheet passed around a department, or a heavyweight Student Information System that requires its own dedicated ops team. **TSMS** sits in between — a right-sized, self-contained system for managing courses, weekly time slots, enrollment, grading, and attendance for a single institution, without the operational overhead of a distributed system it doesn't need.

The system is built as a **Modular Monolith**: four independently modeled Bounded Contexts, each with its own schema and its own Domain/Application/Infrastructure/Presentation layers, deployed as a single process and sharing one SQL Server instance. This gives the project clean domain boundaries and the option to extract a module into its own service later, while avoiding the deployment and consistency overhead a real microservices split would add at this scale.

TSMS covers the full lifecycle of a teaching term:

- Authentication & role-based access control (Admin / Lecturer / Student)
- User management, including bulk creation via CSV import
- Course authoring, weekly recurring time slots, and auto-generated class sessions
- Student enrollment with session selection and capacity limits
- Grading and real-time score notifications (SignalR)
- Attendance tracking per class session
- Reporting & analytics (course statistics, score distribution, attendance rate) via a dedicated read-side context
- Scheduled background jobs (course status transitions, pre-class email reminders) via Hangfire

## Tech Stack

| Layer | Technology                                                  |
|---|-------------------------------------------------------------|
| **Language / Runtime** | C#, .NET 10                                                 |
| **Architecture Style** | Modular Monolith + Clean Architecture + DDD + CQRS          |
| **Mediator** | MediatR                                                     |
| **ORM** | Entity Framework Core                                       |
| **Database** | SQL Server (single instance, one schema per Bounded Context) |
| **Authentication** | ASP.NET Core Identity + JWT                                 |
| **Real-time** | SignalR                                                     |
| **Background Jobs** | Hangfire                                                    |
| **Reliability Pattern** | Transactional Outbox (per-schema)                           |
| **Frontend Framework** | React + TypeScript, built with Vite                         |
| **Frontend State/Data** | TanStack Query, Axios                                       |
| **UI Library** | Ant Design                                                  |
| **Charts** | ECharts                                                     |
| **Styling** | Tailwind CSS                                       |
| **Package Manager** | pnpm                                                        |
| **Containerization** | Docker & Docker Compose                                     |

## Architecture

### Bounded Contexts

TSMS is composed of **4 Bounded Contexts**, each fully layered (Domain → Application → Infrastructure → Presentation) and owning its own database schema, sharing a single SQL Server database.

| Context | Primary Aggregate | Responsibility                                                                                           |
|---|---|----------------------------------------------------------------------------------------------------------|
| **Identity** | `User` (+ `LecturerProfile` / `StudentProfile`) | Authentication, role-based authorization, user CRUD, CSV bulk import                                     |
| **CourseManagement** | `Course` (+ `WeeklySlot`, `ClassSession`) | Course authoring, recurring weekly time slots, auto-generated class sessions, enrollment gating, grading |
| **EnrollmentManagement** | `Enrollment` (+ `EnrolledSession`, `Attendance`) | Student enrollment, session selection, per-session attendance                                            |
| **Reporting** | Read Models | Event-driven, read-only projections for dashboards and statistical reports                               |

`Reporting` never writes to another context's data — it is built entirely from Domain Events projected off the other three contexts' Outbox processors.

### Cross-Context Communication

Each Bounded Context owns its own **Outbox table** in its own schema; there is no single shared Outbox table. Communication across contexts follows two rules:

- **Synchronous, in-process contracts**: the *consuming* context defines the interface it needs, and the *owning* context implements it in its own Infrastructure layer. There is no direct cross-context `DbContext` injection.
- **Asynchronous, event-driven projection**: state changes are raised as Domain Events, persisted via each context's own Outbox, and consumed by `Reporting`'s event handlers to build denormalized read models — keeping reporting queries fast without coupling it to the other contexts' write models.

## Project Structure

```
TSMS/
├── src/
│   ├── Api/
│   │   └── TSMS.Api/                        # Composition root — Program.cs, middleware, Hangfire
│   │
│   ├── BuildingBlocks/
│   │   ├── SharedKernel/                     # Cross-cutting Domain primitives (Entity, ValueObject, Result, Error)
│   │   └── SharedInfrastructure/             # Outbox contracts, persistence helpers, time abstraction
│   │
│   ├── Modules/
│   │   ├── Identity/
│   │   │   ├── Identity.Domain/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Infrastructure/
│   │   │   └── Identity.Presentation/
│   │   ├── Course/
│   │   │   ├── CourseManagement.Domain/
│   │   │   ├── CourseManagement.Application/
│   │   │   ├── CourseManagement.Infrastructure/
│   │   │   └── CourseManagement.Presentation/
│   │   ├── Enrollment/
│   │   │   ├── EnrollmentManagement.Domain/
│   │   │   ├── EnrollmentManagement.Application/
│   │   │   ├── EnrollmentManagement.Infrastructure/
│   │   │   └── EnrollmentManagement.Presentation/
│   │   └── Reporting/
│   │       ├── Reporting.Domain/
│   │       ├── Reporting.Application/
│   │       ├── Reporting.Infrastructure/
│   │       └── Reporting.Presentation/
│   │
│   └── Migrator/
│       └── TSMS.DbMigrator/                  # Standalone console app: applies EF Core migrations, seeds roles + default Admin
│
├── client/                                   # React + TypeScript frontend (Vite, pnpm)
│   └── src/
│
├── tests/
│   ├── Identity.UnitTests/
│   ├── Course.UnitTests/
│   ├── Enrollment.UnitTests/
│   └── Reporting.UnitTests/
│
├── docs/                                    
├── docker-compose.yml                        # Local SQL Server for development
├── Dockerfile                                # Backend image (used for Railway deployment)
└── TSMS.slnx                                 # Solution file
```

Within each use case folder (e.g. `Courses/CreateCourse/`), the convention is 3 files: `XxxCommand.cs` / `XxxQuery.cs` (record + handler), `XxxValidator.cs`, and `XxxDto.cs` (input/output DTOs) — keeping each vertical slice self-contained and easy to navigate.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20 LTS](https://nodejs.org/) (managed via `nvm` recommended) + [pnpm](https://pnpm.io/)
- Docker & Docker Compose (for local SQL Server)

### Backend Setup

**1. Start SQL Server**

A `docker-compose.yml` is provided at the repo root to spin up a local SQL Server instance:

```bash
# Set SA_PASSWORD in your environment first
docker compose up -d
```

**2. Configure connection strings & secrets**

`appsettings.json` is committed with placeholder values only (`YOUR_PASSWORD_HERE`, `YOUR_SECRET_KEY_HERE`, ...). 
Create an `appsettings.Development.json` next to it in `src/Api/TSMS.Api/` with your real local values. See [Configuration](#configuration) below for the required sections.

**3. Apply migrations & seed data**

`TSMS.DbMigrator` is a standalone console app that applies EF Core migrations across all four schemas and seeds the fixed roles (`Admin`, `Lecturer`, `Student`) plus a default Admin account:

```bash
dotnet run --project src/Migrator/TSMS.DbMigrator 
```

The migrator is idempotent — safe to re-run on an already-migrated database.

**4. Run the API**

```bash
dotnet run --project src/Api/TSMS.Api --launch-profile https
```

The API exposes Swagger/OpenAPI in Development, and a Hangfire dashboard at `/hangfire` for monitoring background jobs.

### Frontend Setup

```bash
cd client
pnpm install
pnpm run dev
```

The dev server runs on `http://localhost:5173` by default — make sure this origin is present in the backend's `Cors:AllowedOrigins`.

## Configuration

Key configuration sections (schema lives in `appsettings.json` with placeholder values; real values are supplied via `appsettings.Development.json` locally, or environment variables in deployment):

| Section | Purpose |
|---|---|
| `ConnectionStrings` | One connection string per schema: `IdentityDb`, `CourseDb`, `EnrollmentDb`, `ReportingDb` (all can point at the same database) |
| `Jwt` | Token signing key, issuer, audience, expiry |
| `Cors:AllowedOrigins` | Origins allowed to call the API (the Vite dev server, and the deployed frontend URL) |
| `DefaultAdmin` | Email/password/full name for the Admin account seeded on first run |
| `Email` | SMTP settings used for grade-update and pre-class reminder notifications |
| `Demo:EnableReset` | Feature flag gating the `POST /api/dev/reset-demo-data` endpoint (Admin-only, used for demo resets) |

Secrets should never be committed. Use one of them (or environment variables) in place of the placeholder values checked into `appsettings.json`.

## Testing

Unit tests are split per module under `tests/`:

```bash
dotnet test tests/Identity.UnitTests
dotnet test tests/Course.UnitTests
dotnet test tests/Enrollment.UnitTests
dotnet test tests/Reporting.UnitTests
```

## Deployment

- **Frontend** deploys to **Vercel**.
- **Backend + SQL Server** deploy to **Railway**, using the root `Dockerfile` (build context is the full repo; `client/` and `tests/` are excluded via `.dockerignore`). The container listens on `ASPNETCORE_URLS=http://+:8080`, with TLS terminated at Railway's edge.
- Demo data can be reset on the deployed environment via the gated `POST /api/dev/reset-demo-data` endpoint (see [Configuration](#configuration)).
