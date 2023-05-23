using DesktopClient.Auth;
using DesktopClient.Domain;
using DesktopClient.Services;
using System;
using System.Windows;

namespace DesktopClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailInput.Text.Trim();
            var apiKey = ApiInput.Text.Trim();
            var token = await JwtMapper.GetJwtToken(new AuthSettings
            {
                AuthClientGrantType = AppSettings.AuthClientGrantType,
                AuthClientId = AppSettings.AuthClientId,
                AuthClientScope = AppSettings.AuthClientScope,
                AuthClientTokenParamName = AppSettings.AuthClientTokenParamName,
                AuthServerUrl = AppSettings.AuthServerUrl
            }, email, apiKey);
            if (token == null)
            {
                MessageBox.Show("error");
            }
            else
            {
                MessageBox.Show("succeded");
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }

        private async void ApiCallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!JwtMapper.IsAuthenticated())
            {
                MessageBox.Show("You need to login!");
                return;
            }
            var count = await WebApiService.GetUsersCount();
            MessageBox.Show("Users count: " + count);
        }
    }
}
