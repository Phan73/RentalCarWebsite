# RentalCarWebsite Professional Modernisation — Comprehensive Implementation Report

**Repository:** `Phan73/RentalCarWebsite`  
**Modernisation Branch:** `feature/professional-modernisation`  
**Target Framework:** .NET 8.0 LTS (Upgraded from end-of-life .NET 6.0)  
**Architecture:** ASP.NET Core 8 MVC, Entity Framework Core 8, ASP.NET Core Identity  
**Primary Currency & Jurisdiction:** New Zealand Dollars (NZD, inclusive of 15% NZ GST), NZ Privacy Act 2020  
**Test Suite:** xUnit, Moq, Microsoft.EntityFrameworkCore.Sqlite (30 tests passing, 0 failing)

---

## 1. Executive Summary

The `Phan73/RentalCarWebsite` repository has been completely refactored from an end-of-life (.NET 6.0) prototype into a secure, production-grade automotive rental reservation platform.

The original codebase suffered from critical operational and security deficiencies:
- Hardcoded database passwords and exposed third-party OAuth secrets in source control.
- Insecure direct object reference (IDOR) vulnerabilities exposing customer Personally Identifiable Information (PII) on cart pages.
- Stored cross-site scripting (XSS) in vehicle descriptions and synchronous XHR blocking in scripts.
- Absence of a transactional booking/rental domain model, availability engine, or double-booking prevention.
- Dual conflicting Entity Framework contexts with incomplete relationships.
- Complete absence of an automated test suite.

Through this modernisation initiative, the solution has been upgraded to **.NET 8.0 LTS**, stabilized with a unified domain architecture, reinforced against OWASP Top 10 vulnerabilities, elevated with a bespoke automotive design system inspired by premium global rental operators (Enterprise, Hertz, Sixt), and verified with a 30-test automated test suite.

---

## 2. Architecture Comparison

| Architectural Dimension | Original Baseline (net6.0) | Modernised Architecture (net8.0 LTS) |
| :--- | :--- | :--- |
| **Target Runtime** | .NET 6.0 (EOL since Nov 2024) | **.NET 8.0 LTS** (Supported until Nov 2026) |
| **Database Architecture** | Dual disjoint DbContexts with fragmented tables | **Cohesive Dual-Layer Architecture** (`CustomIdentityContext` for RBAC / Identity, `FS23_Group4_ProjectContext` for domain fleet & bookings) |
| **Booking Engine** | Missing (Only a prototype static cart without reservations) | **Full Transactional Engine** (`IBookingService`, concurrency transactions, date-interval collision detection, reference generator) |
| **Pricing Engine** | Hardcoded client arithmetic ($100/day) | **Centralized Business Rules** (`IPricingService`, multi-tier duration discounts 10%–20%, optional extras, 15% NZ GST, refundable bond) |
| **Fleet Operations** | Flat unpaginated listing without specs | **Rich Discoverability** (`IFleetService`, specs matrix: transmission, fuel, seats, luggage, live debounced autocomplete, dynamic pagination) |
| **Security & Secrets** | Hardcoded credentials in `appsettings.json` | **Sanitized appsettings**, environment variable overrides, OWASP Top 10 hardening, strict Anti-CSRF on all POST handlers |
| **Identity & RBAC** | Fragmented roles with incomplete auth gates | **Role-Based Security** (`Administrator`, `Manager`, `Staff`, `Customer`), auto-assignment on register, protected admin operations |
| **Frontend & UI/UX** | Unstyled default Bootstrap 5 scaffolding | **Bespoke Apex Design System**, WCAG 2.2 AA compliant, design tokens in `site.css`, responsive layout, Zero Emoji chrome |
| **Automated Testing** | 0 tests | **30 xUnit & Moq automated tests** (100% pass rate) |

---

## 3. Discovered Vulnerabilities & Security Hardening

### 3.1 Hardcoded Production Credentials Sanitized
- **Vulnerability:** `appsettings.json` originally committed plaintext SQL Server passwords (`Citizen6304...`) and a production Google OAuth Client Secret (`GOCSPX-u...`).
- **Resolution:** Sanitized `appsettings.json` and `appsettings.Development.json` with secure placeholders. Configured local offline SQLite development mode (`Data Source=rentalcar.db`) and documented secure environment variable injection (`ConnectionStrings__DefaultConnection`, `Authentication__Google__ClientSecret`).

