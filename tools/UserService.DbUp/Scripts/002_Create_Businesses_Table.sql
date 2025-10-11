-- 002_Create_Businesses_Table.sql
CREATE TABLE IF NOT EXISTS businesses (
                                          id UUID PRIMARY KEY,
                                          business_name TEXT NOT NULL,
                                          business_address TEXT NOT NULL,
                                          logo TEXT,
                                          opening_days TEXT,
                                          opening_time TEXT,
                                          business_email TEXT NOT NULL,
                                          business_phone TEXT NOT NULL,
                                          cac_number TEXT,
                                          access_username TEXT,
                                          access_number TEXT,
                                          social_links JSONB,
                                          website_link TEXT,
                                          business_description TEXT,
                                          sector TEXT,
                                          verified BOOLEAN DEFAULT false,
                                          review_link TEXT,
                                          preferred_contact TEXT,
                                          created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                          updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
