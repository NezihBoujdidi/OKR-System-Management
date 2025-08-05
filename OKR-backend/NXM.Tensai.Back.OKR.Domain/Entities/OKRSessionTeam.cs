namespace NXM.Tensai.Back.OKR.Domain;

    public class OKRSessionTeam
    {
        public Guid OKRSessionId { get; set; }
        public OKRSession OKRSession { get; set; } = null!;

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }

