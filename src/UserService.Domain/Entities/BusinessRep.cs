namespace UserService.Domain.Entities
{
    public class BusinessRep
    {
        public Guid Id { get; private set; }
        public Guid BusinessId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Required for Dapper materialization
        public BusinessRep() { }

        public BusinessRep(Guid businessId, Guid userId)
        {
            Id = Guid.NewGuid();
            BusinessId = businessId;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}