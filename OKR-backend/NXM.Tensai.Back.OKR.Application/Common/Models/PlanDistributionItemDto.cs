public class PlanDistributionItemDto
{
    public string Plan { get; set; }    // e.g. “Basic”, “Pro”
    public int Count { get; set; }      // # of active subs on that plan
}