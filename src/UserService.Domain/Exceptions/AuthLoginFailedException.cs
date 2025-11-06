namespace UserService.Domain.Exceptions;

public class AuthLoginFailedException(string error, string description) : Exception(description)
{
    public string Error { get; } = error;
    public string Description { get; } = description;
}

public class Auth0ErrorResponse
{
    public string Error { get; set; }
    public string Error_Description { get; set; }
}