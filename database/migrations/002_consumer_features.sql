-- ============================================
-- Consumer Features Database Migration
-- Version: 002
-- Date: 2024
-- Description: Adds tables for Badge System, Points System,
--              User Verification, Referral System, and GPS Geolocation
-- ============================================

-- ============================================
-- BADGE SYSTEM TABLES
-- ============================================

-- Badge definitions table
CREATE TABLE IF NOT EXISTS badge_definitions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    icon_url VARCHAR(500),
    tier INT NOT NULL DEFAULT 1, -- 1=Bronze, 2=Silver, 3=Gold, 4=Platinum, 5=Diamond
    points_required INT NOT NULL DEFAULT 0,
    category VARCHAR(50) NOT NULL DEFAULT 'general', -- general, review, referral, engagement, loyalty
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User badges (earned badges)
CREATE TABLE IF NOT EXISTS user_badges (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    badge_id UUID NOT NULL REFERENCES badge_definitions(id) ON DELETE CASCADE,
    earned_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    source VARCHAR(100), -- what action triggered this badge
    metadata JSONB DEFAULT '{}',
    UNIQUE(user_id, badge_id)
);

-- Current badge level for user (Pioneer, Expert, Pro, Master, Legend)
CREATE TABLE IF NOT EXISTS user_badge_levels (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    current_level VARCHAR(50) NOT NULL DEFAULT 'Pioneer', -- Pioneer, Expert, Pro, Master, Legend
    level_progress INT NOT NULL DEFAULT 0, -- Progress towards next level (0-100)
    total_badges_earned INT NOT NULL DEFAULT 0,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- POINTS SYSTEM TABLES
-- ============================================

-- Point rules definitions
CREATE TABLE IF NOT EXISTS point_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action_type VARCHAR(100) NOT NULL UNIQUE, -- review_submitted, referral_complete, profile_complete, etc.
    points_value INT NOT NULL,
    description TEXT,
    max_daily_occurrences INT, -- NULL means unlimited
    max_total_occurrences INT, -- NULL means unlimited
    cooldown_minutes INT, -- NULL means no cooldown
    is_active BOOLEAN NOT NULL DEFAULT true,
    multiplier_eligible BOOLEAN NOT NULL DEFAULT true, -- Can bonus multipliers apply
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User points balance
CREATE TABLE IF NOT EXISTS user_points (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    total_points INT NOT NULL DEFAULT 0,
    available_points INT NOT NULL DEFAULT 0, -- Points that can be redeemed
    lifetime_points INT NOT NULL DEFAULT 0, -- Total points ever earned
    redeemed_points INT NOT NULL DEFAULT 0,
    pending_points INT NOT NULL DEFAULT 0, -- Points awaiting confirmation
    last_earned_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Points transactions history
CREATE TABLE IF NOT EXISTS point_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    rule_id UUID REFERENCES point_rules(id),
    transaction_type VARCHAR(20) NOT NULL, -- earn, redeem, expire, adjust, bonus
    points INT NOT NULL, -- Positive for earn, negative for redeem/expire
    balance_after INT NOT NULL,
    description TEXT,
    reference_type VARCHAR(50), -- review, referral, order, etc.
    reference_id UUID, -- ID of the related entity
    multiplier DECIMAL(3,2) DEFAULT 1.00,
    expires_at TIMESTAMP WITH TIME ZONE, -- When these points expire (if applicable)
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Points multiplier events (double points weekends, etc.)
CREATE TABLE IF NOT EXISTS point_multipliers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    multiplier DECIMAL(3,2) NOT NULL DEFAULT 1.00,
    action_types TEXT[], -- NULL means applies to all actions
    starts_at TIMESTAMP WITH TIME ZONE NOT NULL,
    ends_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Daily point tracking for rate limiting
CREATE TABLE IF NOT EXISTS user_daily_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    action_type VARCHAR(100) NOT NULL,
    occurrence_date DATE NOT NULL DEFAULT CURRENT_DATE,
    occurrence_count INT NOT NULL DEFAULT 1,
    last_occurrence_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, action_type, occurrence_date)
);

-- ============================================
-- USER VERIFICATION TABLES
-- ============================================

-- Email verification
CREATE TABLE IF NOT EXISTS email_verifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    verification_code VARCHAR(10) NOT NULL,
    verification_token UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    is_verified BOOLEAN NOT NULL DEFAULT false,
    verified_at TIMESTAMP WITH TIME ZONE,
    attempts INT NOT NULL DEFAULT 0,
    max_attempts INT NOT NULL DEFAULT 5,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Phone verification
