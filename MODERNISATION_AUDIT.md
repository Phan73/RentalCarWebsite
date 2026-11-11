# Modernisation Audit — RentalCarWebsite

**Repository:** `Phan73/RentalCarWebsite`  
**Branch:** `feature/professional-modernisation`  
**Audit Date:** November 2026  
**Auditor:** Principal Software Architect & QA Lead  
**Assessment Framework:** OWASP ASVS 5.0, .NET Architecture Standards, WCAG 2.2 AA  

---

## 1. Executive Summary

A comprehensive architectural, security, database, and functional audit was conducted on the `Phan73/RentalCarWebsite` repository. 

While the project was originally conceptualised as a 7-table automotive rental platform with customer and administrative interfaces, the actual codebase is in an early, incomplete prototype state built on an out-of-support .NET 6.0 MVC template. The core vehicle rental booking engine described in the README does not exist; instead, an incomplete retail e-commerce shopping cart was partially scaffolded. Critical security vulnerabilities—including hardcoded database and OAuth credentials in source control, broken access controls (IDOR), cross-site scripting (XSS), missing CSRF protections, and unrestricted file uploads—were identified alongside architectural inconsistencies and a complete lack of automated testing.

This audit details all findings across architecture, security, database integrity, business logic, and user experience, providing the foundation for professional modernisation.

---

## 2. Technology Stack: Reality vs. Specification

| Dimension | README Specification | Actual Implementation Found | Modernisation Target (.NET 8 LTS) |
|---|---|---|---|
| **Target Framework** | ASP.NET Core MVC | .NET 6.0 (`net6.0`) — **End of Life (EOL)** | Upgrade to **.NET 8.0 LTS** |
| **Language** | C# | C# 10 with partial nullable reference warnings (53 warnings) | C# 12 on .NET 8 with clean nullable diagnostics |
| **Database** | SQL Server (7 tables: UserType, User, CarType, CarDescription, Rental, CarAvailability, UserDescription) | SQL Server 2019/2022 hosted remotely on `citizen.manukautech.info:6304`. Tables found: `CarDescription` (2 rows), `CarType` (3 rows), `AspNetUsers` (9 rows), `CartItems` (0 rows), `Memberships` (0 rows). **`Rental` table is completely missing.** | Unified relational domain model with real `Bookings`/`Rentals`, `Locations`, `BookingExtras`, and `Payments`, with dual SQL Server & SQLite fallback support. |
| **ORM / Data Access** | Entity Framework | Entity Framework Core 6.0.23 split across two competing contexts (`CustomIdentityContext` & `FS23_Group4_ProjectContext`). | Unified EF Core 8.0 context architecture with proper relational configuration and concurrency handling. |
| **Authentication** | User roles: admin, customer | ASP.NET Core Identity with `WebUser`, 4 roles seeded (`Administrator`, `Manager`, `Editor`, `Visitor`). Self-registration fails to assign any role. | Hardened ASP.NET Core Identity with role-based policies (`Administrator`, `Manager`, `Staff`, `Customer`). |
| **Booking Engine** | Full online reservation system | **Non-existent.** Replaced by a mock "Shopping Cart" with "Quantity" inputs and an unverified client-side PayPal button redirecting to a 404 URL (`/product/success`). | Production multi-step rental engine with real date math, availability validation, transactional reservation locking, and price calculation. |
| **Frontend & UI** | Responsive web design | Default Bootstrap 5.1 layout. `site.css` is completely empty (0 bytes). `site.js` is empty. No custom brand, inconsistent navigation, invalid HTML attributes. | Custom automotive rental design system, responsive tokens, WCAG 2.2 AA compliant, accessible keyboard navigation, no AI tropes. |
| **Testing** | Not specified | **Zero tests.** No test projects, no unit tests, no integration tests, no test fixtures. | xUnit test suite covering pricing calculations, availability validation, concurrency protection, and booking workflows. |

---

## 3. Critical Security & Privacy Findings (OWASP ASVS 5.0)

### 3.1 Hardcoded Secrets in Source Control (VULN-01) — **CRITICAL**
- **Location:** `ProjectPartB/appsettings.json` (lines 10, 15-16).
- **Issue:** 
  1. Plaintext SQL Server credentials (`UID=FS23_Group4;PWD=fBit$85676`) pointing to an external institutional server (`citizen.manukautech.info:6304`) are committed directly to Git.
  2. Google OAuth client credentials (`ClientId` and `ClientSecret: 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`) are committed in plaintext.
