-- ============================================================================
-- Add UNIQUE constraint on user_id in end_user and user_settings tables
-- Version: 2.0.1
-- ============================================================================

-- Add UNIQUE constraint to end_user table if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_end_user_user_id'
    ) THEN
        ALTER TABLE end_user ADD CONSTRAINT uq_end_user_user_id UNIQUE (user_id);
    END IF;
END $$;

-- Add UNIQUE constraint to user_settings table if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_user_settings_user_id'
    ) THEN
        ALTER TABLE user_settings ADD CONSTRAINT uq_user_settings_user_id UNIQUE (user_id);
    END IF;
END $$;