CREATE TABLE IF NOT EXISTS phone_verifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    phone_number VARCHAR(20) NOT NULL,
    country_code VARCHAR(5) NOT NULL DEFAULT '+1',
    verification_code VARCHAR(10) NOT NULL,
    verification_method VARCHAR(20) NOT NULL DEFAULT 'sms', -- sms, voice, whatsapp
    is_verified BOOLEAN NOT NULL DEFAULT false,
    verified_at TIMESTAMP WITH TIME ZONE,
    attempts INT NOT NULL DEFAULT 0,
    max_attempts INT NOT NULL DEFAULT 5,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User verification status (summary)
CREATE TABLE IF NOT EXISTS user_verification_status (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    email_verified BOOLEAN NOT NULL DEFAULT false,
    email_verified_at TIMESTAMP WITH TIME ZONE,
    phone_verified BOOLEAN NOT NULL DEFAULT false,
    phone_verified_at TIMESTAMP WITH TIME ZONE,
    identity_verified BOOLEAN NOT NULL DEFAULT false, -- Future: ID verification
    identity_verified_at TIMESTAMP WITH TIME ZONE,
    verification_level VARCHAR(20) NOT NULL DEFAULT 'none', -- none, basic, verified, trusted
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- REFERRAL SYSTEM TABLES
-- ============================================

-- User referral codes
CREATE TABLE IF NOT EXISTS user_referral_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    referral_code VARCHAR(20) NOT NULL UNIQUE,
    custom_code VARCHAR(20), -- User-chosen custom code
    is_active BOOLEAN NOT NULL DEFAULT true,
    total_referrals INT NOT NULL DEFAULT 0,
    successful_referrals INT NOT NULL DEFAULT 0, -- Completed required actions
    pending_referrals INT NOT NULL DEFAULT 0,
    total_points_earned INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Referral tracking
CREATE TABLE IF NOT EXISTS referrals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    referrer_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    referred_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    referral_code_id UUID NOT NULL REFERENCES user_referral_codes(id),
    referral_code VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, registered, completed, expired, cancelled
    referred_email VARCHAR(255),
    referred_phone VARCHAR(20),
    referrer_reward_points INT DEFAULT 0,
    referred_reward_points INT DEFAULT 0,
    referrer_rewarded BOOLEAN NOT NULL DEFAULT false,
    referred_rewarded BOOLEAN NOT NULL DEFAULT false,
    completion_requirements JSONB DEFAULT '{}', -- What the referred user needs to do
    completed_requirements JSONB DEFAULT '{}', -- What they've completed
    expires_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Referral reward tiers
CREATE TABLE IF NOT EXISTS referral_reward_tiers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tier_name VARCHAR(50) NOT NULL,
    min_referrals INT NOT NULL DEFAULT 0,
    max_referrals INT, -- NULL means unlimited
    referrer_points INT NOT NULL DEFAULT 0,
    referred_points INT NOT NULL DEFAULT 0,
    bonus_multiplier DECIMAL(3,2) DEFAULT 1.00,
    additional_rewards JSONB DEFAULT '{}', -- badges, perks, etc.
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Referral campaigns
CREATE TABLE IF NOT EXISTS referral_campaigns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    bonus_referrer_points INT DEFAULT 0,
    bonus_referred_points INT DEFAULT 0,
    multiplier DECIMAL(3,2) DEFAULT 1.00,
    starts_at TIMESTAMP WITH TIME ZONE NOT NULL,
    ends_at TIMESTAMP WITH TIME ZONE NOT NULL,
    max_referrals_per_user INT, -- NULL means unlimited
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- GPS GEOLOCATION TABLES
-- ============================================

-- User location history
CREATE TABLE IF NOT EXISTS user_locations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    accuracy DECIMAL(10, 2), -- Accuracy in meters
    altitude DECIMAL(10, 2),
    altitude_accuracy DECIMAL(10, 2),
    heading DECIMAL(5, 2), -- Direction in degrees
    speed DECIMAL(10, 2), -- Speed in m/s
    source VARCHAR(20) NOT NULL DEFAULT 'gps', -- gps, network, ip, manual
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    country_code VARCHAR(5),
    postal_code VARCHAR(20),
    timezone VARCHAR(50),
    metadata JSONB DEFAULT '{}',
    recorded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User's preferred/saved locations
CREATE TABLE IF NOT EXISTS user_saved_locations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL, -- Home, Work, Gym, etc.
    label VARCHAR(50), -- custom label
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    country_code VARCHAR(5),
    postal_code VARCHAR(20),
    is_default BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, name)
);

