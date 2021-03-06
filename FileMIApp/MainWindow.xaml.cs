﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Net;
using Dropbox.Api.Files;
using Microsoft.Win32;
using System.Data;

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
        private NewOAuthUtility NewOAuthUtility;

        private string CurrentPath = "";

        private Stream DownloadReader = null;
        private FileStream DownloadFileStream = null;
        private BinaryWriter DownloadWriter = null;
        private byte[] DownloadReadBuffer = new byte[4096];
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
            this.Getfiles();
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
                        path = ((String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + 
                        System.IO.Path.Combine(this.CurrentPath, this.MyTextBox.Text).Replace("\\", "/"))
                    }
                },
                contentType: "application/json",
                authorization: this.Authorization,
                callback: this.CreateFolder_Result
                );

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
            OpenFileDialog fileD = new OpenFileDialog();        
            Nullable<bool> result = fileD.ShowDialog();
            if (result.HasValue && result.Value == false)
            {
                return;
            }
            

                var fs = new FileStream(fileD.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var fileInfo = UniValue.Empty;
                fileInfo["path"] = (String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + System.IO.Path
                                       .Combine(this.CurrentPath, System.IO.Path.GetFileName(fileD.FileName))
                                       .Replace("\\", "/");
                fileInfo["mode"] = "add";
                fileInfo["autorename"] = true;
                fileInfo["mute"] = false;

                OAuthUtility.PostAsync
                ("https://content.dropboxapi.com/2/files/upload",
                    new HttpParameterCollection
                    {
                        {fs}
                    },
                    headers: new NameValueCollection {{"Dropbox-API-Arg", fileInfo.ToString()}},
                    contentType: "application/octet-stream",
                    authorization: this.Authorization,
                    callback: this.Upload_Result,
                    streamWriteCallback: this.Upload_Processing
                );
            

        }

        private void Upload_Processing(object sender, StreamWriteEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<object, StreamWriteEventArgs>(this.Upload_Processing), sender, e);
                return;
            }
        }

        private void Upload_Result(RequestResult result)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<RequestResult>(this.Upload_Result), result);
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

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (MyListBox.SelectedItem == null)
            {
                return;
            }

            var file = (UniValue)this.MyListBox.SelectedItem;

            if (file[".tag"].Equals("folder"))
            {
                // this.CurrentPath = file["path_display"].ToString();
                MessageBox.Show("Cannot download folder, please select a file");
                return;
            }

            SaveFileDialog SaveF = new SaveFileDialog();

            SaveF.FileName = System.IO.Path.GetFileName(file["path_display"].ToString());

            Nullable<bool> result = SaveF.ShowDialog();
            if (result.HasValue && result.Value == false)
            {
                return;
            }


            this.DownloadFileStream = new FileStream(SaveF.FileName, FileMode.Create, FileAccess.Write);
            this.DownloadWriter = new BinaryWriter(this.DownloadFileStream);

            var req = WebRequest.Create("https://content.dropboxapi.com/2/files/download");

            req.Method = "GET";

            req.Headers.Add(HttpRequestHeader.Authorization, this.Authorization.ToString());
            req.Headers.Add("Dropbox-API-Arg", UniValue.Create(new { path = file["path_display"].ToString() }).ToString());

            req.BeginGetResponse(resultB =>
            {
                var resp = req.EndGetResponse(resultB);

                this.DownloadReader = resp.GetResponseStream();

                this.DownloadReader.BeginRead(this.DownloadReadBuffer, 0, this.DownloadReadBuffer.Length,
                    this.DownloadReadCallback, null);
            }, null);

            this.Getfiles();

        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            NewOAuthUtility.DeleteAsync
            (
                "https://api.dropboxapi.com/2/files/delete_v2",
                new HttpParameterCollection
                {
                    new
                    {
                        path = ((String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + System.IO.Path
                                    .Combine(this.CurrentPath, this.MyTextBox.Text).Replace("\\", "/"))
                    }
                },
                contentType: "application/json",
                authorization: this.Authorization,
                callback: this.DeleteFile_Result
            );

        }

        private void DeleteFile_Result(RequestResult result)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<RequestResult>(this.DeleteFile_Result), result);
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





        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        public void Getfiles()
        {
            OAuthUtility.PostAsync
            (
                "https://api.dropboxapi.com/2/files/list_folder",
                new HttpParameterCollection
                {
                    new
                    {
                        path = this.CurrentPath,
                        include_media_info = true
                    }
                    //{"path", this.CurrentPath },
                    //{"access_token", DBB.AccessTocken }
                },
                contentType: "application/json",
                authorization: this.Authorization,
                callback: this.GetFiles_Result
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

                if (!String.IsNullOrEmpty(this.CurrentPath))
                {
                    this.MyListBox.Items.Insert(0, UniValue.ParseJson("{path_display: '..'}"));
                }
            }
            else
            {
                MessageBox.Show(result.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //MyTextBox.Text = MyListBox.SelectedItem.ToString().Split('/')[0];
            //this.MyListBox.DisplayMemberPath = "name".ToString();
            //MyTextBox.Text = MyListBox.SelectedItem.ToString();

            //MyTextBox.Text = MyListBox.DisplayMemberPath = "path_display";

            /*var memberpath = MyListBox.DisplayMemberPath = "path_display";

            foreach (var file in memberpath)
            {
                MyTextBox.Text = MyListBox.SelectedItem.ToString();
            }*/

            //this.MyTextBox.Text = MyListBox.DisplayMemberPath.ToString();

            //var selectedThing = MyListBox.SelectedItem.ToString();
            //MyTextBox.Text = selectedThing.Substring(0, selectedThing.IndexOf("@") + 1);

        }
        
        private void MyListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (MyListBox.SelectedItem == null)
            {
                return;
            }
            var file = (UniValue)this.MyListBox.SelectedItem;

            if (file["path_display"] == "..")
            {
                if (!String.IsNullOrEmpty(this.CurrentPath))
                {
                    this.CurrentPath = System.IO.Path.GetDirectoryName(this.CurrentPath).Replace("\\", "/");
                    if (this.CurrentPath == "/")
                    {
                        this.CurrentPath = "";
                    }
                }
            }
            else
            {
                if (file[".tag"].Equals("folder"))
                {
                    this.CurrentPath = file["path_display"].ToString();
                }
                else
                {
                    SaveFileDialog SaveF = new SaveFileDialog();

                    SaveF.FileName = System.IO.Path.GetFileName(file["path_display"].ToString());

                    Nullable<bool> result = SaveF.ShowDialog();
                    if (result.HasValue && result.Value == false)
                    {
                        return;
                    }


                    this.DownloadFileStream = new FileStream(SaveF.FileName, FileMode.Create, FileAccess.Write);
                    this.DownloadWriter = new BinaryWriter(this.DownloadFileStream);

                    var req = WebRequest.Create("https://content.dropboxapi.com/2/files/download");

                    req.Method = "GET";

                    req.Headers.Add(HttpRequestHeader.Authorization, this.Authorization.ToString());
                    req.Headers.Add("Dropbox-API-Arg", UniValue.Create(new { path = file["path_display"].ToString() }).ToString());

                    req.BeginGetResponse(resultB =>
                    {
                        var resp = req.EndGetResponse(resultB);

                        this.DownloadReader = resp.GetResponseStream();

                        this.DownloadReader.BeginRead(this.DownloadReadBuffer, 0, this.DownloadReadBuffer.Length,
                            this.DownloadReadCallback, null);
                    }, null);


                }
            }
            this.Getfiles();
        }

        private void DownloadReadCallback(IAsyncResult resultB)
        {
            var bytesRead = this.DownloadReader.EndRead(resultB);

            if (bytesRead > 0)
            {
                if (this.DownloadFileStream.CanWrite)
                {
                    this.DownloadWriter.Write(this.DownloadReadBuffer.Take(bytesRead).ToArray());
                    this.DownloadReader.BeginRead(this.DownloadReadBuffer, 0, this.DownloadReadBuffer.Length,
                        DownloadReadCallback, null);
                }
            }
            else
            {
                this.DownloadFileStream.Close();
            }
        }



        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            OAuthUtility.PostAsync
            (
                "https://api.dropboxapi.com/2/files/search",
                new HttpParameterCollection
                {
                    new
                    {                   
                        //string som man skal søge.
                        query = MyTextBox.Text, 
                        // den path i brugerens dropbox man skal søge i.
                        path = this.CurrentPath
                    }
                },
                contentType: "application/json",
                authorization: this.Authorization,
                callback: this.GetFilesSearch_Result
            );

        }

        private void GetFilesSearch_Result(RequestResult result)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<RequestResult>(GetFilesSearch_Result), result);
                return;
            }

            if (result.StatusCode == 200)
            {
                this.MyListBox.Items.Clear();
                this.MyListBox.DisplayMemberPath = "metadata";

                foreach (UniValue file in result["matches"])
                {
                    MyListBox.Items.Add(file);
                }

                if (!String.IsNullOrEmpty(this.CurrentPath))
                {
                    this.MyListBox.Items.Insert(0, UniValue.ParseJson("{path_display: '..'}"));
                }
            }
            else
            {
                MessageBox.Show(result.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void MyListBox_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string file in files)
            {
                var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var fileInfo = UniValue.Empty;
                fileInfo["path"] = (String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + System.IO.Path
                                       .Combine(this.CurrentPath, System.IO.Path.GetFileName(file))
                                       .Replace("\\", "/");
                fileInfo["mode"] = "add";
                fileInfo["autorename"] = true;
                fileInfo["mute"] = false;

                OAuthUtility.PostAsync
                ("https://content.dropboxapi.com/2/files/upload",
                    new HttpParameterCollection
                    {
                        {fs}
                    },
                    headers: new NameValueCollection { { "Dropbox-API-Arg", fileInfo.ToString() } },
                    contentType: "application/octet-stream",
                    authorization: this.Authorization,
                    callback: this.Upload_Result,
                    streamWriteCallback: this.Upload_Processing
                );
            }
        }

        private void MyListBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileD = new OpenFileDialog();
            Nullable<bool> result = fileD.ShowDialog();
            if (result.HasValue && result.Value == false)
            {
                return;
            }


            var fs = new FileStream(fileD.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var fileInfo = UniValue.Empty;
            fileInfo["path"] = (String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + System.IO.Path
                                   .Combine(this.CurrentPath, System.IO.Path.GetFileName(fileD.FileName))
                                   .Replace("\\", "/");
            fileInfo["mode"] = "add";
            fileInfo["autorename"] = true;
            fileInfo["mute"] = false;

            OAuthUtility.PostAsync
            ("https://content.dropboxapi.com/2/files/upload",
                new HttpParameterCollection
                {
                        {fs}
                },
                headers: new NameValueCollection { { "Dropbox-API-Arg", fileInfo.ToString() } },
                contentType: "application/octet-stream",
                authorization: this.Authorization,
                callback: this.Upload_Result,
                streamWriteCallback: this.Upload_Processing
            );

            NewOAuthUtility.DeleteAsync
            (
                "https://api.dropboxapi.com/2/files/delete_v2",
                new HttpParameterCollection
                {
                    new
                    {
                        path = ((String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + System.IO.Path
                                    .Combine(this.CurrentPath, this.MyTextBox.Text).Replace("\\", "/"))
                    }
                },
                contentType: "application/json",
                authorization: this.Authorization,
                callback: this.DeleteFile_Result
            );
        }
    }
}
