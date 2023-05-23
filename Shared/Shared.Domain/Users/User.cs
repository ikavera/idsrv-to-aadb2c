using System.Runtime.Serialization;

namespace Shared.Domain.Users
{
    [DataContract]
    public class User
    {
        [DataMember]
        public int Id { get; set; }
        public string Password { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string AadB2CGuid { get; set; }

    }

}
