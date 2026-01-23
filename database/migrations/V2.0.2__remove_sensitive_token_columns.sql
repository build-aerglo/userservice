-- ============================================================================
-- Remove sensitive token storage from social_identities table
-- Version: 2.0.2
-- Security: Removes access_token and refresh_token columns to improve security
-- ============================================================================

-- Drop sensitive columns that store OAuth tokens
ALTER TABLE social_identities
    DROP COLUMN IF EXISTS access_token,
    DROP COLUMN IF EXISTS refresh_token,
    DROP COLUMN IF EXISTS token_expires_at;

-- The table now only stores:
-- - user_id: Reference to your user
-- - provider: Social provider name (google, facebook, etc.)
-- - provider_user_id: The user's ID from the social provider
-- - email: User's email from social provider (for reference)
-- - name: User's name from social provider (for reference)
--
-- Auth0 manages the actual OAuth tokens securely
