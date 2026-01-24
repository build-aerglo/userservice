-- ============================================================================
-- Update provider check constraint to match exact Auth0 provider names
-- Version: 2.0.2
-- ============================================================================

-- Drop the old constraint
ALTER TABLE social_identities
    DROP CONSTRAINT IF EXISTS chk_provider;

-- Add new constraint with exact Auth0 provider names
ALTER TABLE social_identities
    ADD CONSTRAINT chk_provider
    CHECK (provider IN ('google-oauth2', 'Facebook', 'Apple', 'GitHub', 'Twitter', 'linkedin'));