- **Risk:** Compromise of database data, unauthorized external database tampering, credential theft, and OAuth account impersonation.
- **Remediation:** Remove plaintext secrets from `appsettings.json`. Introduce user-secrets for local development, environment variable overrides for production, and provide sanitized configuration templates.

### 3.2 Insecure Direct Object Reference & Data Exposure in Cart (VULN-02) — **HIGH**
- **Location:** `ProjectPartB/Controllers/CartController.cs` (lines 20–48) and `Views/Cart/Index.cshtml`.
- **Issue:** The `CartController.Index` action lacks any `[Authorize]` attribute. When invoked by any anonymous user, it executes `await _userManager.Users.ToListAsync()` and iterates over every registered user in the database, passing each user's full name, driving license number, and cart items to the view.
- **Risk:** Complete privacy violation under the New Zealand Privacy Act 2020. Severe identity leak exposing customer driving license numbers to unauthenticated internet traffic.
- **Remediation:** Enforce role-based authorization, isolate carts to the authenticated session/user, and eliminate bulk customer PII dumping.

### 3.3 Broken Access Control & Privilege Escalation in Role Management (VULN-03) — **HIGH**
- **Location:** `ProjectPartB/Controllers/UserRoleController.cs` (lines 12, 33, 230–256).
- **Issue:** 
  1. The controller-level `[Authorize(Roles = "Administrator")]` attribute is commented out (`//[Authorize(Roles = "Administrator")]`).
  2. The `UpdateRole` HTTP POST action has no authorization attribute and lacks `[ValidateAntiForgeryToken]`.
- **Risk:** Any unauthenticated visitor or malicious actor can issue a POST request to `/UserRoles/UpdateRole` and assign the `Administrator` role to any user ID, gaining full system compromise.
- **Remediation:** Re-enable strict `[Authorize(Roles = "Administrator")]` across the controller, add anti-forgery token validation, and eliminate insecure role reassignment pathways.

### 3.4 Cross-Site Scripting (XSS) Vulnerabilities (VULN-04) — **HIGH**
- **Location:** 
  1. `ProjectPartB/Views/CarDescriptions/Index.cshtml` (line 70): `@Html.Raw(item.Description)`. Raw HTML rendering without sanitization enables stored XSS via vehicle descriptions.
  2. `ProjectPartB/Views/CarDescriptions/Index.cshtml` (lines 134–154): Inline JavaScript dynamically injects unsanitized strings directly into `autoSuggestContainer.innerHTML`.
- **Risk:** Session hijacking, cookie theft, and defacement through malicious vehicle descriptions or search inputs.
- **Remediation:** Remove `@Html.Raw()`, use standard Razor contextual HTML encoding, and replace dynamic string concatenation with safe DOM manipulation.

### 3.5 Unrestricted File Upload in Vehicle Management (VULN-05) — **HIGH**
- **Location:** `ProjectPartB/Controllers/CarDescriptionsController.cs` (lines 107–118, 159–169).
- **Issue:** Vehicle image uploads are saved directly into `wwwroot/uploads` using the original file extension without checking file signatures, MIME types, or extension whitelists.
- **Risk:** Attackers with editor access could upload executable scripts (e.g., `.asp`, `.cshtml`, `.exe`, `.svg` with embedded scripts) into web-accessible directories, resulting in remote code execution (RCE).
- **Remediation:** Enforce strict file extension whitelisting (`.jpg`, `.jpeg`, `.png`, `.webp`), validate MIME headers, generate sanitized GUID filenames, and enforce upload size limits.

### 3.6 Synchronous XMLHttpRequest UI Freezes (VULN-06) — **MEDIUM**
- **Location:** `ProjectPartB/Views/CarDescriptions/Index.cshtml` (lines 112–115):
  `var url = "/CarDescriptions/indexAjax?searchString" + searchString;`
  `xmlhttp.open("GET", url, false);`
- **Issue:** Uses deprecated synchronous XHR (`open(..., false)`), freezing the browser rendering thread on every keystroke. Furthermore, the query string is malformed (`?searchString` missing `=`).
- **Remediation:** Modernize with non-blocking `fetch()` API, debouncing, and proper URL parameter encoding.

