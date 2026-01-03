# Aerglo Review Platform - Backend Stories (Business Context)

## Platform Overview

Aerglo is a **Nigerian business review platform** connecting consumers with local businesses through authentic reviews. It serves:

1. **Consumers** - Write reviews, earn badges/points, build reputation
2. **Businesses** - Manage online reputation, respond to reviews, gain insights
3. **Support/Admin** - Moderate content, resolve disputes, manage platform

---

# 1. USER SERVICE STORIES

---

## US-001: Consumer Badge System

### What Is This?
A gamification system rewarding consumers with visual badges based on activity and tenure. Badges appear on profiles and next to reviews, signaling credibility.

### Why It Matters

| Stakeholder | Benefit |
|-------------|---------|
| **Consumers** | Recognition, motivation, visible reputation |
| **Businesses** | Identify trustworthy reviewers, "Pro" reviews carry weight |
| **Platform** | Increases engagement, encourages quality |

### Badge Types

| Badge | Criteria | Signal |
|-------|----------|--------|
| **Pioneer** 🏅 | Joined first 100 days of launch | Early adopter |
| **Top Contributor** 🏆 | Top 5 reviewers in location + lowest disputes | Active AND trustworthy |
| **Expert in [Category]** ⭐ | 10+ reviews in category + high helpful votes | Deep category knowledge |
| **Most Helpful** 👍 | Top 10% by helpful votes | Useful reviews |
| **Newbie** 🌱 | <100 days OR <25 reviews | New user |
| **Expert** 📚 | 100-250 days OR 25-50 reviews | Established |
| **Pro** 💎 | 250+ days OR 50+ reviews | Veteran |

### User Journey
1. Sign up → Get "Newbie" badge
2. Write 25 reviews → Upgrade to "Expert"
3. Hit 50 reviews → Upgrade to "Pro"
4. Become top reviewer in Lagos → Earn "Top Contributor in Lagos"

### Business Rules
- Users can hold multiple badges simultaneously
- Tier badges (Newbie/Expert/Pro) are mutually exclusive
- Badges recalculated daily via background job
- Badges can be revoked if criteria no longer met

### API Endpoints (`/api/badge`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/badge/definitions` | Get all active badge definitions | Public |
| `GET` | `/api/badge/definitions/{id}` | Get badge by ID | Public |
| `GET` | `/api/badge/definitions/category/{category}` | Get badges by category | Public |
| `GET` | `/api/badge/user/{userId}` | Get user's earned badges | Public |
| `GET` | `/api/badge/user/{userId}/level` | Get user's badge level and progress | Public |
| `GET` | `/api/badge/user/{userId}/summary` | Get user's badge summary (level, badges, available) | Public |
| `GET` | `/api/badge/user/{userId}/has/{badgeName}` | Check if user has specific badge | Public |
| `POST` | `/api/badge/award` | Award a badge to user | 🔒 Support |
| `POST` | `/api/badge/user/{userId}/check-eligible` | Check and award eligible badges | Public |
| `POST` | `/api/badge/definitions` | Create new badge definition | 🔒 Support |
| `PUT` | `/api/badge/definitions/{id}` | Update badge definition | 🔒 Support |
| `POST` | `/api/badge/definitions/{id}/activate` | Activate a badge | 🔒 Support |
| `POST` | `/api/badge/definitions/{id}/deactivate` | Deactivate a badge | 🔒 Support |

---

## US-002: Consumer Points System

### What Is This?
A reward system tracking engagement through points. Every meaningful action earns points, reflecting contribution level.

### Why It Matters

| Stakeholder | Benefit |
|-------------|---------|
| **Consumers** | Tangible rewards, motivation, may unlock perks |
| **Businesses** | High-point reviewers are more engaged/thoughtful |
| **Platform** | Drives quality behaviors, creates stickiness |

### Points Rules

**Review Points:**

| Action | Non-Verified | Verified | Why |
|--------|--------------|----------|-----|
| Stars only | 2 pts | 3 pts | Minimal effort |
| Header | 1 pt | 1 pt | Extra detail |
| Body ≤50 chars | 2 pts | 3 pts | Brief |
| Body 51-150 chars | 3 pts | 4.5 pts | More detail |
| Body 151-500 chars | 5 pts | 6.5 pts | Detailed |
| Body 500+ chars | 6 pts | 7.5 pts | Comprehensive |
| Per image (max 3) | 4 pts | 6 pts | Visual proof |

**Milestone Points:**

| Achievement | Non-Verified | Verified |
|-------------|--------------|----------|
| Referral (after 3rd review) | 50 pts | 75 pts |
| 100-day streak | 100 pts | 150 pts |
| 25 reviews | 20 pts | 30 pts |
| 100 helpful votes | 50 pts | 75 pts |
| 500 days + 10 reviews | 500 pts | 750 pts |

### Example
Verified user writes 300-char review with 2 photos:
- Header: 1 pt + Body: 6.5 pts + Images: 12 pts = **19.5 points**

### Streak System
- Write review daily to maintain streak
- Miss a day = streak resets
- 100-day streak = bonus points

### Point Tiers

| Tier | Points | Perks |
|------|--------|-------|
| Bronze | 0-999 | Base |
| Silver | 1,000-4,999 | Profile badge |
| Gold | 5,000-9,999 | Featured status |
| Platinum | 10,000+ | VIP status |

### API Endpoints (`/api/points`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/points/user/{userId}` | Get user's points balance | Public |
| `GET` | `/api/points/user/{userId}/summary` | Get points summary with transactions | Public |
| `GET` | `/api/points/user/{userId}/transactions` | Get transaction history (limit, offset) | Public |
| `GET` | `/api/points/user/{userId}/transactions/type/{type}` | Get transactions by type | Public |
| `GET` | `/api/points/user/{userId}/transactions/range` | Get transactions by date range | Public |
| `GET` | `/api/points/user/{userId}/rank` | Get user's rank on leaderboard | Public |
| `POST` | `/api/points/earn` | Earn points for an action | Public |
| `POST` | `/api/points/redeem` | Redeem points | Public |
| `POST` | `/api/points/adjust` | Admin adjustment of points | 🔒 Support |
| `GET` | `/api/points/rules` | Get all active point rules | Public |
| `GET` | `/api/points/rules/{actionType}` | Get rule by action type | Public |
| `POST` | `/api/points/rules` | Create a point rule | 🔒 Support |
| `PUT` | `/api/points/rules/{id}` | Update a point rule | 🔒 Support |
| `GET` | `/api/points/multipliers` | Get active point multipliers | Public |
| `POST` | `/api/points/multipliers` | Create a multiplier | 🔒 Support |
| `GET` | `/api/points/leaderboard` | Get points leaderboard | Public |

---

## US-003: User Verification System

### What Is This?
A trust-building system allowing users to verify identity via phone (OTP) and email. Verified users earn 50% more points and get a verified badge.

### Why It Matters

| Stakeholder | Benefit |
|-------------|---------|
| **Consumers** | 50% more points, credibility badge |
| **Businesses** | Verified reviews more trustworthy |
| **Platform** | Real people = fewer fake accounts |

### Verification Methods

**Phone (OTP):**
1. Enter Nigerian phone number (+234...)
2. Receive 6-digit SMS code
3. Enter code within 10 minutes (max 3 attempts)
4. Phone verified ✓

**Email (Link):**
1. Request verification email
2. Click link within 24 hours
3. Email verified ✓

### Verification Levels

| Level | Requirements |
|-------|--------------|
| Unverified | Neither verified |
| Partial | Phone OR email |
| Fully Verified | BOTH phone AND email |

### Business Rules
- OTP expires in 10 minutes
- Max 3 OTP attempts
- Email link expires in 24 hours
- Nigerian phone numbers only

### API Endpoints (`/api/verification`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/verification/user/{userId}/status` | Get user's verification status | Public |
| `POST` | `/api/verification/email/send` | Send email verification code | Public |
| `POST` | `/api/verification/email/verify` | Verify email with code | Public |
| `GET` | `/api/verification/email/verify/{token}` | Verify email via token link (24hr expiry) | Public |
| `POST` | `/api/verification/email/resend/{userId}` | Resend email verification | Public |
| `GET` | `/api/verification/email/active/{userId}` | Get active email verification | Public |
| `POST` | `/api/verification/phone/send` | Send phone verification code (OTP) | Public |
| `POST` | `/api/verification/phone/verify` | Verify phone with code (10min expiry, 3 attempts) | Public |
| `POST` | `/api/verification/phone/resend/{userId}` | Resend phone verification | Public |
| `GET` | `/api/verification/phone/active/{userId}` | Get active phone verification | Public |

