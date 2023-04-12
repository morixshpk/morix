using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Morix
{
    public partial class FrmDownload : Form
    {
        private bool _finished = false;
        private readonly RemoteFile _remoteFile = null;
        private ManualResetEvent _evtDownload = null;
        private WebClient _webClient = null;

        public FrmDownload(RemoteFile remoteFile)
        {
            InitializeComponent();
            this._remoteFile = remoteFile;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_finished && DialogResult.No == MessageBox.Show("Update is in progress. Do you really want to cancel?", Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                e.Cancel = true;
                return;
            }
            else
            {
                _webClient?.CancelAsync();
                _evtDownload.Set();
            }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            this._evtDownload = new ManualResetEvent(true);
            this._evtDownload.Reset();
            this.Text = Program.Name;
            this.labelStatus.Text = "Downloading...";
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ProcessDownload));
        }

        private void ProcessDownload(object o)
        {
            bool succeess = false;
            string tempFilePath = Path.Combine(Program.TempFolder, _remoteFile.Name);
            
            try
            {
                this.SetStatus("Downloading...");

                //Download
                _webClient = new WebClient();
                _webClient.Headers.Add("morix", Program.EncryptAes(_remoteFile.Name));
                _webClient.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                {
                    try
                    {
                        this.SetProcessBar(e.ProgressPercentage);
                    }
                    catch (Exception)
                    {
                        //log the error message,you can use the application's log code
                    }
                };

                _webClient.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) =>
                {
                    try
                    {
                        this.SetStatus("Download completed successfully");
                        this.SetProcessBar(100);
                        _evtDownload.Set();
                    }
                    catch (Exception)
                    {
                        //log the error message,you can use the application's log code
                    }
                };

                //Download the folder file
                var uri = new Uri(Program.ServerUrl);
                _webClient.DownloadFileAsync(uri, tempFilePath, _remoteFile);

                //Wait for the download complete
                _evtDownload.WaitOne();

                _webClient.Dispose();
                _webClient = null;
                System.Threading.Thread.Sleep(1000);
                succeess = true;
            }
            catch
            {

            }
            Exit(succeess);
        }

        private void SetStatus(string name)
        {
            if (this.labelStatus.InvokeRequired)
            {
                ShowStatusCallback cb = new ShowStatusCallback(SetStatus);
                this.Invoke(cb, new object[] { name });
            }
            else
            {
                this.labelStatus.Text = name;
            }
        }

        private void SetProcessBar(int total)
        {
            if (this.progressBar1.InvokeRequired)
            {
                SetProcessBarCallback cb = new SetProcessBarCallback(SetProcessBar);
                this.Invoke(cb, new object[] { total });
            }
            else
            {
                this.progressBar1.Value = total;
            }
        }

        private void Exit(bool success)
        {
            if (this.InvokeRequired)
            {
                ExitCallback cb = new ExitCallback(Exit);
                this.Invoke(cb, new object[] { success });
            }
            else
            {
                this._finished = success;
                this.DialogResult = success ? DialogResult.OK : DialogResult.Cancel;
                this.Close();
            }
        }
    }
}