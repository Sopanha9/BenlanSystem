# BenLan Management System — Build, Wire & Bug Fix Plan

## Project Overview

ASP.NET Core MVC app (net10.0) for bus transportation booking in Cambodia. Uses EF Core + SQL Server, ASP.NET Core Identity (BIGINT keys), AutoMapper. The backend has 12 entities, 8 services, 4 API controllers — but the frontend is entirely static mockups. Only Login/Register/Logout work end-to-end.

**Project root:** `BenLanSystem\BenLanSystem\`
**Solution:** `BenLanSystem\BenLanSystem.sln`
**Connection string:** `Server=localhost\SQLEXPRESS;Database=BenLanDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`

---

## PHASE 0 — CRITICAL BACKEND BUG FIXES (do first, everything depends on these)

### 0.1 — Wrap BookingService.CreateAsync in a transaction

**File:** `Services\Implementations\BookingService.cs` lines 57-92

**Problem:** `SaveChangesAsync()` is called 3 separate times. If the 2nd or 3rd call fails, the DB is left inconsistent (booking exists with no passengers, or seats not decremented).

**Fix:** Replace the three separate `SaveChangesAsync()` calls with a single transaction:
1. Add `using Microsoft.EntityFrameworkCore.Storage;` at top
2. At the start of `CreateAsync`, add: `using var tx = await db.Database.BeginTransactionAsync();`
3. Replace all `await db.SaveChangesAsync();` calls (3 occurrences) with a single call at the end, before returning
4. Add `await tx.CommitAsync();` before the single `SaveChangesAsync`
5. Keep the intermediate `SaveChangesAsync` to get `booking.Id` for passengers, but wrap everything in the transaction

**Actual implementation — replace the entire method body:**

```csharp
public async Task<BookingDto> CreateAsync(long customerId, BookingCreateDto dto)
{
    using var tx = await db.Database.BeginTransactionAsync();

    // Validate trip exists and is open
    var trip = await db.Trips.FindAsync(dto.TripId)
        ?? throw new InvalidOperationException("Trip not found");

    if (trip.StatusName != "Open")
        throw new InvalidOperationException("Trip is not open for booking");

    if (trip.AvailableSeats < dto.SeatsBooked)
        throw new InvalidOperationException($"Only {trip.AvailableSeats} seats available");

    // Validate passenger count matches seat count
    if (dto.Passengers.Count != dto.SeatsBooked)
        throw new InvalidOperationException("Passenger count must match seat count");

    var booking = new Booking
    {
        TripId = dto.TripId, CustomerId = customerId, SeatsBooked = dto.SeatsBooked,
        UnitPrice = dto.UnitPrice, BookingStatus = "Pending", Notes = dto.Notes
    };
    db.Bookings.Add(booking);
    await db.SaveChangesAsync(); // need booking.Id

    foreach (var p in dto.Passengers)
    {
        db.BookingPassengers.Add(new BookingPassenger
        {
            BookingId = booking.Id, PassengerName = p.PassengerName, SeatNumber = p.SeatNumber
        });
    }

    db.BookingHistories.Add(new BookingHistory
    {
        BookingId = booking.Id, ChangeDate = DateTime.UtcNow, ChangedBy = customerId.ToString(),
        OldStatus = "None", NewStatus = "Pending", Remarks = "Booking created"
    });

    // Decrease available seats
    trip.AvailableSeats -= dto.SeatsBooked;
    trip.UpdatedAtUtc = DateTime.UtcNow;

    await db.SaveChangesAsync();
    await tx.CommitAsync();

    return (await GetByIdAsync(booking.Id))!;
}
```

---

### 0.2 — Add optimistic concurrency control to Trip

**File:** `Models\Entities\Trip.cs`

**Problem:** Two simultaneous bookings can both read `AvailableSeats=5`, both subtract, and both succeed — overbooking. No `RowVersion` exists.

**Fix:** Add a concurrency token to the Trip entity:

```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = null!;
```

Then in `ApplicationDbContext.cs`, configure it:
```csharp
e.Property(t => t.RowVersion).IsRowVersion();
```

Then in `BookingService.CreateAsync`, catch `DbUpdateConcurrencyException` and retry or throw a user-friendly error.

**Actually, simpler approach — use a raw SQL check instead of full concurrency token (less invasive):**

In `BookingService.CreateAsync`, before decrementing, verify the seat count hasn't changed:
```csharp
// Re-read trip to verify seat availability hasn't changed since we started
await db.Entry(trip).ReloadAsync();
if (trip.AvailableSeats < dto.SeatsBooked)
    throw new InvalidOperationException($"Seats no longer available. Only {trip.AvailableSeats} left.");
