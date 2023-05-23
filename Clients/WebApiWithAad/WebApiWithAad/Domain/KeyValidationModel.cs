namespace WebApiWithAad.Domain
{
    public class KeyValidationModel
    {
        public string ApiKey { get; set; }
        public string Email { get; set; }
        public string CustomGrantType { get; set; }
    }
}
