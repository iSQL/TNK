# TerminNaKlik (TNK) 🇷🇸

**TerminNaKlik (TNK)** is a modern, multi-vendor service booking platform designed to connect service providers with customers in Serbia. It allows vendors to manage their services, staff, and schedules, while customers can easily discover and book these services online.

## Vision

To create a seamless and efficient online booking experience for a variety of services, empowering local businesses and providing convenience to customers. The platform aims to be intuitive, reliable, and tailored to the needs of the Serbian market with bilingual support (Serbian and English).

## Key Features

### Core Functionality
* **Multi-Vendor Platform:** Supports numerous independent vendors offering various services.
* **Service Management:** Vendors can define and manage their service offerings, including descriptions, duration, and pricing.
* **Staff Management:** Vendors can manage their staff members, assign services, and set individual working hours.
* **Schedule & Availability Management:** Sophisticated scheduling tools for vendors to manage their availability and bookings.
* **Customer Booking:** Easy-to-use interface for customers to search, select, and book services.
* **User Authentication & Authorization:** Secure registration and login for customers, vendors, and administrators using ASP.NET Core Identity.

### Implemented & In Progress
* **User Identity Management:** Registration, login, role management (Customer, Vendor, SuperAdmin).
* **Vendor Business Profile Management:** Vendors can create and update their business profiles.
* **SuperAdmin Vendor Overview:** (Phase 2 in progress) SuperAdmins will be able to manage vendor profiles.
* **API-First Design:** Decoupled backend and frontend.
* **Localization Foundation:** Setup for Serbian (Latin & Cyrillic) and English.

### Planned Features
* **Complete SuperAdmin Dashboard:** Full CRUD operations for vendors, services, users.
* **Customer Appointment Management:** View, reschedule, and cancel bookings.
* **Real-time Notifications & Chat:** Using SignalR for instant updates and communication (e.g., booking confirmations, reminders, vendor-customer chat).
* **Email Notifications:** Integrated with Resend for transactional emails.
* **Advanced Search & Filtering:** For customers to easily find services.
* **Payment Gateway Integration:** (Post-launch V2)
* **Customer Reviews and Ratings:** (Post-launch V2)
* **Promotions and Discount Codes:** (Post-launch V2)
* **Calendar Integrations (Google, Outlook):** (Post-launch V2)

## Technology Stack

### Backend
* **Framework:** .NET 9 with ASP.NET Core for Web API
* **Architecture:** Ardalis Clean Architecture
* **API Endpoints:** FastEndpoints
* **Database:** PostgreSQL
* **ORM:** Entity Framework Core 9
* **Mediation:** MediatR for CQRS pattern implementation
* **Authentication:** ASP.NET Core Identity
* **Real-time Communication:** SignalR
* **Validation:** FluentValidation
* **Logging:** Serilog
* **Email Service:** Resend

### Frontend
* **Framework:** Angular (v19)
* **UI Components:** Syncfusion Angular UI Components (Community License)
* **State Management:** Angular Services with RxJS (BehaviorSubjects); considering NgRx for more complex state.
* **Internationalization (i18n):** @ngx-translate
* **HTTP Client:** Angular HttpClient

### DevOps & Tools
* **Version Control:** Git & GitHub
* **IDE:** Visual Studio 2022 (for backend), VS Code (for frontend)
* **CI/CD:** (To be set up - Azure DevOps)

## Architecture

The project follows the **Clean Architecture** principles for the backend, promoting separation of concerns, testability, and maintainability.

* **Domain (`TNK.Core`):** Contains enterprise-wide logic, entities, value objects, and domain events. It has no dependencies on other layers.
* **Application (`TNK.UseCases`):** Contains application-specific logic, CQRS commands/queries/handlers, DTOs, and interfaces for infrastructure services. Depends only on the Domain layer.
* **Infrastructure (`TNK.Infrastructure`):** Implements services defined in the Application layer, such as data access (EF Core, repositories), email sending, file storage, etc. Depends on the Application layer.
* **Presentation (`TNK.Web`):** The entry point of the application, exposing the API (ASP.NET Core, FastEndpoints). Handles HTTP requests, authentication, and calls into the Application layer.
    * **Frontend (`tnk-frontend`):** The Angular SPA, responsible for the user interface and interaction. Communicates with the backend via HTTP API calls.

This architecture ensures that business logic is independent of UI, database, and external frameworks.

## Project Structure

<details>
<summary>Project Structure</summary>

.
├── docs/                    # ADRs and other documentation
├── src/
│   ├── TNK.Core/            # Domain Layer: Entities, Value Objects, Domain Services, Interfaces
│   ├── TNK.UseCases/        # Application Layer: Commands, Queries, Handlers, DTOs
│   ├── TNK.Infrastructure/  # Infrastructure Layer: EF Core, Repositories, External Services
│   ├── TNK.Web/             # Presentation Layer: ASP.NET Core API, Endpoints, Auth
│   └── tnk-frontend/        # Frontend: Angular SPA
├── tests/
│   ├── TNK.UnitTests/
│   ├── TNK.IntegrationTests/
│   └── TNK.FunctionalTests/
├── .gitignore
├── global.json              # .NET SDK version (e.g., 9.0.100-rc.2.24474.11)
├── TerminNaKlik.sln         # Visual Studio Solution File
└── README.md                # This file
</details>

## Getting Started

### Prerequisites
* **.NET SDK `9.0.100-rc.2.24474.11`** (or the specific .NET 9 SDK version defined in `global.json`)
* **Node.js and npm:** Latest LTS version of Node.js (which includes npm).
* **Angular CLI:** Version compatible with Angular ~v19 (e.g., `npm install -g @angular/cli@19`).
* **PostgreSQL Server:** A running instance of PostgreSQL.
* **Git:** For cloning the repository.

### Backend Setup
1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/isql/TNK.git](https://github.com/isql/TNK.git)
    cd TNK
    ```
2.  **Configure Backend (`TNK.Web` project):**
    * Navigate to `src/TNK.Web`.
    * Rename `appsettings.Development.json.example` to `appsettings.Development.json` (or `appsettings.json`).
    * Update `appsettings.Development.json` with your local settings (Database Connection String, JWT Settings, Resend API Key).
3.  **Database Migrations:**
    * Ensure you have `dotnet-ef` tools installed for .NET 9.
    * Navigate to `src/TNK.Web` 
    * Run migrations:
        ```bash
        dotnet ef database update --project ../TNK.Infrastructure --startup-project .
        ```
4.  **Run the Backend:**
    * Navigate to `src/TNK.Web`.
    * Start the API:
        ```bash
        dotnet run
        ```
    * The API should be accessible, e.g., at `https://localhost:7042`. Swagger UI at `https://localhost:7042/swagger`.

### Frontend Setup (`tnk-frontend`)
1.  **Navigate to the frontend directory:**
    ```bash
    cd src/tnk-frontend
    ```
2.  **Install dependencies:**
    ```bash
    npm install
    ```
3.  **Configure Environment:**
    * Update `src/tnk-frontend/src/environments/environment.ts` and `src/tnk-frontend/src/environments/environment.prod.ts` to point `apiUrl` to your running backend.
4.  **Run the Frontend:**
    ```bash
    ng serve
    ```
    * Access at `http://localhost:4200/`.

## Running Tests

### Backend Tests
1.  Navigate to the solution root directory (`cd TNK`).
2.  Execute: `dotnet test`

### Frontend Tests
1.  Navigate to `src/tnk-frontend`.
2.  Run unit tests: `ng test`
3.  Run e2e tests: `ng e2e`

## API Documentation

Swagger/OpenAPI documentation is available at `https://localhost:7042/swagger` (or your backend URL + `/swagger`) when the backend is running.

## Contributing

Contributions are welcome! Please read our `CONTRIBUTING.md` for guidelines on how to contribute to the project, including coding standards, pull request processes, and more.

Also, please adhere to our `CODE_OF_CONDUCT.md`.

**Additionally, if you use any part of this code, we kindly request that you provide credit to the TerminNaKlik (TNK) project and its contributors.**

## Acknowledgements

This project was started using the **Ardalis Clean Architecture solution template** by Steve "ardalis" Smith. We are grateful for this foundation which has significantly helped in structuring the backend. You can find more about the template [here](https://github.com/ardalis/CleanArchitecture).

---

Happy Coding!
