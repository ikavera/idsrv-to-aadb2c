using System.ComponentModel.DataAnnotations;

namespace Auth.Web.UI.Quickstart.Account
{
    public class MigrationLoginValidationModel
    {
        [Required]
        [Display(Name = "email")]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
