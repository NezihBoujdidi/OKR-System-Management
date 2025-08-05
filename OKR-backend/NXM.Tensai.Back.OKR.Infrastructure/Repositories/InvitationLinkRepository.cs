namespace NXM.Tensai.Back.OKR.Infrastructure
{
    public class InvitationLinkRepository : Repository<InvitationLink>, IInvitationLinkRepository
    {
        public InvitationLinkRepository(OKRDbContext context) : base(context)
        {
        }
        public async Task<InvitationLink?> GetByTokenAsync(string token)
        {
            return await _dbSet.SingleOrDefaultAsync(link => link.Token == token);
        }
    }
}
