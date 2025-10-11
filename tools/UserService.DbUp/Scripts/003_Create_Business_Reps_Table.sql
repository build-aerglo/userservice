-- 003_Create_Business_Reps_Table.sql
CREATE TABLE IF NOT EXISTS business_reps (
                                             id UUID PRIMARY KEY,
                                             business_id UUID NOT NULL,
                                             user_id UUID NOT NULL,
                                             branch_name TEXT,
                                             branch_address TEXT,
                                             created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                             updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                             CONSTRAINT fk_business_reps_business
                                             FOREIGN KEY (business_id)
    REFERENCES businesses(id)
    ON DELETE CASCADE,
    CONSTRAINT fk_business_reps_user
    FOREIGN KEY (user_id)
    REFERENCES users(id)
    ON DELETE CASCADE
    );
