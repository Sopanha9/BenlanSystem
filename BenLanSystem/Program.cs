using BenLanSystem.Data;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Implementations;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddIdentity<Staff, IdentityRole<long>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Register application services
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IBlogService, BlogService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed default admin user and roles (non-fatal if DB is unavailable)
try
{
    await SeedAdminAsync(app);
}
catch (Exception ex)
{
    Console.WriteLine($"Could not seed database - SQL Server may be unavailable. The app will start without seeded data. {ex.Message}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

app.Run();

async Task SeedAdminAsync(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Staff>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

    await db.Database.MigrateAsync();

    // Seed roles
    var roles = new[] { "Admin", "Staff", "Customer", "Driver" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<long>(role));
        }
    }

    // Seed admin user
    const string adminEmail = "admin@benlan.com";
    const string adminPassword = "Admin@123456";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is null)
    {
        adminUser = new Staff
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Admin",
            Position = "Admin",
            EmployeeId = "EMP-0001"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to seed admin user: {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    await SeedApplicationDataAsync(db, adminUser.Id);
}

async Task SeedApplicationDataAsync(ApplicationDbContext db, long adminUserId)
{
    var locationSeeds = new[]
    {
        new Location { Name = "Phnom Penh", Province = "Phnom Penh", AddressLine = "Central bus station", IsActive = true },
        new Location { Name = "Siem Reap", Province = "Siem Reap", AddressLine = "Siem Reap city terminal", IsActive = true },
        new Location { Name = "Kampot", Province = "Kampot", AddressLine = "Kampot town station", IsActive = true },
        new Location { Name = "Battambang", Province = "Battambang", AddressLine = "Battambang bus terminal", IsActive = true },
        new Location { Name = "Kampong Thom", Province = "Kampong Thom", AddressLine = "Kampong Thom station", IsActive = true },
        new Location { Name = "Banteay Meanchey", Province = "Banteay Meanchey", AddressLine = "Sisophon terminal", IsActive = true },
        new Location { Name = "Sihanoukville", Province = "Preah Sihanouk", AddressLine = "Sihanoukville station", IsActive = true }
    };

    foreach (var seed in locationSeeds)
    {
        if (!await db.Locations.AnyAsync(l => l.Name == seed.Name))
            db.Locations.Add(seed);
    }
    await db.SaveChangesAsync();

    var locations = await db.Locations.ToDictionaryAsync(l => l.Name);
    var routeSeeds = new[]
    {
        ("Phnom Penh", "Siem Reap", 318m, 330),
        ("Phnom Penh", "Kampot", 148m, 210),
        ("Phnom Penh", "Battambang", 291m, 300),
        ("Phnom Penh", "Sihanoukville", 230m, 240),
        ("Siem Reap", "Phnom Penh", 318m, 330),
        ("Battambang", "Phnom Penh", 291m, 300)
    };

    foreach (var (origin, destination, distance, minutes) in routeSeeds)
    {
        var startId = locations[origin].Id;
        var endId = locations[destination].Id;
        if (!await db.Routes.AnyAsync(r => r.StartLocationId == startId && r.EndLocationId == endId))
        {
            db.Routes.Add(new BenLanSystem.Models.Entities.Route
            {
                StartLocationId = startId,
                EndLocationId = endId,
                DistanceKm = distance,
                EstimatedMinutes = minutes,
                IsActive = true
            });
        }
    }
    await db.SaveChangesAsync();

    var vehicleSeeds = new[]
    {
        new Vehicle { PlateNumber = "PP-2A-1688", Brand = "Hyundai", Model = "Solati", SeatCapacity = 12, Transmission = "Auto", FuelType = "Gas", StatusName = "Active", ImageUrl = "/designs/bookticket/car.png" },
        new Vehicle { PlateNumber = "PP-2B-2026", Brand = "Toyota", Model = "HiAce", SeatCapacity = 15, Transmission = "Auto", FuelType = "Gas", StatusName = "Active", ImageUrl = "/designs/bookticket/car.png" },
        new Vehicle { PlateNumber = "SR-3C-8899", Brand = "Mercedes", Model = "Sprinter", SeatCapacity = 16, Transmission = "Auto", FuelType = "Hybrid", StatusName = "Active", ImageUrl = "/designs/bookticket/car.png" }
    };

    foreach (var seed in vehicleSeeds)
    {
        if (!await db.Vehicles.AnyAsync(v => v.PlateNumber == seed.PlateNumber))
            db.Vehicles.Add(seed);
    }
    await db.SaveChangesAsync();

    if (!await db.Trips.AnyAsync())
    {
        var routes = await db.Routes.Include(r => r.StartLocation).Include(r => r.EndLocation).Where(r => r.IsActive).ToListAsync();
        var vehicles = await db.Vehicles.Where(v => v.StatusName == "Active").ToListAsync();
        var day = DateTime.UtcNow.Date.AddDays(1);

        for (var i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            var vehicle = vehicles[i % vehicles.Count];
            var departure = day.AddDays(i % 5).AddHours(7 + (i % 4) * 2);
            db.Trips.Add(new Trip
            {
                RouteId = route.Id,
                VehicleId = vehicle.Id,
                DepartureTimeUtc = departure,
                ArrivalTimeUtc = route.EstimatedMinutes.HasValue ? departure.AddMinutes(route.EstimatedMinutes.Value) : departure.AddHours(4),
                BasePrice = route.DistanceKm.HasValue ? Math.Round(route.DistanceKm.Value / 22m, 2) : 12m,
                AvailableSeats = vehicle.SeatCapacity,
                StatusName = "Open"
            });
        }
        await db.SaveChangesAsync();
    }

    if (!await db.BlogPosts.AnyAsync())
    {
        db.BlogPosts.AddRange(
            new BlogPost
            {
                Title = "Planning a Smooth Trip Across Cambodia",
                Summary = "Simple tips for choosing routes, departure times, and pickup points.",
                Content = "A smooth bus journey starts with the right route and enough time before departure. BenLan recommends arriving at the station at least 15 minutes early, keeping your booking reference ready, and checking your pickup point before travel day.",
                ImageUrl = "/designs/blogImages/planning.png",
                AuthorId = adminUserId,
                IsPublished = true
            },
            new BlogPost
            {
                Title = "Why Travelers Love the Phnom Penh to Siem Reap Route",
                Summary = "A practical guide to one of Cambodia's most popular intercity journeys.",
                Content = "The Phnom Penh to Siem Reap route connects Cambodia's capital with the gateway to Angkor. Morning departures are popular for travelers who want to arrive before evening, while afternoon departures work well for flexible itineraries.",
                ImageUrl = "/designs/blogImages/card1.png",
                AuthorId = adminUserId,
                IsPublished = true
            });
        await db.SaveChangesAsync();
    }
}
