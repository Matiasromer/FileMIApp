using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dropbox.Api;
using Dropbox.Api.TeamLog;

namespace FileMIApp
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private const string RedirectUri = "https://filemi.eu.auth0.com/login/callback";
        private string DBAppKey = string.Empty;
        private string DbAuthenticationURL = string.Empty;
        private string DBoauth2State = string.Empty;

        public bool Result
        {
            get;
            private set;
        }

        public string AccessToken
        {
            get;
            private set;
        }

        public string UserId
        {
            get;
            private set;
        }
        /*
        public Login()
        {
            InitializeComponent();

        }
        */
 public Login(string AppKey, string AuthenticationUrl, string oauth2State)
 {
    InitializeComponent();
    DBAppKey = AppKey;
    DbAuthenticationURL = AuthenticationUrl;
    DBoauth2State = oauth2State;
 }

        public void Navigate()
        {
            try
            {
                if (!string.IsNullOrEmpty(DBAppKey))
                {
                    Uri authorizeUri = new Uri(DbAuthenticationURL);
                    Browser.Navigate(authorizeUri);
                }
            }
            catch (Exception e)
            {
                
                throw;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(Navigate));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception )
            {
                
                throw;
            }
        }

        private void Browser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (!e.Uri.AbsoluteUri.ToString().StartsWith(RedirectUri.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                OAuth2Response result = DropboxOAuth2Helper.ParseTokenFragment(e.Uri);
                if (result.State != DBoauth2State)
                {
                    return;
                }

                this.AccessToken = result.AccessToken;
                this.Uid = result.Uid;
                this.Result = true;
            }
            catch (ArgumentException)
            {

            }
            finally
            {
                e.Cancel = true;
                this.Close();
            }
        }
    }
}
