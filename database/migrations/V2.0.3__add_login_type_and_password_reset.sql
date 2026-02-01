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
    reset_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Indexes for password_reset_requests
CREATE INDEX IF NOT EXISTS idx_password_reset_user_id ON password_reset_requests(id);
CREATE INDEX IF NOT EXISTS idx_password_reset_expires_at ON password_reset_requests(expires_at);

COMMENT ON TABLE password_reset_requests IS 'Tracks password reset requests';
COMMENT ON COLUMN password_reset_requests.reset_id IS 'Primary key for the reset request';
COMMENT ON COLUMN password_reset_requests.id IS 'User ID (foreign key to users)';

-- ============================================================================
-- END OF MIGRATION
-- ============================================================================
