using System.ComponentModel.DataAnnotations;

namespace Auth.Web.UI.Quickstart.Account
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "TheEmailNotValid")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        public string ClientId { get; set; }
    }
}
