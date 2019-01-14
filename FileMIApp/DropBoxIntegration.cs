using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace FileMIApp
{
    class DropBoxIntegration
    {
        private DropboxClient DBClient;
        private ListFolderArg DBFolders;
        private string oauth2State;
        private const string RedirectUri = "https://filemi.eu.auth0.com/login/callback";

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
        //123
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
        // egen metode over henter en lister af filer
        public bool ListFolder(string path)
        {
            try
            {
                if (AccessTocken == null)
                {
                    throw new Exception("AccessToken Not generated!");
                }
                if (AuthenticationUrl == null)
                {
                    throw new Exception("AuthenticationURI not generated!");
                }
                var foldersArg = new ListFolderArg(path);
                var folder = DBClient.Files.ListFolderAsync(foldersArg);
                
                var result = folder.Result;
                return true;
            }
            catch (Exception e)
            {
                
                throw;
            }
        }
        public bool CreateFolder(string path)
        {
            try
            {
                if (AccessTocken == null)
                {
                    throw new Exception("AccessToken Not generated!");
                }
                if (AuthenticationUrl == null)
                {
                    throw new Exception("AuthenticationURI not generated!");
                }

                var folderArg = new CreateFolderArg(path);
                var folder = DBClient.Files.CreateFolderAsync(folderArg);
                var result = folder.Result;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("OOps");
                return false;
            }
        }

        public bool FolderExists(string path)
        {
            try
            {
                if (AccessTocken == null)
                {
                    throw new Exception("AccessToken Not generated!");
                }
                if (AuthenticationUrl == null)
                {
                    throw new Exception("AuthenticationURI not generated!");
                }

                var folders = DBClient.Files.ListFolderAsync(path);
                var result = folders.Result;
                return true;
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }

        public bool Delete(string path)
        {
            try
            {
                if (AccessTocken == null)
                {
                    throw new Exception("AccessToken Not generated!");
                }
                if (AuthenticationUrl == null)
                {
                    throw new Exception("AuthenticationURI not generated!");
                }
                var folders = DBClient.Files.DeleteAsync(path);
                var result = folders.Result;
                return true;
            }
            catch (Exception ex)
            {
                
                throw;
            }
            

            
        }

        public bool Upload(string UploadfolderPath, string UploadfileName, string SourceFilePath)
        {
            try
            {
                using (var stream = new MemoryStream(File.ReadAllBytes(SourceFilePath)))
                {
                    var response = DBClient.Files.UploadAsync(UploadfolderPath + "/" + UploadfileName,
                        WriteMode.Overwrite.Instance, body: stream);
                    var rest = response.Result;
                }
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        public bool Download(string DropboxFolderPath, string DropboxFileName, string DownloadFolderPath,
            string DownloadFileName)
        {
            try
            {
                var response = DBClient.Files.DownloadAsync(DropboxFolderPath + "/" + DropboxFileName);
                var result = response.Result.GetContentAsStreamAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
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
