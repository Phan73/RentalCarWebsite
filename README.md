# 🚗 Apex Car Rentals – Professional Automotive Booking Platform

[![Vercel Deployment](https://img.shields.io/badge/Vercel-Live%20Production-000000?logo=vercel&logoColor=white)](https://rentalcarwebsite.vercel.app)
[![.NET 8.0 LTS](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC%208.0-2b5797)](https://learn.microsoft.com/aspnet/core)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0.11-68217a)](https://learn.microsoft.com/ef/core/)
[![Automated Tests](https://img.shields.io/badge/Tests-30%20Passed%20(100%25)-success)](ProjectPartB.Tests/)
[![WCAG 2.2 AA](https://img.shields.io/badge/Accessibility-WCAG%202.2%20AA-00a896)](ProjectPartB/wwwroot/css/site.css)
[![NZ Privacy Act 2020](https://img.shields.io/badge/Compliance-NZ%20Privacy%20Act%202020-0b132b)](ProjectPartB/Views/Home/Privacy.cshtml)

> 🌐 **Live Production URL:** [**https://rentalcarwebsite.vercel.app**](https://rentalcarwebsite.vercel.app)  
> 📦 **Vercel Project Dashboard:** [**https://vercel.com/zedkilltalon-2072/rentalcarwebsite**](https://vercel.com/zedkilltalon-2072/rentalcarwebsite)

A modern, production-ready car rental reservation platform built on **ASP.NET Core 8.0 LTS** and **Entity Framework Core 8.0**. Designed for high reliability, real-time concurrency-safe reservations, verified server-side pricing in New Zealand Dollars (**NZD**), role-based fleet operations, and a distinctive design system inspired by premium global mobility operators. Hosted on Vercel via containerized Fluid Compute runtime.

---

## 🌟 Key Capabilities & Features

### 1. 🔍 Fleet Discovery & Vehicle Directory
- **Multi-Attribute Filtering:** Filter inventory by vehicle category (SUV, Sedan, Hatchback, Electric, People Mover), transmission (Automatic/Manual), fuel type (Petrol, Hybrid, Diesel, Electric), minimum passenger seating, and price range.
- **Asynchronous Auto-Suggest:** Debounced live search endpoint (`/CarDescriptions/SearchSuggestions`) with real-time brand and model suggestions.
- **Vehicle Specifications Matrix:** Comprehensive technical data for every vehicle including seats, luggage capacity, transmission, fuel type, model year, and mileage policy (Unlimited km).
- **Amenities & Conditions:** Transparent rental policies, included 24/7 roadside assistance, standard insurance, and deposit requirements.

### 2. ⚡ Production-Grade Booking Engine
- **Multi-Step Checkout Workflow:** Select pickup/return depots, rental dates and times, configure optional add-ons, enter verified customer credentials, review full price breakdown, and confirm reservations.
- **Concurrency & Double-Booking Protection:** Concurrency-safe reservations wrapped in Entity Framework database transactions with date-interval overlap detection (`Pickup < ExistingReturn && Return > ExistingPickup`).
- **Verified Server-Side Pricing (`IPricingService`):**
  - Daily base rates calculated across rental duration.
  - Automatic Duration Discounts: **10% off** for 7+ days, **15% off** for 14+ days, **20% off** for 30+ days.
  - Optional Extras: GPS Navigation ($10/day), Child Safety Seat ($12/day), Additional Driver ($8/day), Zero-Excess Comprehensive Cover ($25/day which waives the standard $500 security deposit).
  - Inclusive 15% New Zealand Goods and Services Tax (**GST**).
  - Security deposit/bond tracking.
- **Instant Receipts & Email Notifications:** Unique reservation reference generation (`NZR-YYYY-XXXXX`), itemized receipt view, and automated transactional confirmation emails.

### 3. 👤 Customer Account Management
- **Reservation History:** Dedicated portal (`/Booking/MyBookings`) categorizing customer reservations into Active, Upcoming, Completed, and Cancelled.
- **Self-Service Cancellation:** Immediate cancellation with reason capture, instantly restoring vehicle availability to the public fleet.
- **Identity & Security:** Automatic role assignment to `Customer` upon registration, profile management, and account isolation preventing IDOR vulnerabilities.

### 4. 📊 Fleet Operations & Administrative Dashboard
- **Executive Metrics Dashboard (`/Admin`):** Real-time operational analytics (Total Fleet, Available Units, Currently Rented, In Maintenance, Total Bookings, Active/Upcoming/Completed/Cancelled Bookings, and Total Revenue in NZD).
- **Fleet Manager (`/Admin/Fleet`):** Add, edit, and toggle vehicle availability with one click; upload high-resolution photographs with strict MIME/extension validation; log maintenance and servicing costs.
- **Reservation Manager (`/Admin/Bookings`):** Search and filter customer bookings; vehicle dispatch handover (`Confirmed` → `Active`) and return check-in (`Active` → `Completed`).
- **Role-Based Access Control (`/UserRoles`):** Server-enforced role management (`Administrator`, `Manager`, `Staff`, `Customer`).

### 5. 🛡️ Security & Privacy Engineering
- **Sanitized Secrets:** No hardcoded credentials or API tokens in source control. Supports local zero-configuration SQLite and production SQL Server via environment variables.
- **Defense-in-Depth HTTP Security Headers:** Enforces `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, and `Referrer-Policy: strict-origin-when-cross-origin`.
- **Anti-CSRF & XSS Defense:** Strict `[ValidateAntiForgeryToken]` and `[HttpPost]` enforcement across all mutation endpoints; context-aware Razor HTML encoding.
- **NZ Privacy Act 2020 Compliance:** Transparent customer data collection, retention, and access policies detailed at `/Home/Privacy`.

---

## 🎨 Apex Design System

The application features a bespoke automotive rental design system designed to meet **WCAG 2.2 AA** accessibility standards:

- **Authoritative Palette:**
  - `Midnight Navy` (`#070d1e`, `#0b132b`) — Sophisticated primary branding.
  - `Precision Teal` (`#00a896`, `#028090`) — High-contrast interactive accents.
  - `Crisp Slate & Cloud` (`#f8fafc`, `#e2e8f0`, `#ffffff`) — Clean, readable surfaces.
- **Typography:** Bold, modern headlines in `Plus Jakarta Sans` paired with high-legibility body prose in `Inter`.
- **Restraint & Craftsmanship:** Clean SVG iconography, zero generic template emojis, accessible `:focus-visible` keyboard rings, and fully responsive layouts tested down to 360px mobile viewports.

---

## 🏗️ Technology Stack & Architecture

```
┌────────────────────────────────────────────────────────┐
│                   ASP.NET Core 8 MVC                   │
│   Controllers, Razor Views, ViewModels, Design Tokens  │
├────────────────────────────────────────────────────────┤
│                     Service Layer                      │
│   BookingService  ·  FleetService  ·  PricingService   │
│   PaymentService  ·  EmailNotificationService          │
├──────────────────────────┬─────────────────────────────┤
│   CustomIdentityContext  │ FS23_Group4_ProjectContext  │
│   ASP.NET Core Identity  │ Fleet, Depots, Bookings,    │
│   Roles, Users, Auth     │ Maintenance, Payments       │
├──────────────────────────┴─────────────────────────────┤
│            SQLite (Dev) / SQL Server (Prod)            │
└────────────────────────────────────────────────────────┘
```

- **Framework:** .NET 8.0 LTS (C# 12)
- **Web Layer:** ASP.NET Core MVC, Razor Pages
- **ORM:** Entity Framework Core 8.0.11
- **Authentication & RBAC:** ASP.NET Core Identity with role authorization
- **Database Engine:**
  - **Development:** SQLite 3 (zero configuration required)
  - **Production:** Microsoft SQL Server 2019+, Azure SQL Database, AWS RDS
- **Testing:** xUnit 2.5, Moq 4.21, EF Core SQLite In-Memory

---

## 🚀 Quickstart & Local Setup

### 1. Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later (`dotnet --version` ≥ 8.0.100).
- Git.

### 2. Clone and Checkout
```bash
git clone https://github.com/Phan73/RentalCarWebsite.git
cd RentalCarWebsite
git checkout feature/professional-modernisation
```

### 3. Build & Run Automated Tests
```bash
# Verify the entire solution builds cleanly with 0 errors
dotnet build ProjectPartB.sln

# Run the full 30-test automated test suite
dotnet test ProjectPartB.sln
```

### 4. Launch the Application
```bash
dotnet run --project ProjectPartB/ProjectPartB.csproj
```

Open your browser at `https://localhost:7043` (or the URL printed in your terminal).

---

## 🔑 Pre-Seeded Default Accounts & Branch Depots

On first launch in Development mode, [`DatabaseSeeder.cs`](ProjectPartB/Data/DatabaseSeeder.cs) automatically provisions the database with initial branch depots, categories, sample fleet, and administrative accounts:

| Role | Username / Email | Password | Access Level |
| :--- | :--- | :--- | :--- |
| **Administrator** | `administrator@example.com` | `Password@123` | Full dashboard, fleet management, reservations, role assignments |
| **Fleet Manager** | `manager@example.com` | `Password@123` | Fleet inventory, vehicle editing, maintenance, bookings |
| **Customer** | *(Self-Registration)* | *(User Choice)* | Browse fleet, create bookings, view booking history, self-cancellation |

### Available New Zealand Branch Depots:
1. **Auckland Airport** — *Ray Emery Drive, Mangere, Auckland 2022*
2. **Auckland CBD** — *100 Queen Street, Auckland Central 1010*
3. **Wellington Airport** — *Stewart Duff Drive, Rongotai, Wellington 6022*
4. **Christchurch Airport** — *Durey Road, Christchurch Airport 8053*
5. **Queenstown Airport** — *Sir Henry Wigley Drive, Frankton, Queenstown 9300*

---

## 🧪 Automated Test Suite (`ProjectPartB.Tests`)

The test suite covers all critical business rules, calculation services, double-booking validation, and controller workflows:

```text
Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30, Duration: 1 s
```

- **Pricing Service Tests (8 tests):** Minimum 1-day rental enforcement, rental days ceiling, base pricing calculation, duration discount schedule (10%, 15%, 20%), optional extras computation, negative rate rejection.
- **Booking & Concurrency Tests (8 tests):** Interval overlap collision detection, transactional double-booking prevention, cancellation state transitions, fleet availability recovery, invalid date range rejections.
- **Fleet Discovery Tests (5 tests):** Keyword search by brand/model, active availability filtering, sort by price (ascending/descending), featured vehicle selection, operational statistics aggregation.
- **Payment Service Tests (3 tests):** Simulated credit card processing, pay-at-pickup pending authorization, non-zero monetary validation.
- **Controller Integration Tests (6 tests):** Home view model binding, contact depot discovery, vehicle detail resolution, live AJAX pricing endpoint, HTTP 400 Bad Request on invalid date ranges.

---

## 📚 Comprehensive Modernisation Documentation

For technical deep dives, architectural designs, and operations guides, refer to the project documentation:

- [**`MODERNISATION_AUDIT.md`**](MODERNISATION_AUDIT.md) — Comprehensive audit of the legacy baseline, identified vulnerabilities, EOL dependencies, and architectural debt.
- [**`MODERNISATION_ROADMAP.md`**](MODERNISATION_ROADMAP.md) — The 12-phase execution plan and milestone delivery log.
- [**`TECHNICAL_ARCHITECTURE.md`**](TECHNICAL_ARCHITECTURE.md) — Technical specifications detailing domain models, database ERD, security patterns, and service layer contracts.
- [**`IMPLEMENTATION_REPORT.md`**](IMPLEMENTATION_REPORT.md) — Detailed report of all implemented modules, refactored components, and verification results.
- [**`DEPLOYMENT_GUIDE.md`**](DEPLOYMENT_GUIDE.md) — Production operations guide covering environment variables, Linux/Nginx systemd setup, Docker containerization, and production hardening.

---

## 📄 License & Attribution

Developed by **Luan Cao Phan** (`phan73@manukaumail.com`).  
Modernised to production commercial standards on branch `feature/professional-modernisation`.
