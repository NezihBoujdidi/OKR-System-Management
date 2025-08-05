namespace NXM.Tensai.Back.OKR.Domain;

public interface IInvitationLinkRepository : IRepository<InvitationLink>
{
    Task<InvitationLink?> GetByTokenAsync(string token);

}
