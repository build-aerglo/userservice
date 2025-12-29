-- ============================================================================
-- User Service Test Seed Data
-- Description: Populates test data for development and testing
-- WARNING: Only run this in development/test environments!
-- ============================================================================

-- Note: This script assumes you have test users already created in the users table
-- Replace the UUIDs below with actual user IDs from your test environment

-- ============================================================================
-- SAMPLE BADGE DATA
-- ============================================================================
-- Example: Assign badges to a test user (replace with actual user ID)
-- INSERT INTO user_badges (user_id, badge_type, location, category, earned_at, is_active)
-- VALUES
--     ('00000000-0000-0000-0000-000000000001', 'newbie', NULL, NULL, NOW(), TRUE),
--     ('00000000-0000-0000-0000-000000000001', 'pioneer', NULL, NULL, NOW(), TRUE);

-- ============================================================================
-- SAMPLE POINTS DATA
-- ============================================================================
-- Example: Initialize points for test users
-- INSERT INTO user_points (user_id, total_points, current_streak, longest_streak)
-- VALUES
--     ('00000000-0000-0000-0000-000000000001', 150.5, 5, 10),
--     ('00000000-0000-0000-0000-000000000002', 500.0, 15, 25);

-- ============================================================================
-- SAMPLE VERIFICATION DATA
-- ============================================================================
-- Example: Create verification records for test users
-- INSERT INTO user_verifications (user_id, phone_verified, email_verified, phone_verified_at, email_verified_at)
-- VALUES
--     ('00000000-0000-0000-0000-000000000001', TRUE, TRUE, NOW(), NOW()),
--     ('00000000-0000-0000-0000-000000000002', TRUE, FALSE, NOW(), NULL);

-- ============================================================================
-- SAMPLE REFERRAL DATA
-- ============================================================================
-- Example: Create referral codes for test users
-- INSERT INTO user_referral_codes (user_id, code, total_referrals, successful_referrals, is_active)
-- VALUES
--     ('00000000-0000-0000-0000-000000000001', 'TESTUSER2025', 5, 3, TRUE);

-- Example: Create referral relationships
-- INSERT INTO referrals (referrer_id, referred_user_id, referral_code, status, approved_review_count, points_awarded)
-- VALUES
--     ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'TESTUSER2025', 'completed', 3, TRUE);

-- ============================================================================
-- SAMPLE GEOLOCATION DATA
-- ============================================================================
-- Example: Create geolocation records for test users (Lagos coordinates)
-- INSERT INTO user_geolocations (user_id, latitude, longitude, state, lga, city, is_enabled)
-- VALUES
--     ('00000000-0000-0000-0000-000000000001', 6.5244, 3.3792, 'Lagos', 'Ikeja', 'Lagos City', TRUE),
--     ('00000000-0000-0000-0000-000000000002', 9.0579, 7.4951, 'Abuja FCT', 'Abuja Municipal', 'Abuja', TRUE);

-- ============================================================================
-- HELPER: Create test data for a specific user
-- ============================================================================
-- This function can be used to initialize all tables for a new user
-- USAGE: SELECT initialize_user_features('<user_id>');

CREATE OR REPLACE FUNCTION initialize_user_features(p_user_id UUID)
RETURNS VOID AS $$
BEGIN
    -- Create user points record
    INSERT INTO user_points (user_id, total_points, current_streak, longest_streak)
    VALUES (p_user_id, 0, 0, 0)
    ON CONFLICT (user_id) DO NOTHING;

    -- Create user verification record
    INSERT INTO user_verifications (user_id, phone_verified, email_verified)
    VALUES (p_user_id, FALSE, FALSE)
    ON CONFLICT (user_id) DO NOTHING;

    -- Assign newbie badge
    INSERT INTO user_badges (user_id, badge_type, is_active)
    VALUES (p_user_id, 'newbie', TRUE)
    ON CONFLICT (user_id, badge_type, location, category) DO NOTHING;

    -- Create geolocation record (disabled by default)
    INSERT INTO user_geolocations (user_id, latitude, longitude, is_enabled)
    VALUES (p_user_id, 9.0820, 8.6753, FALSE) -- Nigeria center
    ON CONFLICT (user_id) DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- HELPER: Generate referral code for user
-- ============================================================================
CREATE OR REPLACE FUNCTION generate_referral_code(p_user_id UUID, p_username VARCHAR)
RETURNS VARCHAR AS $$
DECLARE
    v_code VARCHAR(20);
    v_year VARCHAR(4);
BEGIN
    v_year := EXTRACT(YEAR FROM NOW())::VARCHAR;
    v_code := UPPER(LEFT(p_username, 8)) || v_year;

    -- Check if code exists and add suffix if needed
    WHILE EXISTS (SELECT 1 FROM user_referral_codes WHERE code = v_code) LOOP
        v_code := UPPER(LEFT(p_username, 6)) || v_year || (RANDOM() * 99)::INT::VARCHAR;
    END LOOP;

    INSERT INTO user_referral_codes (user_id, code, is_active)
    VALUES (p_user_id, v_code, TRUE);

    RETURN v_code;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- END OF SEED DATA
-- ============================================================================
