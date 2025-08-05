namespace NXM.Tensai.Back.OKR.Application.Common.Models;

public class SubscriptionPlanDTO
{
    public Guid Id { get; set; }
    public string PlanId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Interval { get; set; }
    public string PlanType { get; set; }
    public bool IsActive { get; set; }
    public List<string> Features { get; set; } = new();
}

public class CreateSubscriptionPlanDTO
{
    public string PlanId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Interval { get; set; } = "month";
    public string PlanType { get; set; }
    public List<string> Features { get; set; } = new();
}

public class UpdateSubscriptionPlanDTO
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Interval { get; set; }
    public string PlanType { get; set; }
    public bool IsActive { get; set; }
    public List<string> Features { get; set; } = new();
} 