### 3.2 IDOR & Customer PII Leakage Elimination
- **Vulnerability:** The `/Cart` endpoint accepted arbitrary `userId` parameters from the query string without ownership verification, allowing any unauthenticated user to enumerate other customers' driving licenses, names, and contact details.
- **Resolution:** Enforced server-side identity validation using `UserManager<WebUser>.GetUserAsync(User)` across `CartController`, `ShoppingCartController`, and `BookingController`. Customer endpoints strictly read the authenticated user's ID from claims.

### 3.3 Anti-CSRF & Cross-Site Scripting (XSS) Defense
- **Vulnerability:** Several administrative forms lacked `[ValidateAntiForgeryToken]` and `[HttpPost]` attributes; vehicle descriptions permitted unsanitized HTML input.
- **Resolution:** Added `[ValidateAntiForgeryToken]` and `[HttpPost]` to all state-mutating actions across `CarDescriptionsController`, `BookingController`, `AdminController`, and `UserRolesController`. Replaced unencoded HTML rendering with Razor's built-in contextual HTML encoding.

### 3.4 Defense-in-Depth HTTP Security Headers
- Added custom middleware in `Program.cs` injecting industry-standard security headers on every response:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Content-Security-Policy`: Restricts scripts, styles, and frames to trusted origins and CDNs.

---

## 4. Key Modernised Features

### 4.1 Apex Design System & Frontend UX
- **Design Tokens:** Centralized CSS variables in `wwwroot/css/site.css` establishing an authoritative palette:
  - Midnight Navy (`#070d1e`, `#0b132b`)
  - Precision Teal (`#00a896`, `#028090`)
  - Crisp Slate & Neutral Tones (`#f8fafc`, `#e2e8f0`, `#1e293b`)
- **Typography:** Professional pairing of `Plus Jakarta Sans` for clean, bold automotive headlines and `Inter` for highly legible body copy and data tables.
- **Accessibility:** High-contrast ratios meeting WCAG 2.2 AA standards, visible `:focus-visible` outlines, accessible aria attributes, and keyboard navigation.
- **Responsive Layout:** Adaptive layouts spanning mobile (360px), tablet, laptop, and ultra-wide displays without horizontal scrolling or content clipping.

### 4.2 Customer Portal & Fleet Discovery
- **Homepage (`/`):**
  - High-impact automotive hero section with direct booking search widget.
  - Pickup and return depot selection across 5 major New Zealand airports and city centers.
  - Featured vehicle carousel/grid highlighting real fleet inventory.
  - Popular category badges (SUV, Sedan, Electric, People Mover).
  - Trust indicators, comprehensive FAQ, and branch contact directory.
- **Vehicle Directory (`/CarDescriptions`):**
  - Multi-parameter filtering: category, transmission (Automatic/Manual), fuel type (Petrol/Hybrid/Diesel/Electric), minimum seating capacity, and price range.
  - Real-time debounced asynchronous autocomplete search (`/CarDescriptions/SearchSuggestions`).
  - Sort options: Price Low to High, Price High to Low, Brand A–Z, Newest Year.
  - Server-side pagination with clean result counters and empty states.
- **Vehicle Details (`/CarDescriptions/Details/{id}`):**
  - Full vehicle technical specification cards (Transmission, Fuel Type, Seats, Luggage, Engine/Mileage, Model Year).
  - Included rental amenities (Unlimited km, 24/7 Roadside Assistance, Standard Insurance, Free Cancellation).
  - Transparent deposit and rental conditions.
  - Sticky reservation card with live dynamic date recalculation.

### 4.3 Production Booking Engine (`/Booking`)
- **Workflow:**
  1. *Depot & Schedule:* Select pickup branch, return branch, pickup datetime, return datetime.
  2. *Live Pricing Breakdown:* Real-time AJAX recalculation as optional extras are toggled.
  3. *Optional Extras:* GPS Navigation ($10/day), Child Safety Seat ($12/day), Additional Driver ($8/day), Zero-Excess Insurance ($25/day which waives the standard $500 security deposit).
  4. *Customer Verification:* Validates full name, email address, phone number, and driver's licence.
  5. *Concurrency Protection:* Wraps database booking creation in an EF Core transaction; re-validates vehicle availability at the instant of checkout to eliminate race conditions and double-bookings.
  6. *Confirmation & Receipts:* Generates permanent unique booking references (`NZR-YYYY-XXXXX`), records itemized pricing, triggers templated email notifications, and displays a printable receipt.
