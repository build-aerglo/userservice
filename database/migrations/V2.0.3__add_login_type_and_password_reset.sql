-- ============================================================================
-- User Service Database Schema Migration
-- Version: 2.0.3
-- Description: Adds login_type column to users table and creates
--              password_reset_requests table
-- ============================================================================

-- ============================================================================
-- ADD LOGIN_TYPE COLUMN TO USERS TABLE
-- ============================================================================
ALTER TABLE users
ADD COLUMN IF NOT EXISTS login_type VARCHAR(50) NOT NULL DEFAULT 'email-password';

COMMENT ON COLUMN users.login_type IS 'Login type: email-password, social_handle';

-- ============================================================================
-- PASSWORD RESET REQUESTS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS password_reset_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    identifier VARCHAR(255) NOT NULL,
    identifier_type VARCHAR(20) NOT NULL,
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    verified_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Ensure identifier is either email or phone format
    CONSTRAINT chk_identifier_type CHECK (identifier_type IN ('email', 'sms'))
);

-- Indexes for password_reset_requests
CREATE INDEX IF NOT EXISTS idx_password_reset_user_id ON password_reset_requests(user_id);
CREATE INDEX IF NOT EXISTS idx_password_reset_identifier ON password_reset_requests(identifier);
CREATE INDEX IF NOT EXISTS idx_password_reset_expires_at ON password_reset_requests(expires_at);
CREATE INDEX IF NOT EXISTS idx_password_reset_is_verified ON password_reset_requests(is_verified);

COMMENT ON TABLE password_reset_requests IS 'Tracks password reset requests and OTP verification status';
COMMENT ON COLUMN password_reset_requests.identifier IS 'The email or phone number used for reset';
COMMENT ON COLUMN password_reset_requests.identifier_type IS 'Type: email or sms';
COMMENT ON COLUMN password_reset_requests.is_verified IS 'Whether the OTP has been verified';

-- ============================================================================
-- END OF MIGRATION
-- ============================================================================