---

## US-004: Referral System

### What Is This?
A growth mechanism rewarding users for inviting friends. Users get unique referral codes and earn points when referred friends become active.

### Why It Matters

| Stakeholder | Benefit |
|-------------|---------|
| **Consumers** | Bonus points (50-75 per referral) |
| **Platform** | Organic user acquisition, quality users |

### Referral Flow

```
1. Referrer gets unique code (e.g., AMAKA2025)
2. Friend signs up using code → [Registered]
3. Friend writes 1st, 2nd review → [Active]
4. Friend's 3rd APPROVED review → [Qualified]
5. Referrer gets 50/75 points → [Completed]
```

### Why 3 Approved Reviews?
- Ensures referred user is genuinely engaged
- Prevents gaming with fake sign-ups
- Reviews must be approved (not pending/rejected)

### Rewards

| Referrer Status | Points |
|-----------------|--------|
| Non-Verified | 50 pts |
| Verified | 75 pts |

### API Endpoints (`/api/referral`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/referral/user/{userId}/code` | Get or create user's referral code | Public |
| `GET` | `/api/referral/validate/{code}` | Validate a referral code | Public |
| `GET` | `/api/referral/code/{code}` | Look up referral code details | Public |
| `PUT` | `/api/referral/user/{userId}/code/custom` | Set custom referral code | Public |
| `POST` | `/api/referral/use` | Use referral code when signing up | Public |
| `GET` | `/api/referral/user/{userId}/referrals` | Get user's referrals | Public |
| `GET` | `/api/referral/user/{userId}/summary` | Get referral summary | Public |
| `GET` | `/api/referral/user/{userId}/referred-by` | Check who referred a user | Public |
| `POST` | `/api/referral/{referralId}/complete` | Complete a referral (after 3rd approved review) | Public |
| `GET` | `/api/referral/leaderboard` | Get referral leaderboard | Public |
| `GET` | `/api/referral/tiers` | Get reward tiers | Public |
| `GET` | `/api/referral/user/{userId}/tier` | Get user's current reward tier | Public |
| `GET` | `/api/referral/campaign/active` | Get active referral campaign | Public |
| `GET` | `/api/referral/campaigns` | Get all campaigns | Public |
| `POST` | `/api/referral/campaigns` | Create referral campaign | 🔒 Support |
| `POST` | `/api/referral/invite` | Send referral invite | Public |

---

## US-005: GPS Geolocation Tracking

### What Is This?
A location awareness system capturing user geographic location for badges, leaderboards, and review validation.

### Why It Matters

| Stakeholder | Benefit |
|-------------|---------|
| **Consumers** | Location badges, local recommendations |
| **Businesses** | Know if reviewers are in service area |
| **Platform** | Spam detection, local leaderboards |

### Data Captured
- Latitude/Longitude
- State, LGA, City
- Timestamp

### Uses
- "Top Contributor in Lagos" badges
- Review validation (Lagos reviewer vs Abuja business = flag)
- "Near Me" discovery
- VPN detection for spam prevention

### Privacy
- Opt-in (user grants permission)
- Precise coordinates never shown publicly
- Only State/LGA level displayed
- Can disable anytime

### API Endpoints (`/api/location`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/location/record` | Record user's current location | Public |
| `GET` | `/api/location/user/{userId}/latest` | Get user's latest location | Public |
| `GET` | `/api/location/user/{userId}/history` | Get location history (limit, offset) | Public |
| `GET` | `/api/location/user/{userId}/history/range` | Get locations by date range | Public |
| `DELETE` | `/api/location/{locationId}` | Delete a specific location | Public |
| `DELETE` | `/api/location/user/{userId}/history` | Delete all location history | Public |
| `GET` | `/api/location/user/{userId}/saved` | Get user's saved locations | Public |
| `GET` | `/api/location/saved/{locationId}` | Get saved location by ID | Public |
| `GET` | `/api/location/user/{userId}/saved/default` | Get user's default location | Public |
| `POST` | `/api/location/saved` | Create a saved location | Public |
| `PUT` | `/api/location/saved/{locationId}` | Update a saved location | Public |
| `POST` | `/api/location/user/{userId}/saved/{locationId}/set-default` | Set default location | Public |
| `DELETE` | `/api/location/saved/{locationId}` | Delete a saved location | Public |
| `GET` | `/api/location/user/{userId}/preferences` | Get location preferences | Public |
| `PUT` | `/api/location/user/{userId}/preferences` | Update location preferences | Public |
| `GET` | `/api/location/geofences` | Get active geofences | Public |
| `GET` | `/api/location/geofences/{geofenceId}` | Get geofence by ID | Public |
| `POST` | `/api/location/geofences` | Create a geofence | 🔒 Support |
| `PUT` | `/api/location/geofences/{geofenceId}` | Update a geofence | 🔒 Support |
| `DELETE` | `/api/location/geofences/{geofenceId}` | Delete a geofence | 🔒 Support |
| `POST` | `/api/location/geofences/check` | Check if user is inside any geofences | Public |
| `GET` | `/api/location/user/{userId}/geofence-events` | Get user's geofence events | Public |
| `POST` | `/api/location/nearby` | Find nearby users | Public |
| `POST` | `/api/location/user/{userId}/cleanup` | Cleanup old locations | Public |

---

## US-006: Authentication & User Management

### API Endpoints (`/api/auth`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/auth/login` | Login with email/password | Public |
| `POST` | `/api/auth/refresh` | Refresh access token | Public |
| `POST` | `/api/auth/logout` | Logout (clear refresh token) | Public |
| `GET` | `/api/auth/social/providers` | Get available social login providers | Public |
| `POST` | `/api/auth/social/authorize` | Get authorization URL for social login | Public |
| `POST` | `/api/auth/social/callback` | Complete social login with auth code | Public |
| `POST` | `/api/auth/social/link` | Link social account to existing user | 🔒 |
| `DELETE` | `/api/auth/social/link/{provider}` | Unlink social account | 🔒 |
| `GET` | `/api/auth/social/linked` | Get all linked social accounts | 🔒 |

### API Endpoints (`/api/user`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/user/business` | Register new business account | Public |
| `GET` | `/api/user/business/{id}` | Get business user by ID | 🔒 Business/Support |
| `POST` | `/api/user/sub-business` | Create sub-business user | 🔒 Business |
| `PUT` | `/api/user/sub-business/{userId}` | Update sub-business user | 🔒 Business |
| `POST` | `/api/user/support` | Create support user | 🔒 Support |
| `POST` | `/api/user/end-user` | Create end user | Public |
| `GET` | `/api/user/end-user/{userId}/profile` | Get end user profile | Public |
| `PUT` | `/api/user/end-user/{userId}/profile` | Update end user profile | Public |
| `GET` | `/api/user/business-rep/{businessRepId}` | Get business rep details | Public |
| `GET` | `/api/user/business-rep/parent/{businessId}` | Get parent rep by business ID | Public |
| `GET` | `/api/user/support-user/{userId}/exists` | Check if user is support user | Public |

---

# 2. BUSINESS SERVICE STORIES

---

## BS-001: Business Verification Badge System

### What Is This?
A trust indicator showing how thoroughly a business has been verified. Badges (Standard/Verified/Trusted) based on documentation completed.

### Why It Matters

| Stakeholder | Benefit |
|-------------|---------|
| **Consumers** | Know which businesses are legitimate |
| **Businesses** | Higher verification = more trust |
| **Platform** | Ensures quality listings |

### Verification Levels

| Level | Icon | Requirements |
|-------|------|--------------|
| **Standard** | 🎗️ Ribbon | CAC + Phone/Email + Address |
| **Verified** | ✓ Check | + Online presence + Other IDs |
| **Trusted** | 🛡️ Shield | + Business domain email |

### Requirements Explained

| Requirement | Verification Method |
|-------------|---------------------|
| CAC Number | API check against CAC database |
| Phone/Email | OTP/email verification |
| Address | Utility bill or manual check |
| Online Presence | Website/social media exists |
| Other IDs | TIN, licenses (document upload) |
| Business Domain Email | Verify email from company domain |

