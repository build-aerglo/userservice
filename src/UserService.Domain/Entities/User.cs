namespace UserService.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }
        public string UserType { get; private set; }
        public string? Address { get; private set; }
        public DateTime JoinDate { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public User(string username, string email, string phone, string userType, string? address)
        {
            Id = Guid.NewGuid();
            Username = username;
            Email = email;
            Phone = phone;
            UserType = userType;
            Address = address;
            JoinDate = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Needed for Dapper materialization
        public User() { }

        public void Update(string? email, string? phone, string? address)
        {
            Email = email ?? Email;
            Phone = phone ?? Phone;
            Address = address ?? Address;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}