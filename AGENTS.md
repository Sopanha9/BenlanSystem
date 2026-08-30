# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

BenLanSystem is a bus transportation booking management system for Cambodia, built with ASP.NET Core MVC targeting **.NET 10.0**. It is a hybrid MVC + API application: Razor views render the customer-facing site and admin UI, while API controllers under `/api/*` serve data to those views and external consumers.

**Stack:**
- ASP.NET Core MVC + Razor Views (no frontend framework — vanilla JS + Bootstrap)
- Entity Framework Core 10 with SQL Server
- ASP.NET Core Identity with `long` primary keys (custom user entity: `Staff`)
- AutoMapper for DTO mapping
- Swagger/Swashbuckle for API documentation (Development only)

## Repository Layout

```
BenlanSystem/                    # repo root
├── BenLanSystem.sln
├── docker-compose.yml           # SQL Server (db) + app containers
├── BenLan_Database.sql          # legacy SQL script (migrations are the source of truth)
├── CLAUDE.md
└── BenLanSystem/                # the web project
    ├── Program.cs               # app bootstrap, DI registration, DB seeding
    ├── Controllers/
    │   ├── HomeController.cs    # customer-facing pages
    │   ├── AccountController.cs # login/register
    │   ├── Admin/               # [Area("Admin")] MVC controllers
    │   └── Api/                 # [ApiController] REST endpoints
    ├── Services/
    │   ├── Interfaces/          # ILocationService, IRouteService, ITripService,
    │   │                        # IVehicleService, IBookingService, IPaymentService,
    │   │                        # IStaffService, IBlogService, IDashboardService
    │   └── Implementations/
    ├── Models/
    │   ├── Entities/            # EF Core entities
    │   ├── DTOs/                # request/response DTOs
    │   └── *ViewModel.cs        # MVC view models (Login, Register, Error)
    ├── Data/
    │   ├── ApplicationDbContext.cs
    │   └── ApplicationDbContextFactory.cs
    ├── Mappings/MappingProfile.cs
    ├── Migrations/
    ├── Views/                   # customer-facing views (Home, Account, Shared)
    ├── Areas/Admin/Views/       # admin views (Admin, Bookings, Locations, Payments,
    │                            # Routes, Staff, Trips, Vehicles)
    └── wwwroot/                 # css, js, lib (Bootstrap/jQuery), designs (images)
```

## Development Commands

```bash
docker compose up -d db          # start SQL Server container (required first)
dotnet build                     # build (run from repo root)
dotnet run --project BenLanSystem   # dev server: http://localhost:5216
dotnet ef migrations add <Name> --project BenLanSystem
dotnet ef database update --project BenLanSystem
docker compose up --build        # run full stack in Docker
docker compose down              # stop containers
```

**There is no test project.** Verify changes manually via the browser or Swagger UI (`/swagger` in Development).

## Local Setup

1. Docker Desktop must be installed and running.
2. `docker compose up -d db` starts SQL Server 2022 on `localhost:1433`.
3. Dev connection string (`appsettings.Development.json`): `Server=localhost,1433;Database=BenLanDB_Dev;User Id=sa;Password=BenLan@Dev2026!;TrustServerCertificate=True;Encrypt=False`.
4. On startup, `Program.cs` runs `MigrateAsync()` then seeds automatically (non-fatal if DB is unavailable):
   - Roles: `Admin`, `Staff`, `Customer`, `Driver`
   - Admin user: `admin@benlan.com` / `Admin@123456`
   - Cambodian locations, routes, vehicles, upcoming trips, and blog posts
   - Past trips are deleted and re-seeded on each start so trip dates stay in the future

## Domain Model

Core flow: **Location → Route → Trip → Booking → Payment**

| Entity | Key facts |
|---|---|
| `Location` | Unique `Name`; Cambodian cities/provinces |
| `Route` | `StartLocationId` → `EndLocationId` (unique pair, must differ); `DistanceKm`, `EstimatedMinutes` |
| `Vehicle` | Unique `PlateNumber`; `StatusName` ∈ {Active, Maintenance, Retired}; check constraints on Transmission/FuelType |
| `Trip` | Belongs to Route + Vehicle; `BasePrice`, `AvailableSeats`, `StatusName` ∈ {Open, Closed, Cancelled, Completed}; has `[Timestamp] RowVersion` for optimistic concurrency |
| `Booking` | Belongs to Trip + Customer(`Staff`); `TotalAmount` is a **computed column** (`SeatsBooked * UnitPrice`); `BookingStatus` ∈ {Pending, Confirmed, Cancelled, Completed} |
| `BookingPassenger` | Unique (`BookingId`, `SeatNumber`) |
| `Payment` | `PaymentMethod` ∈ {ABA, ACLEDA, Wing, Cash, Card}; `PaymentStatus` ∈ {Pending, Paid, Failed, Refunded} |
| `*History` tables | Audit tables for Vehicle, Booking, Payment |
| `BlogPost` | Content entity authored by `Staff` |
| `Staff` | The Identity user (`IdentityUser<long>`); serves as admin, staff, and customer depending on role |

## Architecture Conventions

- **Service layer**: all business logic lives in `Services/Implementations/`, behind interfaces in `Services/Interfaces/`. Registered scoped in `Program.cs`. Controllers should call services, not the DbContext directly.
- **DTOs**: defined in `Models/DTOs/`, mapped via AutoMapper (`Mappings/MappingProfile.cs`). API controllers return DTOs, not entities.
- **Admin area**: controllers in `Controllers/Admin/` use `[Area("Admin")]` and `[Authorize(Roles = "Admin,Staff")]`. Views live in `Areas/Admin/Views/` with layout `_LayoutAdmin.cshtml`.
- **Routing**: `areas` route is mapped before the default route in `Program.cs` — keep that order.
- **Concurrency**: `Trip.RowVersion` is used for optimistic concurrency — handle `DbUpdateConcurrencyException` when updating trips.
- **Auth**: customer identity is read from `ClaimTypes.NameIdentifier` in API controllers; admin/staff checks use `User.IsInRole`.

## Notable Patterns & Gotchas

- The `Route` entity collides with `System.Web.Http.RouteAttribute`/routing types — the codebase uses `using RouteModel = BenLanSystem.Models.Entities.Route;` wherever needed. Follow this convention.
- `Booking.TotalAmount` is computed by the database — never set it in code.
- Data Protection keys persist to `App_Data/DataProtectionKeys/` inside the project directory.
- Seeding is idempotent and runs on every startup; trip re-seeding deletes past trips.
- Many entities have database-level check constraints — invalid enum-like values will be rejected by the DB, not just by validation.

## Code Style

- No comments unless requested.
- Follow existing patterns: primary-constructor style controllers, scoped service DI, DTO + AutoMapper for API boundaries.
- Frontend changes go in `wwwroot/css/`, `wwwroot/js/`, or Razor views — no build step or bundler.
