using System;

namespace Auth.Domain.Users
{
    public class UserPasswordReset
    {
        public int UserId { get; set; }
        public string ResetToken { get; set; }
        public DateTime ExpirationDateUtc { get; set; }
        public bool IsUsed { get; set; }
    }
}
