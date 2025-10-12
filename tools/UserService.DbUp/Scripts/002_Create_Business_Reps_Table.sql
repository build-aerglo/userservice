-- 002_Create_Business_Reps_Table.sql
CREATE TABLE IF NOT EXISTS business_reps (
                                             id UUID PRIMARY KEY,
                                             business_id UUID NOT NULL, -- from BusinessService
                                             user_id UUID NOT NULL,
                                             created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                             updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                             CONSTRAINT fk_business_rep_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );
