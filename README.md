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

UserService manages the following tables:

### Core User Tables

| Table | Description |
|--------|-------------|
| `users` | Core user table for all types (business, end-user, support) |
| `business_reps` | Links a business (from BusinessService) to a user |
| `end_user_profiles` | Extended details for end users |
| `support_user_profiles` | Extended details for internal support staff |

### US-001: Badge System Tables

| Table | Description |
|--------|-------------|
| `badge_definitions` | Badge types with criteria (Pioneer, Expert, Pro, etc.) |
| `user_badges` | Badges earned by users |
| `user_badge_levels` | User progression through badge tiers |

### US-002: Points System Tables

| Table | Description |
|--------|-------------|
| `point_rules` | Configurable point values for actions |
| `user_points` | User point balances and tiers |
| `point_transactions` | Point earning/redemption history |
| `point_multipliers` | Time-limited point boost events |
| `user_daily_points` | Daily occurrence tracking for rate limits |

### US-003: Verification System Tables

| Table | Description |
|--------|-------------|
| `email_verifications` | Email verification codes/tokens |
| `phone_verifications` | Phone OTP verifications |
| `user_verification_status` | Current verification level per user |

### US-004: Referral System Tables

| Table | Description |
|--------|-------------|
| `user_referral_codes` | Unique referral codes per user |
| `referrals` | Referral relationships and status |
| `referral_reward_tiers` | Bonus reward tiers for high referrers |
| `referral_campaigns` | Time-limited referral promotions |

### US-005: Location Tracking Tables

| Table | Description |
|--------|-------------|
| `user_locations` | GPS location history |
| `user_saved_locations` | Saved places (Home, Work, etc.) |
| `user_location_preferences` | Privacy and tracking settings |
| `geofences` | Geographic boundaries for events |
| `user_geofence_events` | Enter/exit/dwell events |

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

## 🧱 API Endpoints

### Authentication (`/api/auth`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/auth/login` | Login with email/password | Public |
| `POST` | `/api/auth/refresh` | Refresh access token | Public |
| `POST` | `/api/auth/logout` | Logout (clear refresh token) | Public |
| `GET` | `/api/auth/social/providers` | Get available social login providers | Public |
| `POST` | `/api/auth/social/authorize` | Get authorization URL for social login | Public |
| `POST` | `/api/auth/social/callback` | Complete social login with auth code | Public |
| `POST` | `/api/auth/social/link` | Link social account to existing user | 🔒 |
| `DELETE` | `/api/auth/social/link/{provider}` | Unlink social account | 🔒 |
| `GET` | `/api/auth/social/linked` | Get all linked social accounts | 🔒 |

### User Management (`/api/user`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/user/business` | Register new business account | Public |
| `GET` | `/api/user/business/{id}` | Get business user by ID | 🔒 Business/Support |
| `POST` | `/api/user/sub-business` | Create sub-business user | 🔒 Business |
| `PUT` | `/api/user/sub-business/{userId}` | Update sub-business user | 🔒 Business |
| `POST` | `/api/user/support` | Create support user | 🔒 Support |
| `POST` | `/api/user/end-user` | Create end user | Public |
| `GET` | `/api/user/end-user/{userId}/profile` | Get end user profile | Public |
| `PUT` | `/api/user/end-user/{userId}/profile` | Update end user profile | Public |
| `GET` | `/api/user/business-rep/{businessRepId}` | Get business rep details | Public |
| `GET` | `/api/user/business-rep/parent/{businessId}` | Get parent rep by business ID | Public |
| `GET` | `/api/user/support-user/{userId}/exists` | Check if user is support user | Public |

---

### US-001: Consumer Badge System (`/api/badge`)

Gamification system rewarding consumers with visual badges based on activity and tenure.

