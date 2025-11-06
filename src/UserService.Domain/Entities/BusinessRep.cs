namespace UserService.Domain.Entities;

/// <summary>
/// Represents a business representative - links a User to a Business
/// This entity maps to the "business_reps" table in PostgreSQL
/// </summary>
public class BusinessRep
{
    // Primary key - unique ID for this business rep relationship
    public Guid Id { get; private set; }
    
    // Foreign key to businesses table (from BusinessService)
    public Guid BusinessId { get; private set; }
    
    // Foreign key to users table
    public Guid UserId { get; private set; }
    public string? BranchName { get; private set; }
    public string? BranchAddress { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Parameterless constructor for Dapper (ORM needs this to create objects from DB)
    protected BusinessRep() { }

    // Domain constructor - used when creating new business reps in code
    public BusinessRep(Guid businessId, Guid userId, string? branchName = null, string? branchAddress = null)
    {
        Id = Guid.NewGuid();
        BusinessId = businessId;
        UserId = userId;
        BranchName = branchName;
        BranchAddress = branchAddress;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Method to update branch information
    public void UpdateBusiness(string? branchName, string? branchAddress)
    {
        if (!string.IsNullOrEmpty(branchName)) BranchName = branchName;
        if (!string.IsNullOrEmpty(branchAddress)) BranchAddress = branchAddress;
        UpdatedAt = DateTime.UtcNow;
    }
}