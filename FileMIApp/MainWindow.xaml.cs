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
using Dropbox.Api.Auth;
using Nemiro.OAuth;
using Nemiro.OAuth.LoginForms;
using System.IO;



namespace FileMIApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables    
        private string strAppKey = "m45bsokx6gse20d";
        private string strAppSecret = "zs3feykgwil7z26";
        private string strAccessToken = string.Empty;
        private string strAuthenticationURL = string.Empty;
        private DropBoxIntegration DBB;
        private HttpAuthorization Authorization = null;
       
        private string CurrentPath = "";
        #endregion

        #region Constructor    
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Private Methods    
        public void Authenticate()
        {
            try
            {
                if (string.IsNullOrEmpty(strAppKey))
                {
                    MessageBox.Show("Please enter valid App Key !");
                    return;
                }
                if (DBB == null)
                {
                    DBB = new DropBoxIntegration(strAppKey, strAppSecret, "FileMI");

                    strAuthenticationURL = DBB.GeneratedAuthenticationURL(); // This method must be executed before generating Access Token.    
                    strAccessToken = DBB.GenerateAccessToken();
                    gbDropBox.IsEnabled = true;
                    this.Authorization = new HttpAuthorization(AuthorizationType.Bearer, DBB.AccessTocken);
                }
                else gbDropBox.IsEnabled = false;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        private void btnApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                strAppKey = txtApiKey.Text.Trim();
                Authenticate();
            }
            catch (Exception)
            {
                throw;
            }

        }

        private void btnCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            OAuthUtility.PostAsync
                (
                "https://api.dropboxapi.com/2/files/create_folder",
                new HttpParameterCollection
                {
                    new 
                    {
                        path = ((String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + System.IO.Path.Combine(this.CurrentPath, this.MyTextBox.Text).Replace("\\", "/"))
                    }
                },
                contentType: "application/json",
                authorization: this.Authorization,
                callback: this.CreateFolder_Result
                );
            /*
            try
            {
                if (DBB != null)
                {
                    if (strAccessToken != null && strAuthenticationURL != null)
                    {
                        if (DBB.FolderExists(MyTextBox.Text) == false)
                        {
                             DBB.CreateFolder(MyTextBox.Text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex = ex.InnerException ?? ex;
            }
            */

        }

        private void CreateFolder_Result(RequestResult result)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<RequestResult>(this.CreateFolder_Result), result);
                return;
            }

            if (result.StatusCode == 200)
            {
                this.Getfiles();
            }
            else
            {
                if (result["error"].HasValue)
                {
                    MessageBox.Show(result["error"].ToString());
                }
                else
                {
                    MessageBox.Show(result.ToString());
                }
            }
        }

        private void btlUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DBB != null)
                {
                    if (strAccessToken != null && strAuthenticationURL != null)
                    {
                        DBB.Upload("/FileMItest2", "12345.jpg", @"D:\12345.JPG");
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DBB != null)
                {
                    if (strAccessToken != null && strAuthenticationURL != null)
                    {
                        DBB.Download("/FileMItest2", "12345.jpg", @"D:\Billedtest", "12345.jpg");
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DBB != null)
                {
                    if (strAccessToken != null && strAuthenticationURL != null)
                    {
                        DBB.Delete("/Dropbox/DotNetApi");
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        

        

        

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        public void Getfiles()
        {
            OAuthUtility.GetAsync
            (
                "https://api.dropboxapi.com/2/files/get_metadata",
                new HttpParameterCollection
                {
                    {"path", this.CurrentPath },
                    {"access_token", DBB.AccessTocken }
                },
                callback: GetFiles_Result
            );
        }
        public void GetFiles_Result(RequestResult result)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<RequestResult>(GetFiles_Result), result);
                return;
            }

            if (result.StatusCode == 200)
            {
                this.MyListBox.Items.Clear();
                this.MyListBox.DisplayMemberPath = "path_display";

                foreach (UniValue file in result["entries"])
                {
                    MyListBox.Items.Add(file);
                }
            }
            if (!String.IsNullOrEmpty(this.CurrentPath))
            {
                this.MyListBox.Items.Insert(0, UniValue.ParseJson("{path_display: '..'}"));
            }
            else
            {
                MessageBox.Show("Deth");
            }

        }

        private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DBB != null)
                {
                    if (strAccessToken != null && strAuthenticationURL != null)
                    {
                        Getfiles();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
