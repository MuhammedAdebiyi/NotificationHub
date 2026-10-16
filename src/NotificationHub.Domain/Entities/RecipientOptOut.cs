namespace NotificationHub.Domain.Entities;

// A recipient opting out of an organization's campaign emails. Checked on
// every campaign send and enforced by the unsubscribe endpoint.
public class RecipientOptOut
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime OptedOutAt { get; set; }
}
