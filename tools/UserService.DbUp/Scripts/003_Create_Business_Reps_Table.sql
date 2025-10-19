-- 003_Create_Business_Reps_Table.sql
DROP TABLE IF EXISTS public.business_reps CASCADE;

CREATE TABLE public.business_reps
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_id UUID NOT NULL, -- no FK constraint here
    user_id UUID NOT NULL REFERENCES public.users (id) ON DELETE CASCADE,
    branch_name TEXT,
    branch_address TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