-- User location preferences
CREATE TABLE IF NOT EXISTS user_location_preferences (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    location_sharing_enabled BOOLEAN NOT NULL DEFAULT false,
    share_with_businesses BOOLEAN NOT NULL DEFAULT false,
    share_precise_location BOOLEAN NOT NULL DEFAULT false, -- If false, only share approximate
    location_history_enabled BOOLEAN NOT NULL DEFAULT true,
    max_history_days INT NOT NULL DEFAULT 90, -- Auto-delete after X days
    auto_detect_timezone BOOLEAN NOT NULL DEFAULT true,
    default_search_radius_km DECIMAL(10, 2) DEFAULT 25.00,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Geofence definitions (for location-based triggers)
CREATE TABLE IF NOT EXISTS geofences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    radius_meters DECIMAL(10, 2) NOT NULL,
    geofence_type VARCHAR(50) NOT NULL DEFAULT 'circle', -- circle, polygon
    polygon_points JSONB, -- For polygon type
    trigger_on_enter BOOLEAN NOT NULL DEFAULT true,
    trigger_on_exit BOOLEAN NOT NULL DEFAULT false,
    trigger_on_dwell BOOLEAN NOT NULL DEFAULT false,
    dwell_time_seconds INT DEFAULT 300, -- Time to trigger dwell event
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User geofence events
CREATE TABLE IF NOT EXISTS user_geofence_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    geofence_id UUID NOT NULL REFERENCES geofences(id) ON DELETE CASCADE,
    event_type VARCHAR(20) NOT NULL, -- enter, exit, dwell
    location_id UUID REFERENCES user_locations(id),
    triggered_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB DEFAULT '{}'
);

-- ============================================
-- INDEXES FOR PERFORMANCE
-- ============================================

-- Badge indexes
CREATE INDEX IF NOT EXISTS idx_user_badges_user_id ON user_badges(user_id);
CREATE INDEX IF NOT EXISTS idx_user_badges_badge_id ON user_badges(badge_id);
CREATE INDEX IF NOT EXISTS idx_badge_definitions_category ON badge_definitions(category);

-- Points indexes
CREATE INDEX IF NOT EXISTS idx_point_transactions_user_id ON point_transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_point_transactions_created_at ON point_transactions(created_at);
CREATE INDEX IF NOT EXISTS idx_point_transactions_type ON point_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_user_daily_points_lookup ON user_daily_points(user_id, action_type, occurrence_date);

-- Verification indexes
CREATE INDEX IF NOT EXISTS idx_email_verifications_user_id ON email_verifications(user_id);
CREATE INDEX IF NOT EXISTS idx_email_verifications_token ON email_verifications(verification_token);
CREATE INDEX IF NOT EXISTS idx_phone_verifications_user_id ON phone_verifications(user_id);

-- Referral indexes
CREATE INDEX IF NOT EXISTS idx_referrals_referrer ON referrals(referrer_user_id);
CREATE INDEX IF NOT EXISTS idx_referrals_referred ON referrals(referred_user_id);
CREATE INDEX IF NOT EXISTS idx_referrals_code ON referrals(referral_code);
CREATE INDEX IF NOT EXISTS idx_referrals_status ON referrals(status);
CREATE INDEX IF NOT EXISTS idx_user_referral_codes_code ON user_referral_codes(referral_code);

-- Location indexes
CREATE INDEX IF NOT EXISTS idx_user_locations_user_id ON user_locations(user_id);
CREATE INDEX IF NOT EXISTS idx_user_locations_recorded_at ON user_locations(recorded_at);
CREATE INDEX IF NOT EXISTS idx_user_locations_coords ON user_locations(latitude, longitude);
CREATE INDEX IF NOT EXISTS idx_user_saved_locations_user_id ON user_saved_locations(user_id);
CREATE INDEX IF NOT EXISTS idx_geofences_coords ON geofences(latitude, longitude);
CREATE INDEX IF NOT EXISTS idx_user_geofence_events_user_id ON user_geofence_events(user_id);

-- ============================================
-- INSERT DEFAULT DATA
-- ============================================

