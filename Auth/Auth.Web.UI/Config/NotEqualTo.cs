using System.ComponentModel.DataAnnotations;

namespace Auth.Web.UI.Config
{
    public class NotEqualTo : ValidationAttribute
    {
        private readonly string _other;
        public NotEqualTo(string other)
        {
            _other = other;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var property = validationContext.ObjectType.GetProperty(_other);
            if (property == null)
            {
                return new ValidationResult($"Unknown property: {_other}");
            }
            var otherValue = property.GetValue(validationContext.ObjectInstance, null);

            if (Equals(value, otherValue))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            return null;
        }
    }

    
}
