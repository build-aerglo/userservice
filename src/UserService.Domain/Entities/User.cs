namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = default!;
    public string Password { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string UserType { get; private set; } = default!;
    public string? Address { get; private set; }
    public DateTime JoinDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public string Auth0UserId { get; private set; }

    // 🟢 This is the key addition — Dapper needs a parameterless constructor
    protected User() { }

    // ✅ Domain-level constructor (for creating new users in code)
    public User(string username, string email, string phone, string password,string userType, string? address,string auth0UserId)
    {
        Id = Guid.NewGuid();
        Username = username;
        Email = email;
        Phone = phone;
        Password = password;
        UserType = userType;
        Address = address;
        JoinDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Auth0UserId = auth0UserId;
    }

    public void Update(string? email, string? phone, string? address)
    {
        if (!string.IsNullOrEmpty(email)) Email = email;
        if (!string.IsNullOrEmpty(phone)) Phone = phone;
        if (!string.IsNullOrEmpty(address)) Address = address;
        UpdatedAt = DateTime.UtcNow;
    }
}