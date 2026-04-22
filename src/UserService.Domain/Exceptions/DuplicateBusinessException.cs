/// <summary>
/// Thrown when the BusinessService rejects a registration with a 409 Conflict
/// (duplicate business name, email, or phone number).
/// The message is the human-readable error forwarded from BusinessService.
/// </summary>
public class DuplicateBusinessException(string message) : Exception(message);
