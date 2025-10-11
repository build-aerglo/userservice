-- 007_Create_Reviews_Table.sql
CREATE TABLE IF NOT EXISTS reviews (
                                       id UUID PRIMARY KEY,
                                       business_id UUID NOT NULL,
                                       user_id UUID NOT NULL,
                                       rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_reviews_business
    FOREIGN KEY (business_id)
    REFERENCES businesses(id)
    ON DELETE CASCADE,
    CONSTRAINT fk_reviews_user
    FOREIGN KEY (user_id)
    REFERENCES users(id)
    ON DELETE CASCADE
    );

-- Optional: Index for faster lookups
CREATE INDEX IF NOT EXISTS idx_reviews_business_id ON reviews(business_id);
CREATE INDEX IF NOT EXISTS idx_reviews_user_id ON reviews(user_id);