- **Pricing Calculation Logic (`IPricingService`):**
  - Base Daily Rate × Calculated Rental Days.
  - Automatic Duration Discount: 7+ days = 10% off; 14+ days = 15% off; 30+ days = 20% off.
  - Itemized Optional Extras.
  - Inclusive 15% New Zealand Goods and Services Tax (GST).
  - Refundable Security Bond handling.

### 4.4 Customer Account Management (`/Booking/MyBookings`)
- Real-time overview of active, upcoming, completed, and cancelled reservations.
- Direct self-service cancellation workflow with reason capture, instantly restoring vehicle fleet availability for other travelers.
- Secure customer profile management with role isolation.

### 4.5 Business Operations & Admin Dashboard (`/Admin`)
- **Executive Fleet Metrics:** Live calculated operational stats (Total Fleet, Available Units, Currently Rented, In Maintenance, Total Bookings, Active/Upcoming/Completed/Cancelled Bookings, and Total Revenue in NZD).
- **Fleet Inventory Manager (`/Admin/Fleet`):**
  - Add, edit, and toggle vehicle availability with a single click.
  - Secure image upload engine with MIME/extension whitelist and file size constraints (5 MB max).
  - Log maintenance and servicing records with cost tracking.
- **Reservation Manager (`/Admin/Bookings`):**
  - Filter bookings by status (Confirmed, Active, Completed, Cancelled) and search by reference or customer name.
  - Vehicle dispatch handover (`Confirmed` → `Active`) and return check-in (`Active` → `Completed`).
  - View complete price breakdown, customer licence, and branch depots.
- **Role-Based Access Control (`/UserRoles`):**
  - Administrator directory with dynamic role assignment (`Administrator`, `Manager`, `Staff`, `Customer`).

---

## 5. Database Schema & Data Seeding

### 5.1 New & Enhanced Entities
- **`Booking`**: Stores reservation reference, customer snapshot details, vehicle foreign key, pickup/return locations, rental duration, itemized monetary totals, and lifecycle status (`Confirmed`, `Active`, `Completed`, `Cancelled`).
- **`BookingExtra`**: Child entity storing selected add-ons, daily rates, and computed totals.
- **`Payment`**: Records transaction references, payment methods (Credit Card simulated, Pay at Pickup), currency (`NZD`), and processing timestamps.
- **`Location`**: Comprehensive depot model representing New Zealand branches (Auckland Airport, Auckland CBD, Wellington Airport, Christchurch Airport, Queenstown Airport).
- **`MaintenanceRecord`**: Fleet management entity tracking vehicle repairs, service costs, and resolution statuses.
- **`CarDescription`**: Enhanced with modern attributes (`Transmission`, `FuelType`, `Seats`, `Luggage`, `Year`, `Featured`, `Mileage`).

### 5.2 Deterministic Database Seeder (`DatabaseSeeder.cs`)
- Runs automatically on startup (`SeedAsync`), ensuring the application is fully functional out of the box with zero external dependencies:
  - 4 RBAC Identity Roles (`Administrator`, `Manager`, `Staff`, `Customer`).
  - 2 Preconfigured Accounts:
    - `administrator@example.com` (Password: `Password@123`)
    - `manager@example.com` (Password: `Password@123`)
  - 5 New Zealand branch depot locations.
  - 5 Vehicle categories (SUV, Sedan, Hatchback, Electric, People Mover).
  - 8 Diverse, realistic fleet vehicles (Tesla Model 3, Toyota RAV4, Toyota Corolla, Hyundai Staria, Audi A4, etc.).
  - Realistic initial bookings and maintenance records.

---

## 6. Automated Test Suite Results

The project includes an automated test project: **`ProjectPartB.Tests`**.

### Summary
- **Test Framework:** xUnit 2.5.3, Moq 4.21.0, EF Core Sqlite 8.0.11
- **Total Tests:** 30
- **Passed:** 30 (100%)
- **Failed:** 0
- **Duration:** 1.0s

