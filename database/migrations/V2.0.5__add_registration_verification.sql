-- ============================================================================
-- Migration V2.0.5
-- Description: Add registration_verification table for email verification on
--              sign-up, and add is_email_verified column to users table.
-- ============================================================================

-- Add is_email_verified to users table
ALTER TABLE users
    ADD COLUMN IF NOT EXISTS is_email_verified BOOLEAN NOT NULL DEFAULT FALSE;

-- ============================================================================
-- REGISTRATION VERIFICATION TABLE
-- Stores pending email verification records created on user sign-up.
-- The token is the AES-encrypted email address used to validate the link.
-- ============================================================================
CREATE TABLE IF NOT EXISTS registration_verification (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email       VARCHAR(255) NOT NULL,
    username    VARCHAR(255) NOT NULL,
    token       TEXT         NOT NULL,
    expiry      TIMESTAMP WITH TIME ZONE NOT NULL,
    user_type   VARCHAR(50)  NOT NULL,
    created_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_registration_verification_email ON registration_verification(email);

COMMENT ON TABLE registration_verification IS 'Pending email-verification records sent on sign-up (24 h TTL)';
COMMENT ON COLUMN registration_verification.token     IS 'AES-encrypted email address; decrypting it yields the owner email';
COMMENT ON COLUMN registration_verification.expiry    IS 'Token expiry — 24 hours after creation';
COMMENT ON COLUMN registration_verification.user_type IS 'end_user or business_user';
