namespace Auth.Web.UI.Quickstart.Account
{
    public class TermViewModel
    {
        public bool Agree { get; set; }

        /// <summary>
        /// uses for correct redirect with angular routing
        /// </summary>
        public string UrlHash { get; set; }
    }
}