```

This is already handled in the 0.1 fix above since we validate before saving. For true concurrency safety, add the `RowVersion` property as described.

---

### 0.3 — Fix BlogPost.AuthorId never being set

**File:** `Services\Implementations\BlogService.cs` line 37-48

**Problem:** Comment says "AuthorId will be set by the controller" but no controller calls BlogService. `AuthorId` is `[Required]` non-nullable `long`, defaults to `0` — causing FK violation on every post.

**Fix:** Change `CreateAsync` to accept `authorId`:

1. Update `IBlogService` interface (`Services\Interfaces\IBlogService.cs`):
```csharp
Task<BlogPostDto> CreateAsync(BlogPostCreateDto dto, long authorId);
```

2. Update `BlogService.CreateAsync`:
```csharp
public async Task<BlogPostDto> CreateAsync(BlogPostCreateDto dto, long authorId)
{
    var post = new BlogPost
    {
        Title = dto.Title, Content = dto.Content, Summary = dto.Summary,
        ImageUrl = dto.ImageUrl, IsPublished = dto.IsPublished, AuthorId = authorId
    };
    db.Set<BlogPost>().Add(post);
    await db.SaveChangesAsync();
    return (await GetByIdAsync(post.Id))!;
}
```

---

### 0.4 — Fix Register not assigning Customer role

**File:** `Controllers\AccountController.cs` line 74-79

**Problem:** After user creation via `UserManager.CreateAsync`, no role is assigned. The user has zero roles — can't access any authorized endpoints properly.

**Fix:** After `CreateAsync` succeeds, add:
```csharp
// Ensure Customer role exists
if (!await roleManager.RoleExistsAsync("Customer"))
{
    await roleManager.CreateAsync(new IdentityRole<long>("Customer"));
}
await userManager.AddToRoleAsync(user, "Customer");
```

You'll need to inject `RoleManager<IdentityRole<long>>` into the constructor:
```csharp
public class AccountController(
    UserManager<Staff> userManager,
    SignInManager<Staff> signInManager,
    RoleManager<IdentityRole<long>> roleManager) : Controller
```

---

### 0.5 — Fix RegisterViewModel.FullName being lost

**File:** `Controllers\AccountController.cs` line 66-72

**Problem:** The Register form collects `FullName`, but the controller only sets `UserName`, `Email`, `PhoneNumber`. The user's name is discarded — `FirstName` and `LastName` remain empty.

**Fix:** Split `FullName` into `FirstName`/`LastName` when creating the Staff:

```csharp
var nameParts = model.FullName?.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
var user = new Staff
{
    UserName = model.Email,
    Email = model.Email,
    PhoneNumber = model.PhoneNumber,
    FirstName = nameParts is { Length: > 0 } ? nameParts[0] : "",
    LastName = nameParts is { Length: > 1 } ? nameParts[1] : "",
    EmailConfirmed = true
};
```

---

### 0.6 — Add [Authorize] to GET endpoints exposing sensitive data

**Files:** 
- `Controllers\Api\BookingController.cs` line 22 (GET by id) — add `[Authorize]`
- `Controllers\Api\PaymentController.cs` line 12 (GET by id) — add `[Authorize]`
- `Controllers\Api\PaymentController.cs` line 20 (GET by booking) — add `[Authorize]`

**Fix:** Add `[Authorize]` attribute to each of these three endpoints. Anyone can currently enumerate booking/payment IDs and see passenger names and payment info.

---

### 0.7 — Add brute-force lockout protection on login

**File:** `Controllers\AccountController.cs` line 32

**Problem:** `PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false)` — unlimited password attempts.

**Fix:** Change `lockoutOnFailure: false` to `lockoutOnFailure: true`.

Also in `Program.cs`, configure lockout settings in the Identity options:
```csharp
builder.Services.AddIdentity<Staff, IdentityRole<long>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
```

---

### 0.8 — Add payment ownership validation

**File:** `Controllers\Api\PaymentController.cs` line 27-33

**Problem:** Any authenticated user can create a payment for any booking (including other users' bookings).

**Fix:** In `PaymentController.Create`, verify the booking belongs to the current user (unless Admin/Staff):

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<PaymentDto>> Create(PaymentCreateDto dto)
{
    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userId is null) return Unauthorized();

    // Verify booking ownership (skip for Admin/Staff)
    if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
    {
        var booking = await db.Bookings.FindAsync(dto.BookingId); // inject db or add lookup
        if (booking is null || booking.CustomerId != long.Parse(userId))
            return Forbid();
    }

    var payment = await paymentService.CreateAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
}
```

---

### 0.9 — Add payment state machine validation

**File:** `Services\Implementations\PaymentService.cs` lines 45-77

**Problem:** `MarkAsPaidAsync` doesn't check current status — can mark a Refunded payment as Paid. `RefundAsync` can refund a Pending payment directly.

**Fix in MarkAsPaidAsync (line 45):**
```csharp
public async Task<PaymentDto> MarkAsPaidAsync(long id, PaymentMarkPaidDto dto)
{
    var payment = await db.Payments.FindAsync(id) ?? throw new KeyNotFoundException($"Payment {id} not found");
    if (payment.PaymentStatus != "Pending")
        throw new InvalidOperationException($"Cannot mark as paid — current status is '{payment.PaymentStatus}'");
    // ... rest stays the same
}
```

**Fix in RefundAsync (line 63):**
```csharp
public async Task<PaymentDto> RefundAsync(long id, PaymentRefundDto dto)
{
    var payment = await db.Payments.FindAsync(id) ?? throw new KeyNotFoundException($"Payment {id} not found");
    if (payment.PaymentStatus != "Paid")
        throw new InvalidOperationException($"Cannot refund — current status is '{payment.PaymentStatus}'");
    // ... rest stays the same
}
```

---

### 0.10 — Seed Customer and Driver roles in Program.cs

**File:** `Program.cs` line 80

**Problem:** Only `Admin` and `Staff` roles are seeded. SQL script has `Admin`, `Staff`, `Customer`, `Driver`.

**Fix:** Add `"Customer"` and `"Driver"` to the roles array:
```csharp
var roles = new[] { "Admin", "Staff", "Customer", "Driver" };
```

---

## PHASE 1 — BACKEND GAPS (API controllers that exist as services but have no controller)

### 1.1 — Create LocationController API

**File to create:** `Controllers\Api\LocationController.cs`