**Badge Types:** Pioneer 🏅, Top Contributor 🏆, Expert ⭐, Most Helpful 👍, Newbie 🌱, Pro 💎

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/badge/definitions` | Get all active badge definitions | Public |
| `GET` | `/api/badge/definitions/{id}` | Get badge by ID | Public |
| `GET` | `/api/badge/definitions/category/{category}` | Get badges by category | Public |
| `GET` | `/api/badge/user/{userId}` | Get user's earned badges | Public |
| `GET` | `/api/badge/user/{userId}/level` | Get user's badge level and progress | Public |
| `GET` | `/api/badge/user/{userId}/summary` | Get user's badge summary (level, badges, available) | Public |
| `GET` | `/api/badge/user/{userId}/has/{badgeName}` | Check if user has specific badge | Public |
| `POST` | `/api/badge/award` | Award a badge to user | 🔒 Support |
| `POST` | `/api/badge/user/{userId}/check-eligible` | Check and award eligible badges | Public |
| `POST` | `/api/badge/definitions` | Create new badge definition | 🔒 Support |
| `PUT` | `/api/badge/definitions/{id}` | Update badge definition | 🔒 Support |
| `POST` | `/api/badge/definitions/{id}/activate` | Activate a badge | 🔒 Support |
| `POST` | `/api/badge/definitions/{id}/deactivate` | Deactivate a badge | 🔒 Support |

---

### US-002: Consumer Points System (`/api/points`)

Reward system tracking engagement through points for meaningful actions.

**Point Tiers:** Bronze (0-999) → Silver (1,000-4,999) → Gold (5,000-9,999) → Platinum (10,000+)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/points/user/{userId}` | Get user's points balance | Public |
| `GET` | `/api/points/user/{userId}/summary` | Get points summary with transactions | Public |
| `GET` | `/api/points/user/{userId}/transactions` | Get transaction history (limit, offset) | Public |
| `GET` | `/api/points/user/{userId}/transactions/type/{type}` | Get transactions by type | Public |
| `GET` | `/api/points/user/{userId}/transactions/range` | Get transactions by date range | Public |
| `GET` | `/api/points/user/{userId}/rank` | Get user's rank on leaderboard | Public |
| `POST` | `/api/points/earn` | Earn points for an action | Public |
| `POST` | `/api/points/redeem` | Redeem points | Public |
| `POST` | `/api/points/adjust` | Admin adjustment of points | 🔒 Support |
| `GET` | `/api/points/rules` | Get all active point rules | Public |
| `GET` | `/api/points/rules/{actionType}` | Get rule by action type | Public |
| `POST` | `/api/points/rules` | Create a point rule | 🔒 Support |
| `PUT` | `/api/points/rules/{id}` | Update a point rule | 🔒 Support |
| `GET` | `/api/points/multipliers` | Get active point multipliers | Public |
| `POST` | `/api/points/multipliers` | Create a multiplier | 🔒 Support |
| `GET` | `/api/points/leaderboard` | Get points leaderboard | Public |

---

### US-003: User Verification System (`/api/verification`)

Trust-building system allowing users to verify identity via phone (OTP) and email.

**Verification Levels:** Unverified → Partial (phone OR email) → Fully Verified (BOTH)

