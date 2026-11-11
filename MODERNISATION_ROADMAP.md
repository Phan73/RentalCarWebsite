# Modernisation Roadmap — RentalCarWebsite

**Project:** Commercial Car Rental Platform Modernisation  
**Target Solution:** ASP.NET Core MVC on .NET 8 LTS (C# 12)  
**Branch:** `feature/professional-modernisation`  
**Execution Strategy:** Autonomous, phased, non-destructive, test-verified  

---

## Roadmap Overview & Phase Tracking

```
Phase 1: Full Repository Audit [COMPLETED]
   └── Codebase inspection, vulnerability analysis, architecture mapping
Phase 2: Professional Design System & Tokens [NEXT]
   └── Design tokens, typography, automotive aesthetics, WCAG 2.2 AA components
Phase 3: Customer Experience Modernisation
   └── Homepage, vehicle discovery, filtering, search, vehicle detail pages
Phase 4: Professional Booking Engine & Pricing Service
   └── Multi-step reservation flow, date math, overlap prevention, concurrency locks
Phase 5: Customer Accounts & Profile Management
   └── Identity hardening, profile management, booking history, cancellation workflow
Phase 6: Administration Operations & Fleet Management
   └── Live dashboard metrics, fleet CRUD, maintenance tracking, booking state management
Phase 7: Database Architecture & Relational Integrity
   └── Unified DbContext, entity relationships, migrations, seed data, SQLite/SQL Server dual support
Phase 8: Payments & Notification Architecture
   └── Provider-agnostic payment abstraction (Stripe/PayPal/Cash-on-pickup), email notifications
Phase 9: Security, Privacy & Compliance Hardening
   └── OWASP ASVS verification, secret sanitation, rate limiting, anti-CSRF, NZ Privacy Act 2020
Phase 10: Performance, Core Web Vitals & SEO
   └── Bundle optimization, lazy loading, OpenGraph metadata, structured schema, clean URLs
Phase 11: Testing & Quality Assurance
   └── Automated xUnit test suite, pricing unit tests, booking overlap tests, E2E verification
Phase 12: Final Polish & Production Readiness Deliverables
   └── Cross-browser verification, documentation deliverables, clean git commit log
```

---

## Detailed Phase Breakdown

### Phase 1: Repository Audit & Baseline Verification (Completed)
- [x] Full source code inspection (.NET version, dependencies, architecture, git history).
- [x] Security vulnerability discovery (hardcoded credentials, IDOR, XSS, file upload).
- [x] Verification of live SQL Server database schema and row counts.
- [x] Production of `MODERNISATION_AUDIT.md`.
- [x] Creation of dedicated feature branch: `feature/professional-modernisation`.

---

### Phase 2: Professional Design System & Frontend Architecture
- **Objective:** Replace default Bootstrap scaffolding with a distinctive, commercial-grade automotive rental identity.
- **Milestones:**
  - `tokens.css`: Centralized CSS variables for palette (Deep Obsidian Navy `#0B132B`, Electric Cyan/Teal accent `#00A896`, Slate Gray `#4A5568`, Warm Surface `#F8FAFC`), typography (Inter / Plus Jakarta Sans), elevation shadows, border radii, and transition timing.
  - Reusable components: Vehicle cards, booking bar, status badges, price breakdown tables, accessible form inputs.
  - Zero generic AI design tells: No warm cream/terracotta templates, no arbitrary word highlights, no emoji-as-icons.
  - Responsive layouts: Seamless scaling from 360px mobile viewports to 4K ultra-wide screens.
  - Accessibility: Focus rings, high-contrast text ratios conforming to WCAG 2.2 AA.

---

### Phase 3: Customer Website Redesign
- **Objective:** Build an intuitive, high-conversion storefront for car rental discovery.
- **Milestones:**
  - **Homepage (`Views/Home/Index.cshtml`):**
    - High-impact automotive hero section with instant search widget (Pickup/Return Location, Date, Time).
    - Category quick-links (Sedan, SUV, Luxury, Van, Electric/Hybrid).
    - Featured vehicle showcase pulled dynamically from database.
    - Value proposition & rental policies (Transparent pricing, unlimited km options, 24/7 roadside assistance).
    - Professional footer with NZ contact details and compliance notices.
  - **Vehicle Catalogue (`Views/CarDescriptions/Index.cshtml`):**
    - Search by make, model, category, transmission, and seating capacity.
    - Dynamic sorting (Price low-to-high, high-to-low, brand alphabetical).
    - Clean vehicle card layout featuring high-res imagery, passenger count, luggage capacity, transmission badge, daily rate in NZD, and direct CTA.
  - **Vehicle Details (`Views/CarDescriptions/Details.cshtml`):**
    - Comprehensive vehicle gallery, technical specifications, included features.
    - Transparent deposit and insurance excess disclosures.
    - Direct "Reserve This Vehicle" booking action.

---

### Phase 4: Professional Booking Engine & Pricing Architecture
- **Objective:** Implement a rock-solid, multi-step reservation system with server-side validation and concurrency safety.
- **Milestones:**
  - Multi-step workflow:
    1. Location & Dates selection.
    2. Vehicle selection.
    3. Rental extras selection (GPS, Baby Seat, Additional Driver, Excess Reduction).
    4. Driver & customer details verification.
    5. Itemised price breakdown (Daily rate × days, extras, NZ GST 15%, total deposit).
    6. Reservation confirmation with unique booking reference (e.g., `NZR-2026-XXXX`).
  - Business rules enforcement:
    - Return date must be after pickup date (minimum 1 day rental).
    - Vehicle availability validation: Check for overlapping active reservations.
    - Concurrency protection: Optimistic concurrency / transactional locking during booking creation.
    - Status lifecycle: `Pending` -> `Confirmed` -> `Active` -> `Completed` -> `Cancelled`.

---

### Phase 5: Customer Accounts & Self-Service Portal
- **Objective:** Empower customers with secure account management and reservation history.
- **Milestones:**
  - Account registration assigning proper default customer role.
  - Secure login, profile editing (Name, Phone, Address, Driving Licence).
  - Customer Booking Dashboard: View upcoming bookings, past rentals, and detailed receipts.
  - Self-service cancellation workflow adhering to business cancellation policy rules.

---

### Phase 6: Administration & Fleet Management Platform
- **Objective:** Deliver an operational business management suite for staff and administrators.
- **Milestones:**
  - Dashboard overview: Real-time operational cards (Total fleet, available vehicles, rented vehicles, active bookings, monthly revenue).
  - Fleet management: Full vehicle CRUD, image upload validation, category assignment, maintenance status toggle.
  - Booking management: Filterable reservation grid, status updates (Mark as Picked Up / Returned / Cancelled), handover notes.
  - Role-based authorization: Granular access control (`Administrator`, `Manager`, `Staff`, `Customer`).

---

### Phase 7: Database Architecture & Relational Modernisation
- **Objective:** Unify EF Core contexts, introduce relational domain entities, and maintain backward compatibility.
- **Milestones:**
  - Upgrade target framework to .NET 8 LTS (`net8.0`).
  - Introduce `Booking`, `Location`, `BookingExtra`, and `Payment` entities.
  - Clean up compiler warnings (CS0114, CS8618).
  - Provide automated database migration / seeding and dual-database configuration (SQL Server for production/remote, SQLite for local/offline testing).

---

### Phase 8: Payments & Notification Services
- **Objective:** Provide a production-grade checkout abstraction and customer email notifications.
- **Milestones:**
  - Payment service abstraction (`IPaymentService`) supporting Mock/Sandbox payment, Cash-on-pickup, and Stripe/PayPal readiness.
  - Email notification service (`IEmailNotificationService`) generating HTML receipts for booking confirmation and cancellation.

---

### Phase 9: Security & Privacy Hardening
- **Objective:** Attain OWASP ASVS 5.0 compliance and NZ Privacy Act alignment.
- **Milestones:**
  - Remove all plaintext secrets from configuration files; provide `.env` / user-secrets architecture.
  - Validate all anti-forgery tokens on HTTP POST actions.
  - Strict image upload validation (whitelist MIME types, random GUID file naming).
  - Security headers (HSTS, X-Content-Type-Options, X-Frame-Options, Referrer-Policy).

---

### Phase 10: Performance, Core Web Vitals & SEO
- **Objective:** Optimize loading times, mobile responsiveness, and search visibility.
- **Milestones:**
  - Responsive image loading and CSS bundling.
  - SEO metadata: Meta tags, OpenGraph tags, semantic schema for car rental services.

---

### Phase 11: Testing & Quality Assurance
- **Objective:** Verify functionality with an automated test suite.
- **Milestones:**
  - Create xUnit test project (`ProjectPartB.Tests`).
  - Unit tests for `PricingService` (rates, durations, discounts, taxes).
  - Unit tests for `BookingService` (availability check, overlapping date prevention).
  - Controller integration tests and authorization checks.

---

### Phase 12: Documentation & Final Polish
- **Objective:** Complete all deliverables and assess production readiness.
- **Milestones:**
  - Produce `TECHNICAL_ARCHITECTURE.md`, `IMPLEMENTATION_REPORT.md`, `DEPLOYMENT_GUIDE.md`.
  - Verify every button, link, form, and validation state across desktop and mobile.
  - Clean commit history on `feature/professional-modernisation`.
