namespace NXM.Tensai.Back.OKR.Application
{
    public static class InvitationLinkMapper
    {
        // Mapping from InvitationLink to ValidateKeyDto
        public static ValidateKeyDto ToDto(this InvitationLink invitationLink)
        {
            return new ValidateKeyDto
            {
                Token = invitationLink.Token,
                ExpirationDate = invitationLink.ExpirationDate
            };
        }
    }
}
