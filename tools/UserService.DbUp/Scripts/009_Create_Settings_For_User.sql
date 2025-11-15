CREATE TABLE user_settings (
                               user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
                               notification_preferences JSONB NOT NULL,
                               dark_mode BOOLEAN NOT NULL DEFAULT FALSE,
                               created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                               updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