### 3.7 Client-Controlled Pricing & Unverified Payments (VULN-07) — **CRITICAL**
- **Location:** `ProjectPartB/Views/ShoppingCart/Index.cshtml` (lines 52, 61–70, 77).
- **Issue:** Total payment amount is read from an unprotected hidden HTML input (`#GrandTotal`) and sent to PayPal. Upon client-side callback (`onApprove`), the browser simply redirects to `/product/success` with no server-side order capture, webhook validation, or database persistence.
- **Risk:** Complete financial manipulation: a client can alter `#GrandTotal` to `$0.01` in DevTools. Furthermore, no actual booking or transaction is recorded on the backend.
- **Remediation:** Centralize price calculations strictly on the server. Never trust client-submitted prices. Implement secure payment verification workflows with proper status lifecycle tracking.

---

## 4. Architectural & Code Quality Findings

### 4.1 Dual Conflicting DbContexts
The application registers two separate Entity Framework Core database contexts pointing to the exact same database:
1. `CustomIdentityContext` (inheriting `IdentityDbContext<WebUser>`): Manages ASP.NET Identity tables and `Membership`.
2. `FS23_Group4_ProjectContext` (inheriting `DbContext`): Scaffolded from the database, defining its own `AspNetUser` entity alongside `CarDescription`, `CarType`, `CarAvailability`, etc.

This leads to:
- 53 compiler warnings regarding member hiding (`AspNetUser.Id` hiding `IdentityUser<string>.Id`).
- Fractured database transactions across contexts.
- Inability to establish clean navigation properties between Identity users and operational entities like bookings.

### 4.2 Sync-Over-Async & Thread Pool Starvation
- `UserRolesController.cs` (lines 27–29): Calls `.GetAwaiter().GetResult()` inside async actions.
- Causes thread starvation and risks deadlocks under concurrent server load.

### 4.3 N+1 Query Anti-Pattern
- `UserRolesController.Users()` (lines 125–144) iterates over all users and calls `await _userManager.GetRolesAsync(user)` per user, executing $N+1$ database round-trips.

### 4.4 Missing Business Domain Entities
The current database is missing core business entities required for a commercial car rental platform:
- `Rental` / `Booking`: Missing start/end dates, rental locations, calculated price, deposit, status, and customer foreign keys.
- `Location`: No concept of branch offices (e.g., Auckland Airport, Christchurch City, Wellington Terminal).
- `BookingExtra`: No support for GPS, baby seats, additional drivers, or insurance tiers.
- `Payment`: No record of payment method, transaction references, or audit trails.
- `VehicleSpecification`: Missing transmission (Automatic/Manual), fuel type (Petrol/Hybrid/EV), seating capacity, and luggage capacity.

---

## 5. UI/UX & Design Deficiencies

1. **Empty Stylesheets & Scripts:** `site.css` is completely empty; all pages rely on default Bootstrap 5 with mismatched tables and generic text.
2. **Broken Navigation:** Duplicate links in `_Layout.cshtml` ("Car type editor" appears twice), invalid HTML tags (`<li class="nav-item" t>`), unstyled navbar.
3. **Flawed Catalog Experience:** Vehicle listing displays raw HTML description, lacks category/price filters, lacks transmission and passenger icons, and shows administrative CRUD links to ordinary visitors.
4. **No Customer Account Portal:** The link `asp-page="/Account/Manage/Index"` leads to a non-existent page, leaving customers unable to view profile details or past bookings.
5. **No Booking Engine:** No date pickers, no location selection, no transparent price breakdowns.

---

## 6. Audit Verdict & Priority Action Items

The codebase requires deep structural modernisation while preserving the core .NET MVC + SQL Server architectural intent. The next phases will execute:
1. **P1 — Foundation:** Upgrade to .NET 8 LTS, eliminate compiler warnings, unify DbContext architecture, implement clean service layer, and sanitize secrets.
2. **P2 — Design System:** Craft a bespoke automotive rental design system with tokens, WCAG 2.2 AA accessibility, and responsive layouts.
3. **P3 — Customer Website:** Rebuild Homepage, Vehicle Catalogue, Search/Filters, and Vehicle Details with real specifications.
4. **P4 — Booking Engine:** Implement end-to-end multi-step reservation engine with server-side pricing, date validation, and concurrency protection.
5. **P5 — Customer & Admin Portals:** Deliver customer booking history and a comprehensive administration dashboard for fleet and reservation management.
6. **P6 — Testing & QA:** Establish automated xUnit test suites and verify end-to-end flows.