### Re-Verification Triggers
Changing business name, CAC, address, or domain email requires re-verification.

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/verification-status` | Get business verification status | Public |
| `POST` | `/api/business/{id}/verify/cac` | Submit CAC for verification | 🔒 Business |
| `POST` | `/api/business/{id}/verify/address` | Submit address verification | 🔒 Business |
| `POST` | `/api/business/{id}/verify/domain-email` | Verify business domain email | 🔒 Business |
| `GET` | `/api/business/{id}/verification-documents` | Get submitted documents | 🔒 Business/Support |
| `POST` | `/api/business/{id}/verification-documents` | Upload verification document | 🔒 Business |

---

## BS-002: Subscription Plan Management

### What Is This?
Tiered subscription system offering different feature levels. Basic is free, premium features require higher plans.

### Plan Comparison

| Feature | Basic | Premium | Enterprise |
|---------|-------|---------|------------|
| Reply to Reviews | 10/month | 120/month | Unlimited |
| Dispute Reviews | 5/month | 25/month | Unlimited |
| Private Reviews | ❌ | ✓ | ✓ |
| Data API | ❌ | ❌ | ✓ |
| DnD Mode | ❌ | ❌ | ✓ |
| External Sources | 1 | 3 | Unlimited |
| User Logins | 1 | 3 | 10 |
| Auto-Response | ❌ | ❌ | ✓ |

### Usage Tracking
- Monthly limits reset on billing date
- Exceeding limits blocks action + prompts upgrade
- Usage dashboard shows current consumption

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/subscription/plans` | Get all available plans | Public |
| `GET` | `/api/business/{id}/subscription` | Get business current subscription | 🔒 Business |
| `POST` | `/api/business/{id}/subscription` | Subscribe to a plan | 🔒 Business |
| `PUT` | `/api/business/{id}/subscription` | Upgrade/downgrade plan | 🔒 Business |
| `DELETE` | `/api/business/{id}/subscription` | Cancel subscription | 🔒 Business |
| `GET` | `/api/business/{id}/usage` | Get current usage stats | 🔒 Business |
| `GET` | `/api/business/{id}/usage/history` | Get usage history | 🔒 Business |

---

## BS-003: Multi-User Access (Parent/Child)

### What Is This?
Access control allowing business owner (Parent) to add team members (Child) with limited permissions.

### Permissions

| Action | Parent | Child |
|--------|--------|-------|
| View reviews/analytics | ✓ | ✓ |
| Reply to reviews | ✓ | ❌ |
| Submit disputes | ✓ | ❌ |
| Change settings | ✓ | ❌ |
| Manage users | ✓ | ❌ |

### Child Account Management
- Parent adds child → Child creates password
- Parent can enable/disable child accounts
- Child password reset requires Parent authorization

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/users` | Get all business users | 🔒 Business |
| `POST` | `/api/business/{id}/users` | Add child user | 🔒 Business (Parent) |
| `PUT` | `/api/business/{id}/users/{userId}` | Update child user | 🔒 Business (Parent) |
| `DELETE` | `/api/business/{id}/users/{userId}` | Remove child user | 🔒 Business (Parent) |
| `POST` | `/api/business/{id}/users/{userId}/enable` | Enable child account | 🔒 Business (Parent) |
| `POST` | `/api/business/{id}/users/{userId}/disable` | Disable child account | 🔒 Business (Parent) |

---

## BS-004: Auto-Response Templates

### What Is This?
Enterprise feature that automatically responds to reviews based on sentiment (positive/negative/neutral).

### How It Works
1. New review posted
2. Sentiment analysis classifies as Positive/Negative/Neutral
3. Matching template populated with variables
4. Response posted automatically (still checked for abuse)

### Template Variables
- `{reviewer_name}` - Reviewer's name
- `{business_name}` - Business name
- `{star_rating}` - Review rating

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/auto-response/templates` | Get all templates | 🔒 Business (Enterprise) |
| `POST` | `/api/business/{id}/auto-response/templates` | Create template | 🔒 Business (Enterprise) |
| `PUT` | `/api/business/{id}/auto-response/templates/{templateId}` | Update template | 🔒 Business (Enterprise) |
| `DELETE` | `/api/business/{id}/auto-response/templates/{templateId}` | Delete template | 🔒 Business (Enterprise) |
| `GET` | `/api/business/{id}/auto-response/settings` | Get auto-response settings | 🔒 Business (Enterprise) |
| `PUT` | `/api/business/{id}/auto-response/settings` | Update settings | 🔒 Business (Enterprise) |

