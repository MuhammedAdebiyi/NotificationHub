using NotificationHub.Domain.Common;

namespace NotificationHub.Domain.Entities;

public class Organization : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "campaigns@coursevaultai.app";

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Template> Templates { get; set; } = new List<Template>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}