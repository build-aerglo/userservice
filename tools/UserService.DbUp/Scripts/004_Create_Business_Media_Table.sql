-- 004_Create_Business_Media_Table.sql
CREATE TABLE IF NOT EXISTS business_media (
                                              id UUID PRIMARY KEY,
                                              business_id UUID NOT NULL,
                                              media_url TEXT NOT NULL,
                                              created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                              updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                              CONSTRAINT fk_business_media_business
                                              FOREIGN KEY (business_id)
    REFERENCES businesses(id)
    ON DELETE CASCADE
    );

-- Limit a business to 5 media entries
CREATE OR REPLACE FUNCTION enforce_media_limit()
RETURNS TRIGGER AS $$
BEGIN
    IF (SELECT COUNT(*) FROM business_media WHERE business_id = NEW.business_id) >= 5 THEN
        RAISE EXCEPTION 'A business can have a maximum of 5 media files';
END IF;
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_enforce_media_limit ON business_media;

CREATE TRIGGER trg_enforce_media_limit
    BEFORE INSERT ON business_media
    FOR EACH ROW
    EXECUTE FUNCTION enforce_media_limit();
