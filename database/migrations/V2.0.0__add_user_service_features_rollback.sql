-- ============================================================================
-- User Service Database Schema Rollback
-- Version: 2.0.0
-- Description: Drops tables for Badge, Points, Verification, Referral, and
--              Geolocation features
-- WARNING: This will delete all data in these tables!
-- ============================================================================

-- Drop tables in reverse order of creation (respecting foreign key dependencies)

-- Drop Geolocation tables
DROP TABLE IF EXISTS geolocation_history CASCADE;
DROP TABLE IF EXISTS user_geolocations CASCADE;

-- Drop Referral tables
DROP TABLE IF EXISTS referrals CASCADE;
DROP TABLE IF EXISTS user_referral_codes CASCADE;

-- Drop Verification tables
DROP TABLE IF EXISTS verification_tokens CASCADE;
DROP TABLE IF EXISTS user_verifications CASCADE;

-- Drop Points tables
DROP TABLE IF EXISTS point_transactions CASCADE;
DROP TABLE IF EXISTS user_points CASCADE;

-- Drop Badge tables
DROP TABLE IF EXISTS user_badges CASCADE;

-- ============================================================================
-- END OF ROLLBACK
-- ============================================================================
