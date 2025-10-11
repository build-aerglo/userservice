CREATE TABLE IF NOT EXISTS users (
                                     id UUID PRIMARY KEY,
                                     username TEXT NOT NULL,
                                     email TEXT NOT NULL,
                                     phone TEXT,
                                     user_type TEXT CHECK (user_type IN ('end_user', 'business_user', 'support_user')),
    address TEXT,
    join_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
