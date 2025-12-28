-- 008_Create_Social_Identities_Table.sql
-- Stores social login identities linked to user accounts

CREATE TABLE IF NOT EXISTS social_identities (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    provider TEXT NOT NULL,
    provider_user_id TEXT NOT NULL,
    email TEXT,
    name TEXT,
    access_token TEXT,
    refresh_token TEXT,
    token_expires_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_social_identity_user
        FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_provider_user_id
        UNIQUE (provider, provider_user_id),

    CONSTRAINT uq_user_provider
        UNIQUE (user_id, provider),

    CONSTRAINT chk_provider
        CHECK (provider IN ('google-oauth2', 'facebook', 'apple', 'github', 'twitter', 'linkedin'))
);

-- Index for faster lookups
CREATE INDEX IF NOT EXISTS idx_social_identities_user_id ON social_identities(user_id);
CREATE INDEX IF NOT EXISTS idx_social_identities_provider ON social_identities(provider);
CREATE INDEX IF NOT EXISTS idx_social_identities_provider_user_id ON social_identities(provider, provider_user_id);