The `ILocationService` exists with: GetAll, GetById, Create, Update, Delete.

```csharp
using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class LocationController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll()
    {
        var locations = await locationService.GetAllAsync();
        return Ok(locations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id)
    {
        var loc = await locationService.GetByIdAsync(id);
        if (loc is null) return NotFound();
        return Ok(loc);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<LocationDto>> Create(LocationCreateDto dto)
    {
        var loc = await locationService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = loc.Id }, loc);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<LocationDto>> Update(int id, LocationUpdateDto dto)
    {
        var loc = await locationService.UpdateAsync(id, dto);
        if (loc is null) return NotFound();
        return Ok(loc);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await locationService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
```

---

### 1.2 — Create RouteController API

**File to create:** `Controllers\Api\RouteController.cs`

The `IRouteService` exists with: GetAll, GetById, Create, Update, Delete. Follow the same pattern as LocationController above — expose all 5 operations with `[Authorize(Roles = "Admin,Staff")]` on Create/Update and `[Authorize(Roles = "Admin")]` on Delete. GetAll and GetById are public.

---

### 1.3 — Create VehicleController API

**File to create:** `Controllers\Api\VehicleController.cs`

The `IVehicleService` exists with: GetAll, GetById, Create, Update, Delete. Same pattern — Create/Update need Admin+Staff, Delete needs Admin, GetAll and GetById are public.

---

### 1.4 — Add GET endpoint to StaffController (admin use)

**File to create:** `Controllers\Api\StaffController.cs`

The `IStaffService` exists with: GetAll, GetById, Create, Update. Only Admin can access Create. Admin/Staff can access GetAll/GetById. Staff can Update their own profile (add self-update logic).

---

### 1.5 — Create BlogController API

**File to create:** `Controllers\Api\BlogController.cs`

The `IBlogService` exists with: GetPublished, GetById, Create, Update, Delete.
- GetPublished and GetById are public
- Create/Update need Admin/Staff + pass the authenticated user's ID as authorId
- Delete needs Admin

---

## PHASE 2 — WIRE THE CUSTOMER JOURNEY (Search → Book → Pay → My Bookings)

### 2.1 — Fix the "My Booking" nav link

**File:** `Views\Shared\_Layout.cshtml` line 35

Change: `<li><a href="#" class="nav-link-item ...">My booking</a></li>`
To: `<li><a asp-controller="Home" asp-action="MyBookings" class="nav-link-item @(ViewData["ActivePage"]?.ToString() == "MyBookings" ? "active" : "")" id="nav-mybooking">My booking</a></li>`

---

### 2.2 — Create MyBookings page

**New controller action in HomeController:**
```csharp
[Authorize]
public async Task<IActionResult> MyBookings()
{
    ViewData["ActivePage"] = "MyBookings";
    return View();
}
```

**New view:** `Views\Home\MyBookings.cshtml`

This page should:
1. On page load, call `GET /api/Booking/my?page=1&pageSize=10` using `fetch()`
2. Display booking cards showing: route (origin → destination), departure time, seats booked, total amount, status
3. Each card has a "Cancel" button for Pending/Confirmed bookings
4. The cancel button calls `POST /api/Booking/{id}/cancel`
5. Show pagination controls using the `TotalCount` from `PagedResultDto`
6. If user is not authenticated, redirect to Login

**Key JS logic for the page:**
```javascript
async function loadMyBookings(page = 1) {
    const res = await fetch(`/api/Booking/my?page=${page}&pageSize=10`);
    if (res.status === 401) { window.location = '/Account/Login'; return; }
    const data = await res.json();
    renderBookings(data.items);
    renderPagination(data);
}

async function cancelBooking(id) {
    if (!confirm('Cancel this booking?')) return;
    const res = await fetch(`/api/Booking/${id}/cancel`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason: 'Cancelled by user' })
    });
    if (res.ok) { loadMyBookings(); }
}
```

Use the same CSS styling patterns from the existing BookTicket ticket cards for consistency.

---

### 2.3 — Wire the homepage search form to the actual API

**File:** `Views\Home\Index.cshtml` — the hero booking form (lines 22-73)
**File:** `wwwroot\js\site.js` — currently just `e.preventDefault()`

