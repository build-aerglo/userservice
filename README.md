# 🧩 UserService — Microservice for User Management

A **.NET 9 Domain-Driven Design (DDD)** based microservice responsible for managing users, businesses, business reps, and support users in the **Aerglo Review Platform** (similar to TrustPilot/Yelp).

This service provides secure, token-based CRUD APIs via **Auth0**, connects to a **PostgreSQL** database using **Dapper**, and manages schema migrations using **DbUp**.

---

## 🏗️ Architecture Overview

The service follows **Domain-Driven Design (DDD)** principles and **Clean Architecture** boundaries:

```
+-----------------------------------------------------------+
|                    UserService.Api                        |
|  → Authentication (Auth0, JWT)                            |
|  → Controllers / Endpoints                                |
|  → Swagger / OpenAPI Docs                                 |
+-----------------------------------------------------------+
|                UserService.Application                    |
|  → Business Logic (Use Cases)                             |
|  → DTOs / Validation                                      |
|  → Service Interfaces (IUserService)                      |
+-----------------------------------------------------------+
|                   UserService.Domain                      |
|  → Core Entities (User, Business, BusinessRep, Review)    |
|  → Value Objects / Enums                                  |
|  → Repository Interfaces                                  |
+-----------------------------------------------------------+
|                UserService.Infrastructure                 |
|  → PostgreSQL Repositories (Dapper)                       |
|  → SQL Queries                                            |
|  → Database Access via Npgsql                             |
+-----------------------------------------------------------+
|                   tools/UserService.DbUp                  |
|  → DbUp migrations (SQL scripts)                          |
|  → Schema versioning and upgrades                         |
+-----------------------------------------------------------+
```

---

## 📁 Folder Structure

```
UserService/
├── src/
│   ├── UserService.Api/              # API endpoints, authentication, Swagger
│   ├── UserService.Application/      # Application logic, DTOs, services
│   ├── UserService.Domain/           # Domain models, interfaces, value objects
│   ├── UserService.Infrastructure/   # Dapper/Postgres repositories
│   └── tools/
│       └── UserService.DbUp/         # Database migrations (DbUp console app)
│
├── tests/
│   ├── UserService.Api.Tests/        # Controller tests (NUnit + Moq)
│   ├── UserService.Application.Tests # Service-level tests
│   └── UserService.Infrastructure.Tests # Repository integration tests
│
└── README.md
```

---

## ⚙️ Tech Stack

| Layer | Technology |
|-------|-------------|
| API | ASP.NET Core 9 (Minimal API + Controllers) |
| Authentication | Auth0 (JWT Bearer Tokens) |
| Database | PostgreSQL |
| ORM | Dapper |
| Migration | DbUp |
| Testing | NUnit + Moq |
| Build | .NET SDK 9 |
| Packaging | Docker (optional) |

---

## 🚀 Getting Started

### 1️⃣ Prerequisites

| Tool | Version |
|------|----------|
| .NET SDK | 9.0+ |
| PostgreSQL | 15+ |
| Auth0 Account | Required |
| Git | Latest |
| Rider / VS Code / Visual Studio | Any modern IDE |

---

### 2️⃣ Clone the Repository

```bash
git clone https://github.com/build-aerglo/userservice.git
cd userservice
```

---

### 3️⃣ Set Up Environment Variables

Create an `appsettings.Development.json` file in `src/UserService.Api/`:

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Port=5432;Database=user_service;Username=postgres;Password=postgres;Include Error Detail=true;"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://api.aerglo-userservice"
  }
}
```

---

### 4️⃣ Apply Database Migrations (DbUp)

From the project root, run:

```bash
dotnet run --project src/tools/UserService.DbUp
```

✅ This creates the schema and tables defined in `/tools/UserService.DbUp/Scripts/`.

Tables include:
- `users`
- `businesses`
- `business_reps`
- `business_media`
- `business_social_media`
- `support_users`
- `reviews`

---

### 5️⃣ Run the API

```bash
dotnet run --project src/UserService.Api
```

API will start at:
```
https://localhost:5001
```

Swagger UI:
```
https://localhost:5001/swagger
```

---

### 6️⃣ Authenticate via Auth0

You’ll need an Auth0 **access token** to call protected routes.

**Public route:**
```
GET /
→ "Welcome to the public API!"
```

**Protected route:**
```
GET /secure
→ Requires Bearer token in Authorization header
```

**Example header:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🧩 Example Endpoints

| Method | Route | Description | Auth |
|--------|--------|--------------|------|
| `GET` | `/api/user` | List all users | 🔒 |
| `GET` | `/api/user/{id}` | Get user by ID | 🔒 |
| `POST` | `/api/user` | Create a new user | 🔒 |
| `PUT` | `/api/user/{id}` | Update a user | 🔒 |
| `DELETE` | `/api/user/{id}` | Delete a user | 🔒 |
| `GET` | `/` | Welcome route | Public |

---

## 🧱 Database Schema (Simplified)

```sql
users
 ├── id (UUID, PK)
 ├── username
 ├── email
 ├── phone
 ├── user_type ('end_user', 'business_user', 'support_user')
 ├── address
 ├── join_date
 ├── created_at / updated_at

businesses
 ├── id (UUID, PK)
 ├── business_name
 ├── business_email
 ├── sector
 ├── verified
 ├── ...etc

reviews
 ├── id (UUID, PK)
 ├── business_id (FK)
 ├── user_id (FK)
 ├── rating (1–5)
 ├── comment
```

---

## 🧪 Running Tests

To execute all test projects:

```bash
dotnet test
```

Tests use **NUnit + Moq** and include:
- API endpoint tests (controllers)
- Application logic tests (service layer)
- Integration tests (repository → Postgres)

---

## 🧰 Local Development with Docker

You can spin up a local Postgres instance with Docker:

```bash
docker run --name userdb -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres
```

Then update your `appsettings.Development.json` to point to `localhost:5432`.

---

## 🔒 Authentication Flow

1. Client authenticates with **Auth0** and receives an **Access Token**.
2. API validates token via `JwtBearer` middleware.
3. Protected routes (like `/api/user`) require a valid token.
4. Authorization attributes ensure scoped access.

---

## 🧩 Application Flow

```text
Client → API Controller → Application Service → Domain Entity → Repository (Dapper) → PostgreSQL
```

- **API**: Handles HTTP requests and Auth0 validation.  
- **Application Layer**: Contains business logic and DTO mappings.  
- **Domain Layer**: Core entities and contracts (no dependencies).  
- **Infrastructure Layer**: Handles persistence with Dapper.  
- **DbUp Tool**: Handles database schema creation and versioning.

---

## 🧠 Design Highlights

✅ Clean separation of concerns  
✅ Testable architecture (unit and integration tests)  
✅ Explicit domain model with value objects  
✅ Lightweight data access (Dapper)  
✅ Secure JWT authentication (Auth0)  
✅ Versioned migrations with DbUp  
✅ Cross-platform development via .NET 9  

---

## 🧱 Future Enhancements

- ✅ Add Review,Company,Notification etc Service microservice 
- 📦 Containerize and deploy via Docker Compose / Kubernetes
- 📊 Add health checks and distributed tracing

---

## 👨‍💻 Contributors

| Role | Name |
|------|------|
| Architect / Lead | Dili & Chinedu |
| Developer(s) | — |
| Reviewer(s) | — |

---

## 🏁 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
