using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using Dropbox.Api;
using Dropbox.Api.Files;
using Microsoft.Win32;
using Nemiro.OAuth;
using Nemiro.OAuth.LoginForms;

namespace FileMIApp
{
    class DropBoxIntegration
    {
        private DropboxClient DBClient;
        private ListFolderArg DBFolders;
        private string oauth2State;
        private const string RedirectUri = "https://filemi.eu.auth0.com/login/callback";
        private string CurrentPath = "";

        //Contructor
        public DropBoxIntegration(string Apikey, string ApiSecret, string ApplicationName = "FileMI")
        {
            try
            {
                AppKey = Apikey;
                AppSecret = ApiSecret;
                AppName = ApplicationName;
            }
            catch (Exception )
            {
                throw;
            }
        }

        public string AppKey
        {
            get;
            private set;
        }

        public string AppSecret
        {
            get;
            private set;
        }

        public string AppName
        {
            get;
            private set;
        }

        public string AuthenticationUrl
        {
            get;
            private set;
        }

        public string AccessTocken
        {
            get;
            private set;
        }

        public string Uid
        {
            get;
            private set;
        }

        public string GeneratedAuthenticationURL()
        {
            try
            {
                this.oauth2State = Guid.NewGuid().ToString("N");
                Uri authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, AppKey, RedirectUri,
                    state: oauth2State);
                AuthenticationUrl = authorizeUri.AbsoluteUri.ToString();
                return authorizeUri.AbsoluteUri.ToString();
            }
            catch (Exception )
            {
                throw;
            }
        }
 public string GenerateAccessToken()
 {
    try
    {
        string _strAccessToken = string.Empty;
        if (CanAuthenticate())
        {
            if (string.IsNullOrEmpty(AuthenticationUrl))
                {
                    throw new Exception("AuthenticationURL is not generated !");
                }
                Login login = new Login(AppKey, AuthenticationUrl, this.oauth2State);
                login.Owner = Application.Current.MainWindow;
                login.ShowDialog();
                if (login.Result)
                {
                    _strAccessToken = login.AccessToken;
                    AccessTocken = login.AccessToken;
                    Uid = login.Uid;
                    DropboxClientConfig CC = new DropboxClientConfig(AppName, 1);
                    HttpClient HTC = new HttpClient();
                    HTC.Timeout = TimeSpan.FromMinutes(10);
                    CC.HttpClient = HTC;
                    DBClient = new DropboxClient(AccessTocken, CC);
                }
                else
                {
                    DBClient = null;
                    AccessTocken = string.Empty;
                    Uid = string.Empty;
                }
        }
        return _strAccessToken;
    }
    catch (Exception ex)
    {               
        throw ex;
    }
 }
        
        
        private bool CanAuthenticate()
        {
            try
            {
                if (AppKey == null)
                {
                    throw new ArgumentNullException("AppKey");
                }
                if (AppSecret == null)
                {
                    throw new ArgumentNullException("AppSecret");
                }
                return true;
            }

            catch (Exception e)
            {
                
                throw;
            }
        }
    }
}