-- Default badge definitions
INSERT INTO badge_definitions (name, display_name, description, tier, points_required, category) VALUES
    ('pioneer', 'Pioneer', 'Welcome to the community! You''re taking your first steps.', 1, 0, 'general'),
    ('explorer', 'Explorer', 'You''ve started exploring and engaging with content.', 1, 100, 'engagement'),
    ('contributor', 'Contributor', 'Thanks for contributing valuable reviews!', 2, 250, 'review'),
    ('expert', 'Expert', 'Your expertise is recognized by the community.', 3, 500, 'general'),
    ('influencer', 'Influencer', 'Your referrals are growing the community.', 2, 0, 'referral'),
    ('super_referrer', 'Super Referrer', 'You''ve successfully referred 10+ users.', 3, 0, 'referral'),
    ('pro', 'Pro', 'You''re a pro-level member with significant contributions.', 4, 1000, 'general'),
    ('reviewer_bronze', 'Bronze Reviewer', 'You''ve submitted your first 5 reviews.', 1, 0, 'review'),
    ('reviewer_silver', 'Silver Reviewer', 'You''ve submitted 25 quality reviews.', 2, 0, 'review'),
    ('reviewer_gold', 'Gold Reviewer', 'You''ve submitted 100 quality reviews.', 3, 0, 'review'),
    ('master', 'Master', 'You''ve mastered the platform with exceptional engagement.', 4, 2500, 'general'),
    ('legend', 'Legend', 'Legendary status achieved! You''re among the elite.', 5, 5000, 'general'),
    ('verified_user', 'Verified User', 'Your identity has been verified.', 2, 0, 'general'),
    ('early_adopter', 'Early Adopter', 'Thanks for being an early member of our community!', 2, 0, 'loyalty'),
    ('loyalty_1year', '1 Year Member', 'Celebrating 1 year with us!', 2, 0, 'loyalty'),
    ('loyalty_2year', '2 Year Member', 'Celebrating 2 years with us!', 3, 0, 'loyalty')
ON CONFLICT (name) DO NOTHING;

-- Default point rules
INSERT INTO point_rules (action_type, points_value, description, max_daily_occurrences, cooldown_minutes) VALUES
    ('account_created', 50, 'Points for creating an account', 1, NULL),
    ('profile_completed', 100, 'Points for completing your profile', 1, NULL),
    ('email_verified', 25, 'Points for verifying your email', 1, NULL),
    ('phone_verified', 50, 'Points for verifying your phone number', 1, NULL),
    ('review_submitted', 20, 'Points for submitting a review', 10, 60),
    ('review_helpful', 5, 'Points when your review is marked helpful', 50, NULL),
    ('review_photo_added', 10, 'Bonus points for adding photos to review', 10, NULL),
    ('referral_signup', 50, 'Points when your referral signs up', NULL, NULL),
    ('referral_completed', 100, 'Points when referral completes first action', NULL, NULL),
    ('daily_login', 5, 'Points for daily login', 1, 1440),
    ('streak_bonus_7day', 25, 'Bonus for 7-day login streak', NULL, NULL),
    ('streak_bonus_30day', 100, 'Bonus for 30-day login streak', NULL, NULL),
    ('first_purchase', 75, 'Points for first purchase through platform', 1, NULL),
    ('social_share', 10, 'Points for sharing on social media', 3, 60),
    ('profile_photo_added', 15, 'Points for adding a profile photo', 1, NULL)
ON CONFLICT (action_type) DO NOTHING;

-- Default referral reward tiers
INSERT INTO referral_reward_tiers (tier_name, min_referrals, max_referrals, referrer_points, referred_points, bonus_multiplier) VALUES
    ('Starter', 0, 4, 100, 50, 1.00),
    ('Bronze', 5, 14, 125, 50, 1.10),
    ('Silver', 15, 29, 150, 75, 1.25),
    ('Gold', 30, 49, 200, 100, 1.50),
    ('Platinum', 50, NULL, 300, 150, 2.00)
ON CONFLICT DO NOTHING;

-- ============================================
-- FUNCTIONS FOR POINTS CALCULATION
-- ============================================

-- Function to calculate distance between two points (Haversine formula)
CREATE OR REPLACE FUNCTION calculate_distance_km(
    lat1 DECIMAL, lon1 DECIMAL,
    lat2 DECIMAL, lon2 DECIMAL
) RETURNS DECIMAL AS $$
DECLARE
    r DECIMAL := 6371; -- Earth's radius in km
    dlat DECIMAL;
    dlon DECIMAL;
    a DECIMAL;
    c DECIMAL;
BEGIN
    dlat := radians(lat2 - lat1);
    dlon := radians(lon2 - lon1);
    a := sin(dlat/2)^2 + cos(radians(lat1)) * cos(radians(lat2)) * sin(dlon/2)^2;
    c := 2 * asin(sqrt(a));
    RETURN r * c;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Function to check if point is within geofence
CREATE OR REPLACE FUNCTION is_within_geofence(
    point_lat DECIMAL, point_lon DECIMAL,
    fence_lat DECIMAL, fence_lon DECIMAL,
    radius_meters DECIMAL
) RETURNS BOOLEAN AS $$
BEGIN
    RETURN calculate_distance_km(point_lat, point_lon, fence_lat, fence_lon) * 1000 <= radius_meters;
END;
$$ LANGUAGE plpgsql IMMUTABLE;