**What to do:**
1. Replace the two text inputs ("Departing from" / "Going to") with `<select>` dropdowns populated from `GET /api/Location`
2. Keep the date inputs
3. Remove the "Returning date" field (backend doesn't support round-trip)
4. On submit, redirect to `BookTicket?from=X&to=Y&date=Z` with query params
5. Or better: on submit, call `GET /api/Trip?OriginId=X&DestinationId=Y&DepartureDate=Z` and redirect to a results page

**Simplest approach — redirect to BookTicket with query params:**

In `site.js`, replace the `e.preventDefault()` handler:
```javascript
const bookingForm = document.getElementById('booking-form');
if (bookingForm) {
    bookingForm.addEventListener('submit', function(e) {
        e.preventDefault();
        const from = document.getElementById('input-departing').value.trim();
        const to = document.getElementById('input-goingto').value.trim();
        const date = document.getElementById('input-goingdate').value;
        if (from && to) {
            window.location.href = `/Home/BookTicket?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&date=${encodeURIComponent(date || '')}`;
        }
    });
}
```

---

### 2.4 — Rewire BookTicket.cshtml to use real API calls

**File:** `Views\Home\BookTicket.cshtml`
**File:** `wwwroot\js\bookticket.js`

This is the most complex rewiring. The page needs to:

1. **Accept query params** (`from`, `to`, `date`) from the homepage redirect, and pre-fill the search fields
2. **Load location dropdowns** from `GET /api/Location` on page load — populate both "Departing from" and "Going to" as datalist or select elements
3. **On Search click**, call `GET /api/Trip?OriginId={id}&DestinationId={id}&DepartureDate={date}&Page=1&PageSize=10` (or use `GET /api/Search` which does the same thing)
4. **Render results dynamically** — replace the 3 hardcoded `.bt-ticket` divs with JS-generated HTML from the API response
5. **On "Book now" click**, open the modal with real trip data (TripId, price, available seats)
6. **On "Confirm Booking" click**, call `POST /api/Booking` with the actual `BookingCreateDto`

**Key changes to bookticket.js:**

```javascript
// Load locations for dropdowns
async function loadLocations() {
    const res = await fetch('/api/Location');
    const locations = await res.json();
    // Populate datalist or build select options
    window.__locations = locations;
}

// Real search
async function searchTrips() {
    const fromName = document.getElementById('bt-input-from').value.trim();
    const toName = document.getElementById('bt-input-to').value.trim();
    const date = document.getElementById('bt-input-date').value;
    
    // Match names to location IDs
    const fromLoc = window.__locations.find(l => l.name.toLowerCase() === fromName.toLowerCase());
    const toLoc = window.__locations.find(l => l.name.toLowerCase() === toName.toLowerCase());
    
    const params = new URLSearchParams();
    if (fromLoc) params.set('OriginId', fromLoc.id);
    if (toLoc) params.set('DestinationId', toLoc.id);
    if (date) params.set('DepartureDate', date);
    params.set('PageSize', '20');
    
    const res = await fetch(`/api/Trip?${params}`);
    const data = await res.json();
    renderTrips(data.items);
}

// Real booking
async function confirmBooking() {
    const firstname = document.getElementById('bt-modal-firstname').value.trim();
    const lastname = document.getElementById('bt-modal-lastname').value.trim();
    const phone = document.getElementById('bt-modal-phone').value.trim();
    const seats = seatCount;

    if (!firstname || !phone) { /* shake validation */ return; }

    const dto = {
        tripId: window.__selectedTripId,
        seatsBooked: seats,
        unitPrice: window.__selectedTripPrice,
        notes: null,
        passengers: [{
            passengerName: `${firstname} ${lastname}`.trim(),
            seatNumber: `A${seats}` // simplified — real seat assignment needed
        }]
    };

    const res = await fetch('/api/Booking', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });

    if (res.status === 401) { window.location = '/Account/Login'; return; }
    if (!res.ok) {
        const err = await res.text();
        alert('Booking failed: ' + err);
        return;
    }

    const booking = await res.json();
    // Show success and redirect to My Bookings or Payment
    window.location.href = `/Home/MyBookings`;
}
```

---

### 2.5 — Create Payment Page

**New controller action in HomeController (or a new Payment MVC controller):**
```csharp
[Authorize]
public async Task<IActionResult> Pay(long bookingId)
{
    // In a real implementation, verify the booking belongs to the current user
    ViewData["BookingId"] = bookingId;
    ViewData["ActivePage"] = "Book";
    return View();
}
```

**New view:** `Views\Home\Pay.cshtml`

This page should:
1. Accept a `bookingId` query param
2. Call `GET /api/Booking/{bookingId}` to show booking summary
3. Show payment method selection: ABA, ACLEDA, Wing, Cash, Card
4. On confirm, call `POST /api/Payment` with the booking ID, amount, and selected method
5. Show success/failure

---

## PHASE 3 — ADMIN DASHBOARD

### 3.1 — Create Admin area structure

**New folder:** `Controllers\Admin\`

**New folder structure under Views:**
```
Views\Admin\
    _LayoutAdmin.cshtml
    Index.cshtml
    Trips\
        Index.cshtml
        Create.cshtml
        Edit.cshtml
    Vehicles\
        Index.cshtml
    Routes\
        Index.cshtml
    Locations\
        Index.cshtml
    Bookings\
        Index.cshtml
    Payments\
        Index.cshtml
    Staff\
        Index.cshtml
```

---

### 3.2 — Admin layout

**New file:** `Views\Admin\_LayoutAdmin.cshtml`

- Separate layout from the public site
- Sidebar navigation: Dashboard, Trips, Vehicles, Routes, Locations, Bookings, Payments, Staff
- Check `User.IsInRole("Admin") || User.IsInRole("Staff")` — redirect to Login if not
- Use the same CSS foundation but with admin-specific styles (tables, forms)
- Reference Bootstrap from the existing lib

---

### 3.3 — AdminController + dashboard

**New file:** `Controllers\Admin\AdminController.cs`

```csharp
[Authorize(Roles = "Admin,Staff")]
[Area("Admin")]
public class AdminController : Controller
{
    public IActionResult Index() => View();
}
```

Dashboard view (`Views\Admin\Index.cshtml`): Summary cards showing counts of Trips, Vehicles, Bookings, Payments, Locations. Each card links to its CRUD page.

---

### 3.4 — Admin Trip Management

**New file:** `Controllers\Admin\TripsController.cs`

CRUD pages:
- **Index** — Table of all trips with search/filter. Columns: ID, Route, Vehicle, Departure, Price, Seats, Status, Actions (Edit/Delete)
- **Create** — Form: Route dropdown, Vehicle dropdown, Departure datetime, Arrival datetime, Base Price, Available Seats, Status
- **Edit** — Same form pre-filled from `GET /api/Trip/{id}`

All API calls go to the existing `TripController` API endpoints.

---

### 3.5 — Admin Vehicle Management

**New file:** `Controllers\Admin\VehiclesController.cs`

Same CRUD pattern. Calls the `VehicleController` API created in Phase 1.3.

---

### 3.6 — Admin Route Management

**New file:** `Controllers\Admin\RoutesController.cs`

Same CRUD pattern. Calls the `RouteController` API created in Phase 1.2.

---

### 3.7 — Admin Location Management

**New file:** `Controllers\Admin\LocationsController.cs`

Same CRUD pattern. Calls the `LocationController` API created in Phase 1.1.

---

### 3.8 — Admin Booking Management

**New file:** `Controllers\Admin\BookingsController.cs`

- **Index** — Table of all bookings: ID, Customer, Trip, Seats, Amount, Status, Date, Actions
- **Detail** — Full booking view with passenger list, payment info
- Actions: Mark as Confirmed / Completed (calls `PUT /api/Booking/{id}` — need to add a confirm endpoint or use existing status changes)

Calls `GET /api/Booking/all` (already exists, Admin/Staff only).

---

### 3.9 — Admin Payment Management

**New file:** `Controllers\Admin\PaymentsController.cs`

- **Index** — Table of all payments: ID, Booking, Amount, Method, Status, Date, Transaction Ref
- Actions: Mark as Paid, Refund (calls existing `POST /api/Payment/{id}/mark-paid` and `refund`)

---

### 3.10 — Admin Staff Management

**New file:** `Controllers\Admin\StaffController.cs`

Uses `IStaffService`. CRUD for staff/driver user accounts. Calls the `StaffController` API from Phase 1.4.

---

## PHASE 4 — POLISH & FIX REMAINING FRONTEND ISSUES

### 4.1 — Wire Blog pages to BlogService

**File:** `Controllers\HomeController.cs`

Replace the hardcoded Blog actions:
```csharp
public async Task<IActionResult> Blog([FromServices] IBlogService blogService)
{
    ViewData["ActivePage"] = "Blog";
    var posts = await blogService.GetPublishedAsync(1, 20);
    return View(posts); // pass posts as model
}
```

Update `Views\Home\Blog.cshtml` to use `@model IEnumerable<BlogPostDto>` and render posts dynamically instead of the 2 hardcoded cards.

Similarly for `BlogDetail` — accept ID, call `GetByIdAsync`, pass the DTO as the model, render dynamically.

---

### 4.2 — Fix CSS duplication

- Move the `.section-container` class to `site.css` (it's duplicated in home.css, about.css, contact.css)
- Define CSS custom properties in `:root` in `site.css` for brand colors (`--primary: #4e78ff`, `--primary-dark: #3f61d8`, etc.)
- Update auth.css, blog.css, about.css to use the variables

---

### 4.3 — Fill Privacy page content

**File:** `Views\Home\Privacy.cshtml`

Write actual privacy policy content relevant to a Cambodian transportation booking service.

---

### 4.4 — Fix footer dead links

**File:** `Views\Shared\_Footer.cshtml`

- "Home" link: change `href="/"` to `<a asp-controller="Home" asp-action="Index">Home</a>`
- "About us": change `href="#"` to `<a asp-controller="Home" asp-action="About">About us</a>`
- "Book Ticket": change `href="#"` to `<a asp-controller="Home" asp-action="BookTicket">Book ticket</a>`

---

### 4.5 — Fix "Forget password?" dead link

**File:** `Views\Account\Login.cshtml` line 45

Change: `<a href="#" class="auth-forgot">Forget password?</a>`
To: Either remove it (no password reset flow exists yet) or link to `#` with a `onclick="alert('Coming soon')"` handler.

---

### 4.6 — Remove dead JS reference to bt-swap-btn

**File:** `wwwroot\js\bookticket.js` lines 4-14

Delete the entire `bt-swap-btn` event listener block since the HTML has no such element.

---

### 4.7 — Remove empty importmap tag

**File:** `Views\Shared\_Layout.cshtml` line 8

Remove: `<script type="importmap"></script>`

---

### 4.8 — Fix Login label inconsistency

**File:** `Views\Account\Login.cshtml` line 34

Either change the label text from "Username / Email" to just "Email" (matching the `[EmailAddress]` validation), OR update `LoginViewModel` to accept a `UserName` property and update the controller to look up by username OR email.

**Simplest fix:** Change the label to "Email" since the backend uses `model.Email` for sign-in.

---

### 4.9 — Add Swagger/OpenAPI

**File:** `Program.cs`

Add before `var app = builder.Build();`:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

After `var app = builder.Build();` and before `app.UseHttpsRedirection();`:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

This gives a documentation UI at `/swagger` in development mode.

---

## EXECUTION ORDER

```
Phase 0 (all bugs) ──────┐
                          ├──▶ Phase 2 (customer journey)
Phase 1 (backend gaps) ──┘       │
                                  ▼
                          Phase 3 (admin dashboard) — can run parallel with Phase 2
                                  │
                                  ▼
                          Phase 4 (polish) — last, after everything else
```

**Critical rule:** Do NOT start Phase 2 until Phase 0.1 (booking transaction) is done — the booking flow depends on it.

---

## FILE INDEX — Everything That Needs Touching

### Modified files (existing):
- `Services\Implementations\BookingService.cs` — 0.1, 0.2
- `Models\Entities\Trip.cs` — 0.2
- `Services\Implementations\BlogService.cs` — 0.3
- `Services\Interfaces\IBlogService.cs` — 0.3
- `Controllers\AccountController.cs` — 0.4, 0.5, 0.7
- `Controllers\Api\BookingController.cs` — 0.6
- `Controllers\Api\PaymentController.cs` — 0.6, 0.8
- `Services\Implementations\PaymentService.cs` — 0.9
- `Program.cs` — 0.7, 0.10, 4.9
- `Controllers\HomeController.cs` — 2.2, 2.5, 4.1
- `Views\Shared\_Layout.cshtml` — 2.1, 4.7
- `Views\Home\Index.cshtml` — 2.3
- `Views\Home\BookTicket.cshtml` — 2.4
- `Views\Home\Blog.cshtml` — 4.1
- `Views\Home\BlogDetail.cshtml` — 4.1
- `Views\Home\Privacy.cshtml` — 4.3
- `Views\Shared\_Footer.cshtml` — 4.4
- `Views\Account\Login.cshtml` — 4.5, 4.8
- `wwwroot\js\bookticket.js` — 2.4, 4.6
- `wwwroot\js\site.js` — 2.3
- `wwwroot\css\site.css` — 4.2
- `wwwroot\css\home.css` — 4.2
- `wwwroot\css\about.css` — 4.2
- `wwwroot\css\contact.css` — 4.2

### New files to create:
- `Controllers\Api\LocationController.cs` — 1.1
- `Controllers\Api\RouteController.cs` — 1.2
- `Controllers\Api\VehicleController.cs` — 1.3
- `Controllers\Api\StaffController.cs` — 1.4
- `Controllers\Api\BlogController.cs` — 1.5
- `Views\Home\MyBookings.cshtml` — 2.2
- `Views\Home\Pay.cshtml` — 2.5
- `Controllers\Admin\AdminController.cs` — 3.3
- `Controllers\Admin\TripsController.cs` — 3.4
- `Controllers\Admin\VehiclesController.cs` — 3.5
- `Controllers\Admin\RoutesController.cs` — 3.6
- `Controllers\Admin\LocationsController.cs` — 3.7
- `Controllers\Admin\BookingsController.cs` — 3.8
- `Controllers\Admin\PaymentsController.cs` — 3.9
- `Controllers\Admin\StaffController.cs` — 3.10
- `Views\Admin\_LayoutAdmin.cshtml` — 3.2
- `Views\Admin\Index.cshtml` — 3.3
- `Views\Admin\Trips\Index.cshtml` — 3.4
- `Views\Admin\Trips\Create.cshtml` — 3.4
- `Views\Admin\Trips\Edit.cshtml` — 3.4
- `Views\Admin\Vehicles\Index.cshtml` — 3.5
- `Views\Admin\Routes\Index.cshtml` — 3.6
- `Views\Admin\Locations\Index.cshtml` — 3.7
- `Views\Admin\Bookings\Index.cshtml` — 3.8
- `Views\Admin\Payments\Index.cshtml` — 3.9
- `Views\Admin\Staff\Index.cshtml` — 3.10

---

## PHASE 5 — FINAL POLISH: WIRE REMAINING HOMEPAGE ELEMENTS

Phase 5 fixes every remaining dead link, hardcoded card, and label mismatch on the public-facing pages. Only 3 files touched, no backend changes.

---

### 5.1 — Quick Check: replace 9 hardcoded cards with dynamic API rendering

**File:** `Views\Home\Index.cshtml`

**What to change:**

1. Delete all 9 hardcoded `.ticket-card` divs (currently lines 88-278 — cards 1-9, all identical fake data). Replace the entire content inside `<div class="tickets-grid" id="tickets-grid">` with:

```html
<div class="tickets-grid" id="tickets-grid">
    <div id="qc-loading" style="grid-column:1/-1;text-align:center;padding:40px;color:rgba(255,255,255,0.5);">Loading available trips...</div>
</div>
```

2. Update the "See more" button (line 81) to be hidden until cards load:

```html
<button class="see-more-btn" id="see-more-btn" onclick="toggleQuickCheck()" style="display:none;">See more</button>
```

3. Replace the ENTIRE `@section Scripts` block (currently lines 580-628) with the following. This keeps `toggleQuickCheck` and `toggleFaq` and adds `loadQuickCheck`:

```html
@section Scripts {
<script>
    // ── Quick Check: dynamic trip cards ──

    let quickCheckTrips = [];

    function escHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, function(c) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
        });
    }

    function formatTime(utcString) {
        if (!utcString) return '';
        return new Date(utcString).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    async function loadQuickCheck() {
        try {
            const res = await fetch('/api/Trip?StatusName=Open&PageSize=9');
            if (!res.ok) {
                document.getElementById('qc-loading').textContent = 'Unable to load trips right now.';
                return;
            }
            const data = await res.json();
            quickCheckTrips = data.items || [];
            renderQuickCheckCards(quickCheckTrips);
        } catch {
            document.getElementById('qc-loading').textContent = 'Unable to load trips right now.';
        }
    }

    function renderQuickCheckCards(trips) {
        const grid = document.getElementById('tickets-grid');
        const seeMoreBtn = document.getElementById('see-more-btn');

        if (!trips.length) {
            grid.innerHTML = '<div style="grid-column:1/-1;text-align:center;padding:40px;color:rgba(255,255,255,0.5);">No trips available at the moment. Check back soon.</div>';
            if (seeMoreBtn) seeMoreBtn.style.display = 'none';
            return;
        }

        grid.innerHTML = trips.map(function(t, i) {
            const depTime = formatTime(t.departureTimeUtc);
            const routeLabel = escHtml(t.originName) + ' to ' + escHtml(t.destinationName) + (depTime ? ' (' + depTime + ')' : '');
            const departureDate = t.departureTimeUtc ? new Date(t.departureTimeUtc).toLocaleDateString() : '';
            const price = '$' + Number(t.basePrice || 0).toFixed(2);
            const isExtra = i >= 3 ? ' ticket-card-extra hidden' : '';
            const seatsLeft = Number(t.availableSeats || 0);

            return '<div class="ticket-card' + isExtra + '" id="qc-card-' + i + '">' +
                '<div class="ticket-card-header">' +
                    '<div class="ticket-route-icon">' +
                        '<svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor"><path d="M17 6H3a2 2 0 00-2 2v6a2 2 0 002 2h1v2a1 1 0 001 1h1a1 1 0 001-1v-2h8v2a1 1 0 001 1h1a1 1 0 001-1v-2h1a2 2 0 002-2V8a2 2 0 00-2-2zM6.5 13a1.5 1.5 0 110-3 1.5 1.5 0 010 3zm11 0a1.5 1.5 0 110-3 1.5 1.5 0 010 3zM3 10V8h18v2H3z"/></svg>' +
                    '</div>' +
                    '<h3 class="ticket-route">' + routeLabel + '</h3>' +
                '</div>' +
                '<p class="ticket-period">Departure: ' + escHtml(departureDate) + ' | ' + seatsLeft + ' seat' + (seatsLeft !== 1 ? 's' : '') + ' left</p>' +
                '<div class="ticket-footer">' +
                    '<div class="ticket-price">' +
                        '<svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" style="color:#f0c040"><path d="M21.41 11.58l-9-9A2 2 0 0011 2H4a2 2 0 00-2 2v7a2 2 0 00.59 1.42l9 9A2 2 0 0013 22a2 2 0 001.41-.59l7-7A2 2 0 0021.41 11.58zM5.5 7a1.5 1.5 0 110-3 1.5 1.5 0 010 3z"/></svg>' +
                        '<span>' + price + '</span>' +
                    '</div>' +
                    '<button class="book-now-btn" onclick="quickBook(\'' + escHtml(t.originName) + '\',\'' + escHtml(t.destinationName) + '\')">Book now</button>' +
                '</div>' +
            '</div>';
        }).join('');

        if (seeMoreBtn) {
            seeMoreBtn.style.display = trips.length > 3 ? '' : 'none';
            seeMoreBtn.textContent = 'See more';
        }
    }

    function quickBook(from, to) {
        window.location.href = '/Home/BookTicket?from=' + encodeURIComponent(from) + '&to=' + encodeURIComponent(to);
    }

    // ── See-more toggle (unchanged logic, works with JS-rendered cards) ──

    function toggleQuickCheck() {
        const extras = document.querySelectorAll('.ticket-card-extra');
        const btn = document.getElementById('see-more-btn');
        const isExpanded = btn.textContent.trim() === 'See less';

        extras.forEach(function(card, index) {
            if (isExpanded) {
                card.classList.add('hidden');
                card.classList.remove('fade-in-card');
            } else {
                card.classList.remove('hidden');
                setTimeout(function() {
                    card.classList.add('fade-in-card');
                }, index * 60);
            }
        });

        btn.textContent = isExpanded ? 'See more' : 'See less';
    }

    // ── FAQ accordion (unchanged) ──

    function toggleFaq(trigger) {
        var item = trigger.closest('.faq-item');
        var body = item.querySelector('.faq-body');
        var isOpen = item.classList.contains('faq-open');

        document.querySelectorAll('.faq-item.faq-open').forEach(function(openItem) {
            var openBody = openItem.querySelector('.faq-body');
            openBody.style.maxHeight = openBody.scrollHeight + 'px';
            requestAnimationFrame(function() {
                openBody.style.maxHeight = '0';
            });
            openItem.classList.remove('faq-open');
            openItem.querySelector('.faq-trigger').setAttribute('aria-expanded', 'false');
        });

        if (!isOpen) {
            item.classList.add('faq-open');
            trigger.setAttribute('aria-expanded', 'true');
            body.style.maxHeight = body.scrollHeight + 'px';
        }
    }

    // ── Kick off Quick Check on page load ──
    loadQuickCheck();
</script>
}
```

---

### 5.2 — Fix FAQ dead links (View Map / Get Directions)

**File:** `Views\Home\Index.cshtml`

The 5 "View Map" links and 3 "Get Directions" links are all `href="#"`. Replace them with real Google Maps links pointed at the relevant Cambodian city bus stations.

**FAQ "Branch Locations?" section (around lines 502-508):**

Change:
```html
<li>Phnom Penh: <a href="#" target="_blank" rel="noopener">View Map</a></li>
<li>Siem Reap: <a href="#" target="_blank" rel="noopener">View Map</a></li>
<li>Sihanoukville: <a href="#" target="_blank" rel="noopener">View Map</a></li>
<li>Battambang: <a href="#" target="_blank" rel="noopener">View Map</a></li>
<li>Poi Pet: <a href="#" target="_blank" rel="noopener">View Map</a></li>
```

To:
```html
<li>Phnom Penh: <a href="https://maps.google.com/?q=Phnom+Penh+Bus+Station" target="_blank" rel="noopener">View Map</a></li>
<li>Siem Reap: <a href="https://maps.google.com/?q=Siem+Reap+Bus+Station" target="_blank" rel="noopener">View Map</a></li>
<li>Sihanoukville: <a href="https://maps.google.com/?q=Sihanoukville+Bus+Station" target="_blank" rel="noopener">View Map</a></li>
<li>Battambang: <a href="https://maps.google.com/?q=Battambang+Bus+Station" target="_blank" rel="noopener">View Map</a></li>
<li>Poi Pet: <a href="https://maps.google.com/?q=Poipet+Bus+Station" target="_blank" rel="noopener">View Map</a></li>
```

**FAQ "Directions to?" section (around lines 566-570):**

Change:
```html
<li>Phnom Penh Station: <a href="#" target="_blank" rel="noopener">Get Directions</a></li>
<li>Siem Reap Station: <a href="#" target="_blank" rel="noopener">Get Directions</a></li>
<li>Battambang Station: <a href="#" target="_blank" rel="noopener">Get Directions</a></li>
```

To:
```html
<li>Phnom Penh Station: <a href="https://maps.google.com/?q=Phnom+Penh+Bus+Station" target="_blank" rel="noopener">Get Directions</a></li>
<li>Siem Reap Station: <a href="https://maps.google.com/?q=Siem+Reap+Bus+Station" target="_blank" rel="noopener">Get Directions</a></li>
<li>Battambang Station: <a href="https://maps.google.com/?q=Battambang+Bus+Station" target="_blank" rel="noopener">Get Directions</a></li>
```

---

### 5.3 — Make Destinations cards clickable

**File:** `Views\Home\Index.cshtml`

Each of the 6 destination cards in the Destinations section should link to BookTicket with that destination pre-filled. Currently they're just `<div>` with no link.

Wrap each card in an `<a>` tag. Replace the 6 `.dest-card` divs (lines 405-445):

```html
<!-- Siem Reap -->
<a asp-controller="Home" asp-action="BookTicket" asp-route-to="Siem Reap" class="dest-card" id="dest-card-siemreap">
    <img src="~/designs/siemreap.png" alt="Siem Reap" class="dest-img" loading="lazy" />
    <div class="dest-overlay"></div>
    <span class="dest-name">Siem Reap</span>
</a>

<!-- Phnom Penh -->
<a asp-controller="Home" asp-action="BookTicket" asp-route-from="Phnom Penh" class="dest-card" id="dest-card-phnompenh">
    <img src="~/designs/phnompenh.png" alt="Phnom Penh" class="dest-img" loading="lazy" />
    <div class="dest-overlay"></div>
    <span class="dest-name">Phnom Penh</span>
</a>

<!-- Kompot -->
<a asp-controller="Home" asp-action="BookTicket" asp-route-to="Kampot" class="dest-card" id="dest-card-kompot">
    <img src="~/designs/kompot.png" alt="Kompot" class="dest-img" loading="lazy" />
    <div class="dest-overlay"></div>
    <span class="dest-name">Kompot</span>
</a>

<!-- Battambang -->
<a asp-controller="Home" asp-action="BookTicket" asp-route-to="Battambang" class="dest-card" id="dest-card-battambang">
    <img src="~/designs/btb.png" alt="Battambang" class="dest-img" loading="lazy" />
    <div class="dest-overlay"></div>
    <span class="dest-name">Battambang</span>
</a>

<!-- Kompong Thom -->
<a asp-controller="Home" asp-action="BookTicket" asp-route-to="Kampong Thom" class="dest-card" id="dest-card-kpc">
    <img src="~/designs/kpc.png" alt="Kompong Thom" class="dest-img" loading="lazy" />
    <div class="dest-overlay"></div>
    <span class="dest-name">Kompong thom</span>
</a>

<!-- Bonteay Meanchey -->
<a asp-controller="Home" asp-action="BookTicket" asp-route-to="Banteay Meanchey" class="dest-card" id="dest-card-bmc">
    <img src="~/designs/bmc.png" alt="Bonteay Meanchey" class="dest-img" loading="lazy" />
    <div class="dest-overlay"></div>
    <span class="dest-name">Bonteay Meanchey</span>
</a>
```

Note: The existing CSS uses `.dest-card` with `position: relative; cursor: pointer;` — changing the tag from `<div>` to `<a>` preserves the layout since the CSS targets the class. The `<a>` tag inherits `color: inherit` from the body. All hover effects (`transform: scale(1.05)` on `.dest-img`, overlay transition) continue to work.

---

### 5.4 — Fix Register label: "Username" → "Full name"

**File:** `Views\Account\Register.cshtml` line 32

Change:
```html
<label asp-for="FullName">Username</label>
```

To:
```html
<label asp-for="FullName">Full name</label>
```

This matches the actual field purpose (the backend splits FullName into FirstName/LastName).

---

### Summary

| Step | File | What changes |
|------|------|--------------|
| 5.1 | `Views/Home/Index.cshtml` | Delete 196 hardcoded card lines + replace entire `@section Scripts` with dynamic rendering from `GET /api/Trip?StatusName=Open&PageSize=9` |
| 5.2 | `Views/Home/Index.cshtml` | Fix 8 dead `#` links in FAQ — View Map and Get Directions now point to Google Maps search |
| 5.3 | `Views/Home/Index.cshtml` | Wrap 6 destination cards in `<a>` tags linking to `/Home/BookTicket` with the destination pre-filled |
| 5.4 | `Views/Account/Register.cshtml` | Change label "Username" to "Full name" on line 32 |

**Total: 2 files touched, no backend changes, no new files.**
