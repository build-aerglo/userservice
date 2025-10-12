-- 004_Create_Support_User_Profile_Table.sql
CREATE TABLE IF NOT EXISTS support_user_profiles (
                                                     id UUID PRIMARY KEY,
                                                     user_id UUID NOT NULL,
                                                     department TEXT,
                                                     role TEXT,
                                                     permissions JSONB,
                                                     created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                     updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                                     CONSTRAINT fk_support_user_profile_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );
