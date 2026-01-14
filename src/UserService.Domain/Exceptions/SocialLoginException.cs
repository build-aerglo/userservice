namespace UserService.Domain.Exceptions;

public class SocialLoginException : Exception
{
    public string Provider { get; }
    public string ErrorCode { get; }

    public SocialLoginException(string provider, string errorCode, string message)
        : base(message)
    {
        Provider = provider;
        ErrorCode = errorCode;
    }
}

public class SocialAccountAlreadyLinkedException : Exception
{
    public string Provider { get; }

    public SocialAccountAlreadyLinkedException(string provider)
        : base($"A {provider} account is already linked to another user.")
    {
        Provider = provider;
    }
}

public class SocialAccountNotLinkedException : Exception
{
    public string Provider { get; }

    public SocialAccountNotLinkedException(string provider)
        : base($"No {provider} account is linked to this user.")
    {
        Provider = provider;
    }
}

public class InvalidSocialProviderException : Exception
{
    public string Provider { get; }

    public InvalidSocialProviderException(string provider)
        : base($"Invalid social provider: {provider}")
    {
        Provider = provider;
    }
}