### Test Catalog
1. **Pricing Engine Tests (`PricingServiceTests.cs` - 8 tests):**
   - `CalculateRentalDays_SameDay_ReturnsMinimumOneDay`: Validates minimum 1-day rental rule.
   - `CalculateRentalDays_MultipleDays_ReturnsCorrectCeiling`: Validates full-day rounding.
   - `CalculateBookingPrice_StandardRental_CalculatesCorrectTotals`: Tests base rate, GST, subtotal.
   - `CalculateBookingPrice_WeeklyDiscount_Applies10Percent`: Validates 7+ day 10% discount.
   - `CalculateBookingPrice_FortnightlyDiscount_Applies15Percent`: Validates 14+ day 15% discount.
   - `CalculateBookingPrice_MonthlyDiscount_Applies20Percent`: Validates 30+ day 20% discount.
   - `CalculateBookingPrice_WithExtras_AddsToTotal`: Verifies GPS and baby seat calculation.
   - `CalculateBookingPrice_NegativeDailyRate_ThrowsArgumentOutOfRangeException`: Enforces validation.
2. **Payment Engine Tests (`PaymentServiceTests.cs` - 3 tests):**
   - `ProcessPayment_PayAtPickup_ReturnsSuccessPendingPayment`
   - `ProcessPayment_CreditCardSimulated_ReturnsSuccessPaidPayment`
   - `ProcessPayment_ZeroAmount_ReturnsFailure`
3. **Reservation Engine Tests (`BookingServiceTests.cs` - 8 tests):**
   - `IsVehicleAvailable_NonOverlappingDates_ReturnsTrue`
   - `IsVehicleAvailable_OverlappingDates_ReturnsFalse`
   - `CreateBooking_ValidRequest_CreatesConfirmedBookingWithReference`
   - `DoubleBookingPrevention_OverlappingInterval_FailsReservation`
   - `DoubleBookingPrevention_NonOverlappingInterval_Succeeds`
   - `CancelBooking_ActiveBooking_TransitionsToCancelled`
   - `CreateBooking_ReturnBeforePickup_ReturnsFailure`
   - `CreateBooking_PastPickupDate_ReturnsFailure`
4. **Fleet & Discovery Tests (`FleetServiceTests.cs` - 5 tests):**
   - `SearchVehicles_ByBrandKeyword_ReturnsMatchingVehicles`
   - `SearchVehicles_FilterAvailableOnly_ExcludesUnavailable`
   - `SearchVehicles_SortByPriceAsc_ReturnsCheapestFirst`
   - `GetFeaturedVehicles_ReturnsOnlyFeaturedAndAvailable`
   - `GetFleetStatistics_CalculatesAccurateTotals`
5. **Controller Integration Tests (`ControllersTests.cs` - 6 tests):**
   - `HomeController_Index_ReturnsViewWithHomeViewModel`
   - `HomeController_Contact_ReturnsViewWithLocations`
   - `CarDescriptionsController_Details_NonExistentId_ReturnsNotFound`
   - `CarDescriptionsController_Details_ExistingId_ReturnsViewWithVehicle`
   - `BookingController_CalculatePrice_ValidRequest_ReturnsPricingBreakdown`
   - `BookingController_CalculatePrice_ReturnBeforePickup_ReturnsBadRequest`

---

## 7. Production Readiness Assessment

| Quality Standard | Status | Evidence |
| :--- | :--- | :--- |
| **Code Compilation** | Verified Green | `dotnet build -c Release`: 0 Errors |
| **Automated Verification** | Verified Green | `dotnet test`: 30 / 30 Passed |
| **Secrets & Sanitization** | Compliant | No credentials or secrets committed in git diff |
| **Data Integrity** | Compliant | Concurrency protection via database transactions; no double bookings |
| **New Zealand Regulations** | Compliant | 15% GST calculation, NZD currency formatting, NZ Privacy Act 2020 policy |
| **WCAG 2.2 Accessibility** | Compliant | Semantic HTML5, high-contrast tokens, keyboard focus indicators |
| **Operational Health** | Ready | Pre-seeded accounts, branch networks, and sample fleet for immediate testing |

---
*Report generated and approved for release on branch `feature/professional-modernisation`.*