---

## BS-005: Do Not Disturb (DnD) Mode

### What Is This?
Enterprise feature temporarily pausing new reviews (max 60 hours). Useful during renovations or crisis situations.

### Rules
- Maximum 60 hours default
- Extensions require Support contact
- Existing reviews remain visible
- Can disable early anytime

### User Message
Consumers see: "This business is temporarily not accepting new reviews."

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/dnd` | Get DnD status | Public |
| `POST` | `/api/business/{id}/dnd/enable` | Enable DnD mode | 🔒 Business (Enterprise) |
| `POST` | `/api/business/{id}/dnd/disable` | Disable DnD mode | 🔒 Business (Enterprise) |
| `POST` | `/api/business/{id}/dnd/extend` | Request extension | 🔒 Support |

---

## BS-006: Private Reviews Mode

### What Is This?
Premium+ feature where reviews are only visible to the business. Consumers see star ratings, but review text is hidden.

### How It Works
- Star ratings visible to everyone
- Review text only visible to business
- Cannot dispute private reviews (no public harm)
- Useful for sensitive industries (healthcare, legal)

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/private-reviews/settings` | Get private reviews settings | 🔒 Business |
| `PUT` | `/api/business/{id}/private-reviews/settings` | Update settings | 🔒 Business (Premium+) |

---

## BS-007: Business Comparison Analytics

### What Is This?
Enterprise feature allowing comparison against own branches and competitors.

### Comparison Types

**Branch Comparison:**

| Metric | Lagos | Abuja | PH |
|--------|-------|-------|-----|
| Avg Rating | 4.5 | 4.2 | 4.7 |
| Reviews | 234 | 156 | 89 |
| Top Complaint | Wait time | Parking | None |

**Competitor Comparison:**
- Compare up to 3 competitors
- Rating trends, sentiment, word clouds
- Data is aggregated (can't see individual reviews)

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/analytics/branches` | Compare branches | 🔒 Business (Enterprise) |
| `GET` | `/api/business/{id}/analytics/competitors` | Compare with competitors | 🔒 Business (Enterprise) |
| `POST` | `/api/business/{id}/analytics/competitors` | Add competitor to track | 🔒 Business (Enterprise) |
| `DELETE` | `/api/business/{id}/analytics/competitors/{competitorId}` | Remove competitor | 🔒 Business (Enterprise) |

---

## BS-008: Unclaimed Business Management

### What Is This?
System allowing legitimate owners to claim businesses that exist on platform but aren't managed.

### Claim Process
1. Owner finds unclaimed business
2. Submits claim with CAC/ID
3. Support reviews (24-48 hours)
4. Approved → Owner gains access

### Claim Requirements
- Full name, email, phone
- CAC number or ID document
- Role declaration (Owner/Manager/Rep)

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/unclaimed` | Search unclaimed businesses | Public |
| `POST` | `/api/business/{id}/claim` | Submit claim request | Public |
| `GET` | `/api/business/claims` | Get all pending claims | 🔒 Support |
| `GET` | `/api/business/claims/{claimId}` | Get claim details | 🔒 Support |
| `POST` | `/api/business/claims/{claimId}/approve` | Approve claim | 🔒 Support |
| `POST` | `/api/business/claims/{claimId}/reject` | Reject claim | 🔒 Support |

---

# 3. REVIEW SERVICE STORIES

---

## RS-001: Review Verification (Spam Detection)

### What Is This?
3-level verification checking every review for spam/fraud before publishing.

### Level 1: Frequency Check
- Same user reviewed business in 12-72 hours?
- Same IP reviewed business recently?
- Same device reviewed business recently?

### Level 2: Content Check
- More than 2 emojis? (spam indicator)
- Blacklisted words? (profanity, hate)
- External links? (spam)
- Personal info? (phone, email)

### Level 3: Location Check
- VPN detected?
- Reviewer location vs business location mismatch?

### Character Limits
- Guest: 50-500 characters
- Registered: 25-500 characters

### Outcomes
- **Approved** → Published immediately
- **Flagged** → Manual review
- **Rejected** → Not published, user notified

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/reviews` | Submit a review | Public |
| `GET` | `/api/reviews/{id}/verification-status` | Get review verification status | 🔒 Support |
| `GET` | `/api/reviews/flagged` | Get flagged reviews | 🔒 Support |
| `POST` | `/api/reviews/{id}/approve` | Approve flagged review | 🔒 Support |
| `POST` | `/api/reviews/{id}/reject` | Reject flagged review | 🔒 Support |

---

## RS-002: Reply Approval System

### What Is This?
Content moderation for business replies ensuring professionalism.

### Rules
**Auto-Approve if:** No abuse, no personal info, no links, no recent warnings

**Auto-Deny if:** Warning in last 30 days OR 2+ rule violations

### Warning System
- 1st violation: Reply rejected
- 2nd violation: Warning issued
- 3rd violation: Replies suspended 30 days

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/reviews/{id}/reply` | Submit reply to review | 🔒 Business |
| `GET` | `/api/business/{id}/reply-warnings` | Get warning history | 🔒 Business/Support |
| `GET` | `/api/replies/pending` | Get pending replies | 🔒 Support |
| `POST` | `/api/replies/{id}/approve` | Approve reply | 🔒 Support |
| `POST` | `/api/replies/{id}/reject` | Reject reply | 🔒 Support |

---

## RS-003: Dispute Management

### What Is This?
Formal system for businesses to challenge rule-violating reviews.

### Dispute Categories
1. Inappropriate Language
2. Fake/Spam
3. Private Information
4. Conflict of Interest
5. Legal Concerns
6. Factually Incorrect

### Requirements
- Premium/Enterprise plan
- Review < 15 days old
- Clear explanation + evidence
- Under monthly dispute limit

### Outcomes
- **Upheld** → Review removed
- **Dismissed** → Review remains

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/reviews/{id}/dispute` | Submit dispute | 🔒 Business (Premium+) |
| `GET` | `/api/business/{id}/disputes` | Get business disputes | 🔒 Business |
| `GET` | `/api/disputes` | Get all disputes | 🔒 Support |
| `GET` | `/api/disputes/{id}` | Get dispute details | 🔒 Support |
| `POST` | `/api/disputes/{id}/uphold` | Uphold dispute (remove review) | 🔒 Support |
| `POST` | `/api/disputes/{id}/dismiss` | Dismiss dispute | 🔒 Support |

---

## RS-004: Bayesian Average Rating

### What Is This?
Fair rating calculation preventing few reviews from artificially skewing ratings.

### The Problem
- New restaurant: 1 review (5 stars) = 5.0 average
- Established café: 500 reviews (4.2 avg) = 4.2 average
- New place appears better unfairly!

### The Solution
Bayesian formula pulls businesses with few reviews toward category average:
- New business (2 reviews, 5.0 avg) → Bayesian: 4.0
- Established (100 reviews, 4.5 avg) → Bayesian: 4.44

As reviews increase, actual average takes over.

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/rating` | Get Bayesian rating | Public |
| `GET` | `/api/business/{id}/rating/breakdown` | Get rating breakdown | Public |
| `GET` | `/api/categories/{id}/average-rating` | Get category average | Public |

---

## RS-005: Helpful Votes System

### What Is This?
Community curation allowing users to vote on helpful reviews. High-helpful reviews surface more prominently.

### Rules
- One vote per user per review
- No self-voting
- No "unhelpful" button (prevents brigading)

### Sorting Options
- Most Recent (default)
- Most Helpful
- Highest/Lowest Rated

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/reviews/{id}/helpful` | Vote review as helpful | 🔒 User |
| `DELETE` | `/api/reviews/{id}/helpful` | Remove helpful vote | 🔒 User |
| `GET` | `/api/reviews/{id}/helpful/count` | Get helpful vote count | Public |
| `GET` | `/api/business/{id}/reviews?sort=helpful` | Get reviews sorted by helpful | Public |

---

## RS-006: Review Edit System

### What Is This?
Limited revision system allowing edits within 3 days. Edits re-verified and marked as edited.

### Rules
- Must edit within 3 days
- Anonymous reviews cannot be edited
- Goes through re-verification
- Shows "Edited" label
- Business notified

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `PUT` | `/api/reviews/{id}` | Edit review (within 3 days) | 🔒 User |
| `GET` | `/api/reviews/{id}/history` | Get edit history | 🔒 Support |

---

## RS-007: Anonymous Review Handling

### What Is This?
Guest review system allowing reviews without accounts, with restrictions.

### Anonymous vs Registered

| Feature | Anonymous | Registered |
|---------|-----------|------------|
| Account needed | No | Yes |
| Email needed | Yes | Optional |
| Can edit | ❌ | ✓ |
| Earns points | ❌ | ✓ |
| Min characters | 50 | 25 |

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/reviews/anonymous` | Submit anonymous review | Public |

---

## RS-008: Sentiment Analysis

### What Is This?
AI classification of reviews as Positive/Negative/Neutral based on content.

### Uses
- Analytics dashboard (sentiment pie chart)
- Auto-response template selection
- Trend analysis ("sentiment improved 15%")
- Alerts ("5 negative reviews in 24 hours")

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/reviews/{id}/sentiment` | Get review sentiment | Public |
| `GET` | `/api/business/{id}/analytics/sentiment` | Get sentiment analytics | 🔒 Business |
| `GET` | `/api/business/{id}/analytics/sentiment/trends` | Get sentiment trends | 🔒 Business |

---

## RS-009: External Source Integration

### What Is This?
Review aggregation importing from external platforms (social media, marketplaces).

### Supported Sources
- X (Twitter), Instagram, Facebook
- Chowdeck, Jumia, JiJi
- Manual CSV upload

### Plan Limits
- Basic: 1 source
- Premium: 3 sources
- Enterprise: Unlimited

### Notes
- Creates unified dashboard
- Cannot reply via Aerglo (must reply on original platform)
- External reviews included in analytics

### API Endpoints (Planned)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/business/{id}/external-sources` | Get connected sources | 🔒 Business |
| `POST` | `/api/business/{id}/external-sources` | Connect external source | 🔒 Business |
| `DELETE` | `/api/business/{id}/external-sources/{sourceId}` | Disconnect source | 🔒 Business |
| `POST` | `/api/business/{id}/external-sources/sync` | Manually sync reviews | 🔒 Business |
| `POST` | `/api/business/{id}/external-sources/import-csv` | Import from CSV | 🔒 Business |
| `GET` | `/api/business/{id}/reviews/external` | Get external reviews | 🔒 Business |

---

# API Summary

## User Service (Implemented)

| Controller | Endpoints | Status |
|------------|-----------|--------|
| Authentication | 9 | ✅ Implemented |
| User Management | 11 | ✅ Implemented |
| Badge System (US-001) | 13 | ✅ Implemented |
| Points System (US-002) | 16 | ✅ Implemented |
| Verification (US-003) | 10 | ✅ Implemented |
| Referral (US-004) | 16 | ✅ Implemented |
| Location (US-005) | 23 | ✅ Implemented |
| **Total** | **98** | |

## Business Service (Planned)

| Feature | Endpoints | Status |
|---------|-----------|--------|
| Business Verification (BS-001) | 6 | 📋 Planned |
| Subscription Plans (BS-002) | 7 | 📋 Planned |
| Multi-User Access (BS-003) | 6 | 📋 Planned |
| Auto-Response (BS-004) | 6 | 📋 Planned |
| DnD Mode (BS-005) | 4 | 📋 Planned |
| Private Reviews (BS-006) | 2 | 📋 Planned |
| Analytics (BS-007) | 4 | 📋 Planned |
| Unclaimed Business (BS-008) | 6 | 📋 Planned |
| **Total** | **41** | |

## Review Service (Planned)

| Feature | Endpoints | Status |
|---------|-----------|--------|
| Spam Detection (RS-001) | 5 | 📋 Planned |
| Reply Approval (RS-002) | 5 | 📋 Planned |
| Dispute Management (RS-003) | 6 | 📋 Planned |
| Bayesian Rating (RS-004) | 3 | 📋 Planned |
| Helpful Votes (RS-005) | 4 | 📋 Planned |
| Review Edit (RS-006) | 2 | 📋 Planned |
| Anonymous Reviews (RS-007) | 1 | 📋 Planned |
| Sentiment Analysis (RS-008) | 3 | 📋 Planned |
| External Sources (RS-009) | 6 | 📋 Planned |
| **Total** | **35** | |

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| 📋 | Planned |
| 🔒 | Requires Authentication |
| 🔒 Business | Requires Business Role |
| 🔒 Support | Requires Support/Admin Role |
| Public | No Authentication Required |
