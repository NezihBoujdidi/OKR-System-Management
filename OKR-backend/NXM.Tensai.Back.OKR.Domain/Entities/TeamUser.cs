    namespace NXM.Tensai.Back.OKR.Domain;

    public class TeamUser
    {
        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
    