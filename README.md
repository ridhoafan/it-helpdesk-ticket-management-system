
# IT Helpdesk & Ticket Management System

A web-based IT Helpdesk and Ticket Management System built with ASP.NET Core MVC and C# to manage IT support requests, automate technician assignments, and monitor ticket resolution workflows.

## Overview

This project provides a centralized platform for employees to report IT-related problems and for IT Support teams to manage, track, and resolve support requests.

The system uses role-based access control to separate responsibilities between Administrators, IT Support personnel, and Employees.

## Key Features

### 1. Authentication & User Management
- User registration and login.
- Account approval by Administrator.
- Role-based access control.
- Supported roles: Admin, IT Support, and Employee.

### 2. Ticket Management
- Create, view, update, and soft-delete support tickets.
- Categorize tickets by Hardware, Software, Network, Account & Access, and Other.
- Set ticket priorities: Low, Medium, High, and Critical.
- Track ticket status and resolution history.

### 3. Automatic Technician Assignment
- Assign tickets based on IT Support specialization.
- Allow each IT Support user to handle multiple categories.
- Automatically select the technician with the lowest active ticket workload.
- Leave tickets unassigned when no eligible technician is available.

### 4. Ticket Workflow

Tickets follow a controlled status lifecycle:

`Open → In Progress → Resolved → Closed`

- IT Support and Administrators can update ticket status.
- Status changes are recorded with timestamps and user information.
- Resolution summaries are automatically saved as public comments.
- Employees can monitor ticket progress and assigned technicians.

### 5. Comments & Internal Notes
- Employees and IT Support can communicate through public comments.
- Admin and IT Support can create internal notes.
- Internal notes are hidden from Employees.
- Resolution summaries are visible to Employees and Administrators.

### 6. Dashboard & Audit Logs
- Role-specific ticket statistics.
- Admin dashboard for overall ticket monitoring.
- IT Support dashboard for assigned tickets.
- Employee dashboard for personally submitted tickets.
- Audit logs for important system activities.

## Technology Stack

| Category | Technology |
|---|---|
| Backend | C# |
| Framework | ASP.NET Core MVC / REST API |
| Runtime | .NET 10 |
| ORM | Entity Framework Core 10 |
| Database | Microsoft SQL Server |
| Development Database | SQL Server LocalDB |
| Frontend | Razor Views, HTML, CSS, JavaScript |
| UI Framework | Bootstrap 5 |
| API Testing | Postman |
| IDE | Visual Studio Code |
| Version Control | Git & GitHub |

## Project Structure

```text
ITHelpdesk/
│
├── ITHelpdesk.slnx
│
├── ITHelpdesk.Web/
│   ├── Controllers/
│   │   ├── Api/
│   │   ├── AccountController.cs
│   │   ├── TicketsController.cs
│   │   ├── UserApprovalsController.cs
│   │   └── ...
│   │
│   ├── Data/
│   │   ├── Migrations/
│   │   ├── AppDbContext.cs
│   │   └── DevelopmentDataSeeder.cs
│   │
│   ├── DTOs/
│   ├── Models/
│   ├── Services/
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   └── ITHelpdesk.Web.csproj
│
└── .gitignore
```

## User Roles

| Role | Responsibilities |
|---|---|
| Admin | Approve accounts, manage users, monitor all tickets, view audit logs, and supervise the system. |
| IT Support | Handle assigned tickets, update ticket status, communicate with employees, and manage internal notes. |
| Employee | Submit support requests, monitor ticket progress, view assigned technicians, and communicate with IT Support. |

## REST API

The application also provides REST API endpoints for ticket management.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/tickets` | Retrieve accessible tickets |
| GET | `/api/tickets/{id}` | Retrieve ticket details |
| POST | `/api/tickets` | Create a ticket |
| PUT | `/api/tickets/{id}` | Update ticket information |
| PATCH | `/api/tickets/{id}/status` | Update ticket status |
| DELETE | `/api/tickets/{id}` | Soft-delete a ticket |

API access is protected by authentication and role-based authorization.

## Getting Started

### Prerequisites

- Windows 10 or Windows 11.
- .NET 10 SDK.
- SQL Server LocalDB.
- Entity Framework Core CLI.
- Visual Studio Code.

### Installation

1. Clone the repository:

```bash
git clone <YOUR_REPOSITORY_URL>
```

2. Navigate to the project directory:

```bash
cd ITHelpdesk
```

3. Restore dependencies:

```bash
dotnet restore
```

4. Start SQL Server LocalDB:

```powershell
sqllocaldb start MSSQLLocalDB
```

5. Navigate to the web project:

```bash
cd ITHelpdesk.Web
```

6. Configure the development database connection string and required development seed credentials using local configuration or ASP.NET Core User Secrets.

7. Apply Entity Framework Core migrations:

```bash
dotnet ef database update
```

8. Run the application:

```bash
dotnet run
```

9. Open the local URL displayed in the terminal.

## Development Status

The project currently includes:

- Authentication and account approval.
- Role-based authorization.
- Ticket management.
- Automatic technician assignment.
- Ticket status workflow.
- Resolution summaries.
- Public comments and internal notes.
- Role-specific dashboards.
- Audit logging.
- REST API endpoints.

In-app notifications and automated notification delivery are planned for future development.

## Author

**Ridho Afan**

Computer Engineering Graduate  
Universitas Andalas

Areas of Interest:
- Software Development
- Web Development
- Backend Engineering
- Artificial Intelligence & Machine Learning

---

*This project is developed as a software engineering portfolio project.*
