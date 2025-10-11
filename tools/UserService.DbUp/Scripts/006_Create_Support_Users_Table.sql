-- 006_Create_Support_Users_Table.sql
CREATE TABLE IF NOT EXISTS support_users (
                                             id UUID PRIMARY KEY,
                                             user_id UUID NOT NULL,
                                             created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                             updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                             CONSTRAINT fk_support_user
                                             FOREIGN KEY (user_id)
    REFERENCES users(id)
    ON DELETE CASCADE
    );
