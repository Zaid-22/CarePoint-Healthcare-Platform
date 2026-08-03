# CarePoint — Comprehensive Healthcare & Appointment Platform

CarePoint is a modern, enterprise-grade healthcare management and specialist appointment platform built with ASP.NET Core Web API and React + TypeScript + Vite. It enables seamless medical appointment scheduling, real-time doctor availability management, credential verification, and administrative workflow orchestration.

---

## Features & Role Portals

### Doctor Portal (`/doctor/dashboard`, `/doctor/profile`)
- **Working Hours & Availability Manager**: Configure custom weekly consultation shifts by day of the week, start/end times, and slot duration (15, 20, 30, 45, or 60 minutes).
- **Practice Details**: Set consultation pricing in Jordanian Dinar (JOD), update bio, phone number, and upload high-resolution profile portraits.
- **Specialty Selection**: Multi-select clinical specialty tags for practitioner directory indexing.
- **Patient Schedule**: Real-time view of today's scheduled consultations and pending patient requests.

### Administrator Portal (`/admin/dashboard`, `/admin/specialties`)
- **Practitioner Credential Verification**: Review new doctor applications with 1-click Approve or Reject workflows.
- **System Analytics**: Real-time dashboard tracking total registered practitioners, pending applications, verified doctors, and active medical categories.
- **Clinical Specialty Management**: Add, edit, or deactivate medical specialties quietly in the background without UI lag.

### Patient Portal (`/dashboard`, `/find-doctors`, `/my-appointments`)
- **Specialist Search**: Filter accredited doctors by clinical specialty, practitioner name, and consultation fees in JOD.
- **Strict Real-Time Booking**: View available 30-minute booking slots based on real-time doctor working hours with instant conflict detection.
- **Personalized Health Dashboard**: View upcoming appointments, booking history, and personal health metrics.
- **Real-Time Notification Drawer**: High-contrast, top-anchored popover for instant appointment status alerts.

---

## Technology Stack

| Layer | Technologies Used |
| :--- | :--- |
| **Frontend** | React 18, TypeScript, Vite, Redux Toolkit, Vanilla CSS Design System |
| **Backend** | ASP.NET Core 10.0 Web API, Clean Architecture |
| **Database & ORM** | Entity Framework Core, SQL Server |
| **Authentication** | ASP.NET Core Identity, JWT Bearer Tokens, Role-Based Access Control (`Admin`, `Doctor`, `Patient`) |
| **Design Aesthetics** | Modern Glassmorphism, HSL color tokens, custom SVG icons, responsive flexbox/grid |

---

## Seeded Demo Accounts

Demo accounts are disabled by default. For a disposable local environment, set `SEED_DEMO_DATA=true` and provide `DEMO_SEED_PASSWORD` in `.env`. They are never seeded outside Development.

| Role | Email | Access Rights |
| :--- | :--- | :--- |
| **Administrator** | `admin@carepoint.com` | Full System & Doctor Approvals |
| **Doctor** | `dr.smith@carepoint.com` | Practitioner Schedule & Profile |
| **Patient** | `patient@carepoint.com` | Find Doctors & Book Appointments |

---

## Getting Started & Local Setup

### Prerequisites
- **.NET 10.0 SDK**
- **Node.js** (v18+) & **npm**
- Docker Desktop (for SQL Server)

### Configure local secrets

Copy the example configuration and replace both placeholders with strong, local-only values. The `.env` file is ignored by Git.

```bash
cp .env.example .env
```

Password resets use SMTP when `SMTP_HOST` and `EMAIL_FROM_ADDRESS` are configured. In Development, if SMTP is intentionally omitted, the reset URL is written to the API log so the flow remains testable locally.

### Run the complete development stack

```bash
./run.sh
```

This starts SQL Server, the API on `http://127.0.0.1:5005`, and the frontend on `http://localhost:5173`.

### 1. Run Backend Web API
```bash
cp .env.example .env
source .env
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=CarePointDb;User Id=sa;Password=$MSSQL_SA_PASSWORD;TrustServerCertificate=True;"
export JwtSettings__Secret="$JWT_SECRET"
cd backend/CarePoint.API
dotnet run --launch-profile http
```
The API server launches at `http://127.0.0.1:5005`.

### 2. Run Frontend Web Application
```bash
cd frontend
npm install
npm run dev
```
Open your browser at `http://localhost:5173`.

### Docker

After configuring `.env`, start all three containers with:

```bash
docker compose up --build
```

---

## Project Architecture

```text
ProjecCV/
├── backend/
│   ├── CarePoint.API/            # Controllers, Middlewares, Routing
│   ├── CarePoint.Application/    # DTOs, Service Interfaces, Logic
│   ├── CarePoint.Infrastructure/ # EF Core, Data Seeder, Services
│   └── CarePoint.Domain/         # Domain Entities, Enums, Exceptions
└── frontend/
    └── src/
        ├── components/           # Common UI Elements & SVG Icons
        ├── layouts/              # Patient, Doctor, Admin Layout Shells
        ├── pages/                # Role Dashboards, Search, Profile Pages
        ├── store/                # Redux Slices & Auth State
        └── types/                # TypeScript Interfaces & DTOs
```

---

## License
This project is proprietary and maintained under the CarePoint medical ecosystem.
