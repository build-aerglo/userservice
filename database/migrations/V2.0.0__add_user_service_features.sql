-- ============================================================================
-- User Service Database Schema Migration
-- Version: 2.0.0
-- Description: Creates tables for Badge, Points, Verification, Referral, and
--              Geolocation features
-- ============================================================================

-- ============================================================================
-- USER BADGES TABLE (US-001: Consumer Badge System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_badges (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    badge_type VARCHAR(50) NOT NULL,
    location VARCHAR(100),
    category VARCHAR(100),
    earned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Ensure unique badge per user (considering location and category for specific badges)
    CONSTRAINT uq_user_badge UNIQUE (user_id, badge_type, location, category)
);

-- Indexes for user_badges
CREATE INDEX IF NOT EXISTS idx_user_badges_user_id ON user_badges(user_id);
CREATE INDEX IF NOT EXISTS idx_user_badges_badge_type ON user_badges(badge_type);
CREATE INDEX IF NOT EXISTS idx_user_badges_is_active ON user_badges(is_active);
CREATE INDEX IF NOT EXISTS idx_user_badges_location ON user_badges(location) WHERE location IS NOT NULL;

COMMENT ON TABLE user_badges IS 'Stores badges earned by consumers (Pioneer, Top Contributor, Expert, Pro, etc.)';
COMMENT ON COLUMN user_badges.badge_type IS 'Type: pioneer, top_contributor, expert_category, most_helpful, newbie, expert, pro';
COMMENT ON COLUMN user_badges.location IS 'Location for location-based badges (e.g., Lagos for Top Contributor in Lagos)';
COMMENT ON COLUMN user_badges.category IS 'Category for category-based badges (e.g., Restaurants for Expert in Restaurants)';

-- ============================================================================
-- USER POINTS TABLE (US-002: Consumer Points System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    total_points DECIMAL(12, 2) NOT NULL DEFAULT 0,
    current_streak INTEGER NOT NULL DEFAULT 0,
    longest_streak INTEGER NOT NULL DEFAULT 0,
    last_activity_date DATE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for user_points
CREATE INDEX IF NOT EXISTS idx_user_points_user_id ON user_points(user_id);
CREATE INDEX IF NOT EXISTS idx_user_points_total_points ON user_points(total_points DESC);
CREATE INDEX IF NOT EXISTS idx_user_points_current_streak ON user_points(current_streak DESC);

COMMENT ON TABLE user_points IS 'Stores user point balances and streak information';
COMMENT ON COLUMN user_points.total_points IS 'Cumulative points earned by user';
COMMENT ON COLUMN user_points.current_streak IS 'Current consecutive days with activity';
COMMENT ON COLUMN user_points.longest_streak IS 'Longest consecutive days with activity';

-- ============================================================================
-- POINT TRANSACTIONS TABLE (US-002: Consumer Points System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS point_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    points DECIMAL(10, 2) NOT NULL,
    transaction_type VARCHAR(20) NOT NULL,
    description VARCHAR(500) NOT NULL,
    reference_id UUID,
    reference_type VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for point_transactions
CREATE INDEX IF NOT EXISTS idx_point_transactions_user_id ON point_transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_point_transactions_type ON point_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_point_transactions_reference ON point_transactions(reference_id, reference_type)
    WHERE reference_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_point_transactions_created_at ON point_transactions(created_at DESC);

COMMENT ON TABLE point_transactions IS 'Transaction history for point earnings and deductions';
COMMENT ON COLUMN point_transactions.transaction_type IS 'Type: earn, deduct, bonus, milestone';
COMMENT ON COLUMN point_transactions.reference_type IS 'Type of reference: review, referral, streak, milestone, helpful_vote';

-- ============================================================================
-- USER VERIFICATIONS TABLE (US-003: User Verification System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_verifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    phone_verified BOOLEAN NOT NULL DEFAULT FALSE,
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    phone_verified_at TIMESTAMP WITH TIME ZONE,
    email_verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for user_verifications
CREATE INDEX IF NOT EXISTS idx_user_verifications_user_id ON user_verifications(user_id);
CREATE INDEX IF NOT EXISTS idx_user_verifications_phone_verified ON user_verifications(phone_verified);
CREATE INDEX IF NOT EXISTS idx_user_verifications_email_verified ON user_verifications(email_verified);

COMMENT ON TABLE user_verifications IS 'Tracks phone and email verification status for users';
COMMENT ON COLUMN user_verifications.phone_verified IS 'Whether phone number has been verified via OTP';
COMMENT ON COLUMN user_verifications.email_verified IS 'Whether email has been verified via link';

-- ============================================================================
-- VERIFICATION TOKENS TABLE (US-003: User Verification System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS verification_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    verification_type VARCHAR(20) NOT NULL,
    token VARCHAR(255) NOT NULL,
    target VARCHAR(255) NOT NULL,
    attempts INTEGER NOT NULL DEFAULT 0,
    max_attempts INTEGER NOT NULL DEFAULT 3,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for verification_tokens
CREATE INDEX IF NOT EXISTS idx_verification_tokens_user_id ON verification_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_verification_tokens_token ON verification_tokens(token);
CREATE INDEX IF NOT EXISTS idx_verification_tokens_type ON verification_tokens(verification_type);
CREATE INDEX IF NOT EXISTS idx_verification_tokens_expires_at ON verification_tokens(expires_at);

COMMENT ON TABLE verification_tokens IS 'Stores OTP codes and email verification tokens';
COMMENT ON COLUMN verification_tokens.verification_type IS 'Type: phone, email';
COMMENT ON COLUMN verification_tokens.target IS 'The phone number or email address';
COMMENT ON COLUMN verification_tokens.max_attempts IS 'Maximum verification attempts (default 3 for OTP)';

-- ============================================================================
-- USER REFERRAL CODES TABLE (US-004: Referral System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_referral_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    code VARCHAR(20) NOT NULL UNIQUE,
    total_referrals INTEGER NOT NULL DEFAULT 0,
    successful_referrals INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for user_referral_codes
CREATE INDEX IF NOT EXISTS idx_user_referral_codes_user_id ON user_referral_codes(user_id);
CREATE INDEX IF NOT EXISTS idx_user_referral_codes_code ON user_referral_codes(code);
CREATE INDEX IF NOT EXISTS idx_user_referral_codes_successful ON user_referral_codes(successful_referrals DESC);

COMMENT ON TABLE user_referral_codes IS 'Unique referral codes for each user (e.g., AMAKA2025)';
COMMENT ON COLUMN user_referral_codes.total_referrals IS 'Total users who signed up with this code';
COMMENT ON COLUMN user_referral_codes.successful_referrals IS 'Referrals that completed qualification (3 approved reviews)';

-- ============================================================================
-- REFERRALS TABLE (US-004: Referral System)
-- ============================================================================
CREATE TABLE IF NOT EXISTS referrals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    referrer_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    referred_user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    referral_code VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'registered',
    approved_review_count INTEGER NOT NULL DEFAULT 0,
    points_awarded BOOLEAN NOT NULL DEFAULT FALSE,
    qualified_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for referrals
CREATE INDEX IF NOT EXISTS idx_referrals_referrer_id ON referrals(referrer_id);
CREATE INDEX IF NOT EXISTS idx_referrals_referred_user_id ON referrals(referred_user_id);
CREATE INDEX IF NOT EXISTS idx_referrals_code ON referrals(referral_code);
CREATE INDEX IF NOT EXISTS idx_referrals_status ON referrals(status);
CREATE INDEX IF NOT EXISTS idx_referrals_qualified ON referrals(status, points_awarded)
    WHERE status = 'qualified' AND points_awarded = FALSE;

COMMENT ON TABLE referrals IS 'Tracks referral relationships and their lifecycle';
COMMENT ON COLUMN referrals.status IS 'Status: registered, active, qualified, completed';
COMMENT ON COLUMN referrals.approved_review_count IS 'Number of approved reviews by referred user';
COMMENT ON COLUMN referrals.points_awarded IS 'Whether referrer has received bonus points';

-- ============================================================================
-- USER GEOLOCATIONS TABLE (US-005: GPS Geolocation Tracking)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_geolocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    state VARCHAR(100),
    lga VARCHAR(100),
    city VARCHAR(100),
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    last_updated TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for user_geolocations
CREATE INDEX IF NOT EXISTS idx_user_geolocations_user_id ON user_geolocations(user_id);
CREATE INDEX IF NOT EXISTS idx_user_geolocations_state ON user_geolocations(state) WHERE state IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_user_geolocations_lga ON user_geolocations(lga) WHERE lga IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_user_geolocations_enabled ON user_geolocations(is_enabled);
CREATE INDEX IF NOT EXISTS idx_user_geolocations_coords ON user_geolocations(latitude, longitude);

COMMENT ON TABLE user_geolocations IS 'Current location data for users (opt-in)';
COMMENT ON COLUMN user_geolocations.state IS 'Nigerian state (e.g., Lagos, Abuja FCT)';
COMMENT ON COLUMN user_geolocations.lga IS 'Local Government Area';
COMMENT ON COLUMN user_geolocations.is_enabled IS 'Whether user has enabled location tracking';

-- ============================================================================
-- GEOLOCATION HISTORY TABLE (US-005: GPS Geolocation Tracking)
-- ============================================================================
CREATE TABLE IF NOT EXISTS geolocation_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    state VARCHAR(100),
    lga VARCHAR(100),
    city VARCHAR(100),
    source VARCHAR(20) NOT NULL DEFAULT 'gps',
    vpn_detected BOOLEAN NOT NULL DEFAULT FALSE,
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for geolocation_history
CREATE INDEX IF NOT EXISTS idx_geolocation_history_user_id ON geolocation_history(user_id);
CREATE INDEX IF NOT EXISTS idx_geolocation_history_recorded_at ON geolocation_history(recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_geolocation_history_vpn ON geolocation_history(user_id, vpn_detected)
    WHERE vpn_detected = TRUE;
CREATE INDEX IF NOT EXISTS idx_geolocation_history_source ON geolocation_history(source);

COMMENT ON TABLE geolocation_history IS 'Historical location data for validation and analytics';
COMMENT ON COLUMN geolocation_history.source IS 'Source: gps, ip, manual';
COMMENT ON COLUMN geolocation_history.vpn_detected IS 'Whether VPN usage was detected';

-- ============================================================================
-- END OF MIGRATION
-- ============================================================================
