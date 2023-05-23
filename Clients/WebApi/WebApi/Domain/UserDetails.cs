namespace WebApi.Domain
{
    public class UserDetails
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserPassword { get; set; }
        public int Id { get; set; }
        public List<PortalPermission> UserPermissions { get; set; }
        public string ClientAuthId { get; set; }
        public string ApiKey { get; set; }
    }
}
