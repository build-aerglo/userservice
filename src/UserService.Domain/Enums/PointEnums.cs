namespace UserService.Domain.Enums;

public enum PointTransactionType
{
    Earn,
    Redeem,
    Expire,
    Adjust,
    Bonus
}

public enum PointActionType
{
    AccountCreated,
    ProfileCompleted,
    EmailVerified,
    PhoneVerified,
    ReviewSubmitted,
    ReviewHelpful,
    ReviewPhotoAdded,
    ReferralSignup,
    ReferralCompleted,
    DailyLogin,
    StreakBonus7Day,
    StreakBonus30Day,
    FirstPurchase,
    SocialShare,
    ProfilePhotoAdded
}
