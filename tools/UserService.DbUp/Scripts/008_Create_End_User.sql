CREATE TABLE IF NOT EXISTS end_user_profiles (
                                   id UUID PRIMARY KEY,
                                   user_id UUID NOT NULL,
                                   preferences JSONB,
                                   bio TEXT,
                                   social_links JSONB,
                                   created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                   updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                   CONSTRAINT fk_end_user_profile_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);