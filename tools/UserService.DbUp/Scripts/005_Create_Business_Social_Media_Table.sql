-- 005_Create_Business_Social_Media_Table.sql
CREATE TABLE IF NOT EXISTS business_social_media (
                                                     id UUID PRIMARY KEY,
                                                     business_id UUID NOT NULL,
                                                     platform TEXT NOT NULL CHECK (platform IN ('instagram', 'facebook', 'x', 'linkedin')),
    url TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_business_social_media_business
    FOREIGN KEY (business_id)
    REFERENCES businesses(id)
    ON DELETE CASCADE
    );
