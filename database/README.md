# Database Migrations

This folder contains PostgreSQL database migration scripts for the User Service.

## Migration Files

| Version | File | Description |
|---------|------|-------------|
| 2.0.0 | `V2.0.0__add_user_service_features.sql` | Adds tables for Badge, Points, Verification, Referral, and Geolocation features |
| 2.0.0 | `V2.0.0__add_user_service_features_rollback.sql` | Rollback script for V2.0.0 |

## New Tables Created (V2.0.0)

### Badge System (US-001)
- `user_badges` - Stores badges earned by consumers

### Points System (US-002)
- `user_points` - User point balances and streak information
- `point_transactions` - Transaction history for point earnings

### Verification System (US-003)
- `user_verifications` - Phone and email verification status
- `verification_tokens` - OTP codes and email verification tokens

### Referral System (US-004)
- `user_referral_codes` - Unique referral codes for users
- `referrals` - Referral relationships and lifecycle tracking

### Geolocation System (US-005)
- `user_geolocations` - Current location data for users
- `geolocation_history` - Historical location data for validation

## Running Migrations

### Using psql directly

```bash
# Connect to your database and run the migration
psql -h <host> -U <username> -d <database> -f database/migrations/V2.0.0__add_user_service_features.sql
```

### Using Docker

```bash
# If using docker-compose
docker exec -i <postgres_container> psql -U <username> -d <database> < database/migrations/V2.0.0__add_user_service_features.sql
```

## Rollback

To rollback the V2.0.0 migration:

```bash
psql -h <host> -U <username> -d <database> -f database/migrations/V2.0.0__add_user_service_features_rollback.sql
```

**WARNING**: Rollback will delete all data in the affected tables!

## Prerequisites

These migrations assume the `users` table already exists with an `id` column of type `UUID`.
