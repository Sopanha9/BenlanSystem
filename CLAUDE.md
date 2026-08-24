# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BenLanSystem is an ASP.NET Core MVC bus transportation booking management system for Cambodia, targeting .NET 10.0. It uses EF Core with SQL Server, ASP.NET Core Identity (with `long` keys), and AutoMapper. The app is a hybrid MVC + API application: Razor views handle the customer-facing and admin UIs, while API controllers under `/api/*` serve data to those views and external consumers.

## Development Commands

- `docker compose up -d db` — Start the SQL Server database container in the background.
- `docker compose down` — Stop and remove Docker containers.
- `docker compose up --build` — Run both the database and the web application in Docker.
- `dotnet build` — Build the solution (run from `BenLanSystem/`).
- `dotnet run` — Run the development server (listens on `http://localhost:5216` and `https://localhost:7109`).
- `dotnet ef migrations add <Name>` — Add an EF Core migration.
- `dotnet ef database update` — Apply pending migrations to the database.
- `dotnet add package <Package>` — Add a NuGet package.

**No automated test project exists.** Verify changes manually via the browser or Swagger UI (`/swagger` in development).

## Local Setup Requirements

- Docker Desktop installed and running.
- Start the SQL Server container with `docker compose up -d db`. The connection string points to `Server=localhost,1433;Database=BenLanDB_Dev;User Id=sa;Password=BenLan@Dev2026!;TrustServerCertificate=True;Encrypt=False`.
- On first run (or after `dotnet ef database update`), the app automatically applies migrations and seeds:
  - Roles: `Admin`, `Staff`, `Customer`, `Driver`
  - Cambodian locations, routes, vehicles, sample trips, and blog posts
  - An admin user: `admin@benlan.com` / `Admin@123456`

## High-Level Architecture

### Auth & Identity
- The custom user entity is `Staff`, which inherits from `IdentityUser<long>`. Identity is configured with `long` primary keys (`IdentityRole<long>`).
- `Program.cs` seeds roles and the admin user on startup inside `SeedAdminAsync`.
- API controllers extract the customer ID from `ClaimTypes.NameIdentifier`.

### Data Access
- `ApplicationDbContext` inherits from `IdentityDbContext<Staff, IdentityRole<long>, long>`.
- It contains 12 entity DbSets: `Locations`, `Routes`, `Vehicles`, `VehicleHistories`, `Trips`, `Bookings`, `BookingPassengers`, `BookingHistories`, `Payments`, `PaymentHistories`, `BlogPosts`.
- Several entities use database constraints and defaults configured in `OnModelCreating` (e.g., `CK_Vehicles_SeatCapacity`, unique indexes on `PlateNumber` and `Location.Name`).
- `Booking.TotalAmount` is a computed column (`[SeatsBooked] * [UnitPrice]`).
- `Trip` has a `[Timestamp] RowVersion` for optimistic concurrency.

### Service Layer
- Business logic lives in `Services/Implementations/` with interfaces in `Services/Interfaces/`.
- Eight services: `LocationService`, `RouteService`, `TripService`, `VehicleService`, `BookingService`, `PaymentService`, `StaffService`, `BlogService`.
- Services are registered as scoped in `Program.cs`.
- DTOs are defined in `Models/DTOs/` and mapped via AutoMapper in `Mappings/MappingProfile.cs`.

### Controllers
- **API controllers** (`Controllers/Api/*`) inherit from `ControllerBase`, use `[ApiController]` and `[Route("api/[controller]")]`, and return DTOs.
- **MVC controllers** (`Controllers/*`) return Razor views.
- **Admin area controllers** (`Controllers/Admin/*`) use `[Area("Admin")]` and `[Authorize(Roles = "Admin,Staff")]`; their views live in `Areas/Admin/Views/` with the shared layout `Areas/Admin/Views/Shared/_LayoutAdmin.cshtml`.

### Static Assets & Frontend
- Custom page-specific CSS files are in `wwwroot/css/` (e.g., `home.css`, `auth.css`).
- Vanilla JavaScript is in `wwwroot/js/` (e.g., `site.js`, `bookticket.js`).
- Third-party libraries (Bootstrap, jQuery) are in `wwwroot/lib/`.

## Notable Patterns
- The `Route` entity collides with `System.Route`, so the codebase consistently uses `using RouteModel = BenLanSystem.Models.Entities.Route;` where needed.
- API authorization checks typically verify the user ID from claims, and admin/staff roles are checked with `User.IsInRole` when accessing individual bookings.
