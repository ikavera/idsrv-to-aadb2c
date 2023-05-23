using Auth.Domain.Common;
using System.Runtime.Serialization;

namespace Auth.Domain.Users
{
    [DataContract]
    public class PortalPermission
    {
        [DataMember]
        public string PermissionCode { get; set; }

        [DataMember]
        public string PermissionName { get; set; }
    }



    public class UserPermission
    {
        public int UserPermissionId { get; set; }
        public string UserPermissionName { get; set; }
    }

    public class UserPermissionSection
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public UserPermission[] UserPermissions { get; set; }
    }

    public class UserPermissionGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public UserPermissionSection[] UserPermissionSections { get; set; }
    }
}
