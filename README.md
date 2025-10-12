# 🧩 UserService — User Management Microservice

A **.NET 9**, **DDD-based microservice** that manages all **user-related data** in the **Aerglo Review Platform**, including:

- End users (consumers who leave reviews)  
- Business representatives (linked to businesses from another service)  
- Support users (admins and internal staff)  

This service connects to **PostgreSQL** via **Dapper**, uses **Auth0** for authentication, and integrates with a separate **BusinessService** for business registration.  

---

## 🧱 Architecture Overview

The service follows **Domain-Driven Design (DDD)** and **Clean Architecture** layering:

```
+-----------------------------------------------------------+
|                     UserService.Api                       |
|  → Auth0 JWT authentication                               |
|  → Controllers / Swagger UI                               |
+-----------------------------------------------------------+
|                UserService.Application                    |
|  → Business logic (user registration, business rep linking)|
|  → DTOs / Use cases                                       |
+-----------------------------------------------------------+
|                   UserService.Domain                      |
|  → Entities: User, BusinessRep, EndUserProfile, SupportUserProfile |
|  → Repository interfaces (contracts)                      |
+-----------------------------------------------------------+
|                UserService.Infrastructure                 |
|  → Dapper-based repositories (PostgreSQL)                 |
|  → Database access via Npgsql                             |
+-----------------------------------------------------------+
|                   tools/UserService.DbUp                  |
|  → SQL migrations for schema creation                     |
+-----------------------------------------------------------+
```

---

## 📁 Project Structure

```
UserService/
├── src/
│   ├── UserService.Api/               # API & Auth0 config
│   ├── UserService.Application/       # Business logic and service layer
│   ├── UserService.Domain/            # Entities & repository interfaces
│   ├── UserService.Infrastructure/    # Dapper repositories
│   └── tools/UserService.DbUp/        # Schema migrations via DbUp
│
├── tests/
│   ├── UserService.Api.Tests/
│   ├── UserService.Application.Tests/
│   └── UserService.Infrastructure.Tests/
│
└── README.md
```

---

## ⚙️ Technology Stack

| Layer | Technology |
|--------|-------------|
| API | ASP.NET Core 9 |
| Auth | Auth0 (JWT Bearer) |
| ORM | Dapper |
| Database | PostgreSQL |
| Migrations | DbUp |
| Testing | NUnit + Moq |
| Containerization | Docker (optional) |

---

## 🧩 Database Schema

UserService manages only **4 tables**.

| Table | Description |
|--------|-------------|
| `users` | Core user table for all types (business, end-user, support) |
| `business_reps` | Links a business (from BusinessService) to a user |
| `end_user_profiles` | Extended details for end users |
| `support_user_profiles` | Extended details for internal support staff |

### SQL Definition

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    phone TEXT,
    user_type TEXT NOT NULL CHECK (user_type IN ('end_user', 'business_user', 'support_user')),
    address TEXT,
    join_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE business_reps (
    id UUID PRIMARY KEY,
    business_id UUID NOT NULL,
    user_id UUID NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_business_rep_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE end_user_profiles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    preferences JSONB,
    bio TEXT,
    social_links JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_end_user_profile_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE support_user_profiles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    department TEXT,
    role TEXT,
    permissions JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_support_user_profile_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
```

---

## 🧭 ERD (Entity Relationship Diagram)

📊 [Download ERD Diagram](UserService_ERD.png)

Each child table links to `users.id` via a foreign key with cascade delete.

---

## 🚀 Business Registration Flow

When a **new business account** registers through UserService:

1. **UserService** receives the user payload.
2. It calls the **BusinessService API** (`POST /api/businesses`) with that payload.
3. **BusinessService** creates the business and returns a `businessId`.
4. UserService:
   - Creates a record in `users` (type = `business_user`)
   - Creates a record in `business_reps` linking the new user to that businessId.

### Example (C#)
```csharp
public async Task<(User user, Guid businessId)> RegisterBusinessAccountAsync(User userPayload)
{
    var response = await _httpClient.PostAsJsonAsync("https://business-service/api/businesses", userPayload);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<BusinessCreatedResponse>();

    await _userRepository.AddAsync(userPayload);

    var businessRep = new BusinessRep(result.BusinessId, userPayload.Id);
    await _businessRepRepository.AddAsync(businessRep);

    return (userPayload, result.BusinessId);
}
```

---

## 🔒 Authentication

Uses **Auth0 JWT Bearer** authentication.  
All `/api/user/*` endpoints are protected.

Example protected route header:ƒ√
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

Example curl:- curl --request POST \
  --url https://dev-jx8cz5q0wcoddune.us.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id":"HYUCewQj7h3Ybt8ttM0jvL5tQApkKEMg",
    "client_secret":"jmwxviI8ZCSqSqD-2ZjYQxZXXn1PHVhzvTMJR4C5W6BHpHflcrlkqdt16VIOEoJv",
    "audience":"https://user-service.aerglotechnology.com",
    "grant_type":"client_credentials"
  }'


## 🧰 Setup Instructions

### 1️⃣ Install prerequisites
- .NET 9 SDK  
- PostgreSQL  
- Auth0 account

### 2️⃣ Clone and configure
```bash
git clone https://github.com/build-aerglo/userservice.git
cd userservice
```

### 3️⃣ Configure appsettings
```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Port=5432;Database=user_service;Username=postgres;Password=postgres"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://api.aerglo-userservice"
  }
}
```

### 4️⃣ Run migrations
```bash
dotnet run --project src/tools/UserService.DbUp
```

### 5️⃣ Run the API
```bash
dotnet run --project src/UserService.Api
```

Swagger UI:  
👉 `https://localhost:5001/swagger`

---

## 🧪 Testing

All test projects use **NUnit + Moq**:
```bash
dotnet test
```

---

## 🧱 Example Endpoints

| Method | Route | Description | Auth |
|--------|--------|--------------|------|
| `GET` | `/api/user` | Get all users | 🔒 |
| `GET` | `/api/user/{id}` | Get user by ID | 🔒 |
| `POST` | `/api/user` | Create user | 🔒 |
| `POST` | `/api/user/register-business` | Register new business account | 🔒 |
| `PUT` | `/api/user/{id}` | Update user | 🔒 |
| `DELETE` | `/api/user/{id}` | Delete user | 🔒 |
| `GET` | `/` | Public welcome route | Public |

---

## 👨‍💻 Contributors

| Role | Name |
|------|------|
| Architect / Lead | Dily & Chinedu |
| Developer(s) | — |
| Reviewer(s) | — |

---

## 🏁 License

This project is licensed under the **MIT License** — see the `LICENSE` file for details.