**Benefits:** Verified users earn 50% more points

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/verification/user/{userId}/status` | Get user's verification status | Public |
| `POST` | `/api/verification/email/send` | Send email verification code | Public |
| `POST` | `/api/verification/email/verify` | Verify email with code | Public |
| `GET` | `/api/verification/email/verify/{token}` | Verify email via token link (24hr expiry) | Public |
| `POST` | `/api/verification/email/resend/{userId}` | Resend email verification | Public |
| `GET` | `/api/verification/email/active/{userId}` | Get active email verification | Public |
| `POST` | `/api/verification/phone/send` | Send phone verification code (OTP) | Public |
| `POST` | `/api/verification/phone/verify` | Verify phone with code (10min expiry, 3 attempts) | Public |
| `POST` | `/api/verification/phone/resend/{userId}` | Resend phone verification | Public |
| `GET` | `/api/verification/phone/active/{userId}` | Get active phone verification | Public |

---

### US-004: Referral System (`/api/referral`)

Growth mechanism rewarding users for inviting friends.

**Flow:** Referrer gets code → Friend signs up → Friend writes 3 approved reviews → Referrer earns 50-75 points

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/referral/user/{userId}/code` | Get or create user's referral code | Public |
| `GET` | `/api/referral/validate/{code}` | Validate a referral code | Public |
| `GET` | `/api/referral/code/{code}` | Look up referral code details | Public |
| `PUT` | `/api/referral/user/{userId}/code/custom` | Set custom referral code | Public |
| `POST` | `/api/referral/use` | Use referral code when signing up | Public |
| `GET` | `/api/referral/user/{userId}/referrals` | Get user's referrals | Public |
| `GET` | `/api/referral/user/{userId}/summary` | Get referral summary | Public |
| `GET` | `/api/referral/user/{userId}/referred-by` | Check who referred a user | Public |
| `POST` | `/api/referral/{referralId}/complete` | Complete a referral (after 3rd approved review) | Public |
| `GET` | `/api/referral/leaderboard` | Get referral leaderboard | Public |
| `GET` | `/api/referral/tiers` | Get reward tiers | Public |
| `GET` | `/api/referral/user/{userId}/tier` | Get user's current reward tier | Public |
| `GET` | `/api/referral/campaign/active` | Get active referral campaign | Public |
| `GET` | `/api/referral/campaigns` | Get all campaigns | Public |
| `POST` | `/api/referral/campaigns` | Create referral campaign | 🔒 Support |
| `POST` | `/api/referral/invite` | Send referral invite | Public |

---

### US-005: GPS Geolocation Tracking (`/api/location`)

Location awareness system capturing user geographic location for badges, leaderboards, and review validation.

**Features:** Location badges, Review validation, "Near Me" discovery, VPN detection

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/location/record` | Record user's current location | Public |
| `GET` | `/api/location/user/{userId}/latest` | Get user's latest location | Public |
| `GET` | `/api/location/user/{userId}/history` | Get location history (limit, offset) | Public |
| `GET` | `/api/location/user/{userId}/history/range` | Get locations by date range | Public |
| `DELETE` | `/api/location/{locationId}` | Delete a specific location | Public |
| `DELETE` | `/api/location/user/{userId}/history` | Delete all location history | Public |
| `GET` | `/api/location/user/{userId}/saved` | Get user's saved locations | Public |
| `GET` | `/api/location/saved/{locationId}` | Get saved location by ID | Public |
| `GET` | `/api/location/user/{userId}/saved/default` | Get user's default location | Public |
| `POST` | `/api/location/saved` | Create a saved location | Public |
| `PUT` | `/api/location/saved/{locationId}` | Update a saved location | Public |
| `POST` | `/api/location/user/{userId}/saved/{locationId}/set-default` | Set default location | Public |
| `DELETE` | `/api/location/saved/{locationId}` | Delete a saved location | Public |
| `GET` | `/api/location/user/{userId}/preferences` | Get location preferences | Public |
| `PUT` | `/api/location/user/{userId}/preferences` | Update location preferences | Public |
| `GET` | `/api/location/geofences` | Get active geofences | Public |
| `GET` | `/api/location/geofences/{geofenceId}` | Get geofence by ID | Public |
| `POST` | `/api/location/geofences` | Create a geofence | 🔒 Support |
| `PUT` | `/api/location/geofences/{geofenceId}` | Update a geofence | 🔒 Support |
| `DELETE` | `/api/location/geofences/{geofenceId}` | Delete a geofence | 🔒 Support |
| `POST` | `/api/location/geofences/check` | Check if user is inside any geofences | Public |
| `GET` | `/api/location/user/{userId}/geofence-events` | Get user's geofence events | Public |
| `POST` | `/api/location/nearby` | Find nearby users | Public |
| `POST` | `/api/location/user/{userId}/cleanup` | Cleanup old locations | Public |

---

## 👨‍💻 Contributors

| Role | Name |
|------|------|
| Architect / Lead | Dily & Chinedu |
| Developer(s) | — |
| Reviewer(s) | — |

---
<img width="1536" height="1024" alt="ChatGPT Image Oct 12, 2025, 04_28_39 PM" src="https://github.com/user-attachments/assets/a82846f4-324e-4290-b017-66732a90c957" />

## 🏁 License

This project is licensed under the **MIT License** — see the `LICENSE` file for details.
