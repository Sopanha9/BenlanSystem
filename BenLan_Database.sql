-- ============================================================
-- BenLan System - Phase 1 Database Script
-- Target: SQL Server 2019+
-- Notes:
-- 1) This script focuses on core ticket-booking domain tables.
-- 2) ASP.NET Identity tables are intentionally NOT created manually here.
-- 3) Run EF Core migration first so dbo.AspNetUsers exists.
-- ============================================================

USE master;
GO

IF DB_ID('BenLanDB') IS NULL
BEGIN
    CREATE DATABASE [BenLanDB];
END
GO

USE [BenLanDB];
GO

-- ============================================================
-- 1) Identity pre-check
-- ============================================================
IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NULL
BEGIN
    PRINT 'Missing dbo.AspNetUsers. Run ASP.NET Identity migrations first, then run this script.';
    RETURN;
END
GO

-- ============================================================
-- 2) Locations
-- ============================================================
IF OBJECT_ID('dbo.Locations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Locations
    (
        LocationId        INT IDENTITY(1,1) NOT NULL,
        Name              NVARCHAR(100)     NOT NULL,
        Province          NVARCHAR(100)     NULL,
        AddressLine       NVARCHAR(200)     NULL,
        IsActive          BIT               NOT NULL CONSTRAINT DF_Locations_IsActive DEFAULT (1),

        CONSTRAINT PK_Locations PRIMARY KEY (LocationId),
        CONSTRAINT UQ_Locations_Name UNIQUE (Name)
    );
END
GO

-- ============================================================
-- 3) Routes
-- ============================================================
IF OBJECT_ID('dbo.Routes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Routes
    (
        RouteId            INT IDENTITY(1,1) NOT NULL,
        StartLocationId    INT               NOT NULL,
        EndLocationId      INT               NOT NULL,
        DistanceKm         DECIMAL(8,2)      NULL,
        EstimatedMinutes   INT               NULL,
        IsActive           BIT               NOT NULL CONSTRAINT DF_Routes_IsActive DEFAULT (1),

        CONSTRAINT PK_Routes PRIMARY KEY (RouteId),
        CONSTRAINT FK_Routes_StartLocation FOREIGN KEY (StartLocationId) REFERENCES dbo.Locations(LocationId),
        CONSTRAINT FK_Routes_EndLocation FOREIGN KEY (EndLocationId) REFERENCES dbo.Locations(LocationId),
        CONSTRAINT CK_Routes_DifferentLocations CHECK (StartLocationId <> EndLocationId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Routes_Start_End' AND object_id = OBJECT_ID('dbo.Routes')
)
BEGIN
    CREATE UNIQUE INDEX UX_Routes_Start_End
    ON dbo.Routes(StartLocationId, EndLocationId);
END
GO

-- ============================================================
-- 4) Vehicles
-- ============================================================
IF OBJECT_ID('dbo.Vehicles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vehicles
    (
        VehicleId          INT IDENTITY(1,1) NOT NULL,
        PlateNumber        NVARCHAR(20)      NOT NULL,
        Brand              NVARCHAR(50)      NULL,
        Model              NVARCHAR(50)      NULL,
        SeatCapacity       INT               NOT NULL,
        Transmission       NVARCHAR(20)      NULL,
        FuelType           NVARCHAR(20)      NULL,
        StatusName         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Vehicles_StatusName DEFAULT ('Active'),
        ImageUrl           NVARCHAR(300)     NULL,
        CreatedAtUtc       DATETIME2(0)      NOT NULL CONSTRAINT DF_Vehicles_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc       DATETIME2(0)      NULL,

        CONSTRAINT PK_Vehicles PRIMARY KEY (VehicleId),
        CONSTRAINT UQ_Vehicles_PlateNumber UNIQUE (PlateNumber),
        CONSTRAINT CK_Vehicles_SeatCapacity CHECK (SeatCapacity > 0),
        CONSTRAINT CK_Vehicles_Transmission CHECK (Transmission IS NULL OR Transmission IN ('Auto', 'Manual')),
        CONSTRAINT CK_Vehicles_FuelType CHECK (FuelType IS NULL OR FuelType IN ('Gas', 'EV', 'Hybrid')),
        CONSTRAINT CK_Vehicles_StatusName CHECK (StatusName IN ('Active', 'Maintenance', 'Retired'))
    );
END
GO

-- ============================================================
-- 5) Trips (schedules)
-- ============================================================
IF OBJECT_ID('dbo.Trips', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Trips
    (
        TripId              BIGINT IDENTITY(1,1) NOT NULL,
        RouteId             INT                  NOT NULL,
        VehicleId           INT                  NOT NULL,
        DepartureTimeUtc    DATETIME2(0)         NOT NULL,
        ArrivalTimeUtc      DATETIME2(0)         NULL,
        BasePrice           DECIMAL(10,2)        NOT NULL,
        AvailableSeats      INT                  NOT NULL,
        StatusName          NVARCHAR(20)         NOT NULL CONSTRAINT DF_Trips_StatusName DEFAULT ('Open'),
        CreatedAtUtc        DATETIME2(0)         NOT NULL CONSTRAINT DF_Trips_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc        DATETIME2(0)         NULL,

        CONSTRAINT PK_Trips PRIMARY KEY (TripId),
        CONSTRAINT FK_Trips_Route FOREIGN KEY (RouteId) REFERENCES dbo.Routes(RouteId),
        CONSTRAINT FK_Trips_Vehicle FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId),
        CONSTRAINT CK_Trips_BasePrice CHECK (BasePrice >= 0),
        CONSTRAINT CK_Trips_AvailableSeats CHECK (AvailableSeats >= 0),
        CONSTRAINT CK_Trips_StatusName CHECK (StatusName IN ('Open', 'Closed', 'Cancelled', 'Completed'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Trips_Route_Departure' AND object_id = OBJECT_ID('dbo.Trips')
)
BEGIN
    CREATE INDEX IX_Trips_Route_Departure
    ON dbo.Trips(RouteId, DepartureTimeUtc);
END
GO

-- ============================================================
-- 6) Bookings
-- ============================================================
IF OBJECT_ID('dbo.Bookings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bookings
    (
        BookingId          BIGINT IDENTITY(1,1) NOT NULL,
        TripId             BIGINT               NOT NULL,
        CustomerId         NVARCHAR(450)        NOT NULL,
        SeatsBooked        INT                  NOT NULL,
        UnitPrice          DECIMAL(10,2)        NOT NULL,
        TotalAmount        AS (SeatsBooked * UnitPrice) PERSISTED,
        BookingStatus      NVARCHAR(20)         NOT NULL CONSTRAINT DF_Bookings_BookingStatus DEFAULT ('Pending'),
        Notes              NVARCHAR(300)        NULL,
        CreatedAtUtc       DATETIME2(0)         NOT NULL CONSTRAINT DF_Bookings_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc       DATETIME2(0)         NULL,

        CONSTRAINT PK_Bookings PRIMARY KEY (BookingId),
        CONSTRAINT FK_Bookings_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips(TripId),
        CONSTRAINT FK_Bookings_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.AspNetUsers(Id),
        CONSTRAINT CK_Bookings_SeatsBooked CHECK (SeatsBooked > 0),
        CONSTRAINT CK_Bookings_UnitPrice CHECK (UnitPrice >= 0),
        CONSTRAINT CK_Bookings_BookingStatus CHECK (BookingStatus IN ('Pending', 'Confirmed', 'Cancelled', 'Completed'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Bookings_Trip' AND object_id = OBJECT_ID('dbo.Bookings')
)
BEGIN
    CREATE INDEX IX_Bookings_Trip
    ON dbo.Bookings(TripId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Bookings_Customer' AND object_id = OBJECT_ID('dbo.Bookings')
)
BEGIN
    CREATE INDEX IX_Bookings_Customer
    ON dbo.Bookings(CustomerId);
END
GO

-- ============================================================
-- 7) BookingPassengers (seat manifest)
-- ============================================================
IF OBJECT_ID('dbo.BookingPassengers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BookingPassengers
    (
        BookingPassengerId  BIGINT IDENTITY(1,1) NOT NULL,
        BookingId           BIGINT               NOT NULL,
        PassengerName       NVARCHAR(120)        NOT NULL,
        SeatNumber          NVARCHAR(10)         NOT NULL,

        CONSTRAINT PK_BookingPassengers PRIMARY KEY (BookingPassengerId),
        CONSTRAINT FK_BookingPassengers_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_BookingPassengers_Booking_Seat' AND object_id = OBJECT_ID('dbo.BookingPassengers')
)
BEGIN
    CREATE UNIQUE INDEX UX_BookingPassengers_Booking_Seat
    ON dbo.BookingPassengers(BookingId, SeatNumber);
END
GO

-- ============================================================
-- 8) Payments
-- ============================================================
IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId          BIGINT IDENTITY(1,1) NOT NULL,
        BookingId          BIGINT               NOT NULL,
        Amount             DECIMAL(10,2)        NOT NULL,
        PaymentMethod      NVARCHAR(30)         NOT NULL,
        PaymentStatus      NVARCHAR(20)         NOT NULL CONSTRAINT DF_Payments_PaymentStatus DEFAULT ('Pending'),
        TransactionRef     NVARCHAR(100)        NULL,
        PaidAtUtc          DATETIME2(0)         NULL,
        CreatedAtUtc       DATETIME2(0)         NOT NULL CONSTRAINT DF_Payments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
        CONSTRAINT FK_Payments_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(BookingId),
        CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0),
        CONSTRAINT CK_Payments_PaymentMethod CHECK (PaymentMethod IN ('ABA', 'ACLEDA', 'Wing', 'Cash', 'Card')),
        CONSTRAINT CK_Payments_PaymentStatus CHECK (PaymentStatus IN ('Pending', 'Paid', 'Failed', 'Refunded'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Payments_Booking' AND object_id = OBJECT_ID('dbo.Payments')
)
BEGIN
    CREATE INDEX IX_Payments_Booking
    ON dbo.Payments(BookingId);
END
GO

-- ============================================================
-- 9) Minimal seed data
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Locations WHERE Name = N'Phnom Penh')
BEGIN
    INSERT INTO dbo.Locations (Name, Province, AddressLine)
    VALUES
        (N'Phnom Penh', N'Phnom Penh', N'Central Station'),
        (N'Siem Reap', N'Siem Reap', N'City Terminal');
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Routes r
    INNER JOIN dbo.Locations s ON s.LocationId = r.StartLocationId
    INNER JOIN dbo.Locations e ON e.LocationId = r.EndLocationId
    WHERE s.Name = N'Phnom Penh' AND e.Name = N'Siem Reap'
)
BEGIN
    INSERT INTO dbo.Routes (StartLocationId, EndLocationId, DistanceKm, EstimatedMinutes)
    SELECT s.LocationId, e.LocationId, 315.00, 360
    FROM dbo.Locations s
    CROSS JOIN dbo.Locations e
    WHERE s.Name = N'Phnom Penh' AND e.Name = N'Siem Reap';
END
GO

PRINT 'BenLanDB schema is ready (Phase 1).';
GO
