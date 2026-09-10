using System.Collections.Generic;
using VenueAxe.Domain.Common;

namespace VenueAxe.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PlanTier { get; set; } = "standard";
    public string? StripeCustomerId { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Venue> Venues { get; set; } = new List<Venue>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
