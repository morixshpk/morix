using Microsoft.Win32;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace Morix
{
    public partial class FrmMain : Form
    {
        bool _canInstallOrUpdate;
        Version _version;
        string _programFilesDir;
        string _productDir;
        string _productExePath;
        int _selectedProduct = 0;
        readonly string[] _products = new string[4] { "None", "Accounts", "Morix POS", "Morix Exchange" };

        string _company;
        string _productName;
        string _productExe;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, System.EventArgs e)
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (isAdmin == false)
            {
                MessageBox.Show("Run as admin this program!");
                Application.Exit();
                return;
            }

            FillProducts();

            DetectFramework();

            DetectSqlLocalDB();
        }

        private void FillProducts()
        {
            this.cbxProduct.Items.Clear();
            foreach (var item in _products)
                this.cbxProduct.Items.Add(item);

            this.cbxProduct.SelectedIndex = 0;
        }

        private void DetectFramework()
        {
            var verison = NetFrameworkVersionNo();
            lblFramework.Text = "Installed .NET Framework " + NetFrameworkVersionName(verison);
            _canInstallOrUpdate = verison >= 528040;

            if (_canInstallOrUpdate == false)
            {
                MessageBox.Show("Install .NET Framework 4.8 or later!");
                Application.Exit();
            }
        }

        private void DetectSqlLocalDB()
        {
            var version = SqlLocalDbVersionNo();

            if (version == 0)
            {
                MessageBox.Show("Install Sql Local DB 2016 or later!");
                Application.Exit();
            }

            lblSqlLocalDb.Text = "Installed SQL Local DB " + SqlLocalDbVersionName(version);
        }

        private void DetectProduct()
        {
            btnInstall.Enabled = false;
            btnUpdate.Enabled = false;
            lblVersion.Text = "Not detected";

            if (Directory.Exists("C:\\Program Files (x86)"))
                _programFilesDir = "C:\\Program Files (x86)";
            else
                _programFilesDir = "C:\\Program Files";

            _productDir = Path.Combine(_programFilesDir, _company + "\\" + _productName);

            _productExePath = Path.Combine(_productDir, _productExe);
            if (File.Exists(_productExePath))
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(_productExePath);
                _version = new Version(fileVersion.FileVersion);
                lblVersion.Text = "Installed " + _productName + " " + _version.ToString();

                btnUpdate.Enabled = true;
            }
            else
                btnInstall.Enabled = true;
        }

        private int NetFrameworkVersionNo()
        {
            try
            {
                const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey))
                {
                    return (int)key.GetValue("Release");
                }
            }
            catch
            {

            }

            return 0;
        }

        private int SqlLocalDbVersionNo()
        {
            int maxVerison = 0;
            try
            {
                float version = 0;
                const string subkey = @"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\";

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey))
                {
                    var names = key.GetSubKeyNames();

                    foreach (var name in names)
                    {
                        if (float.TryParse(name, out version))
                        {
                            if (maxVerison < version)
                                maxVerison = (int)version;
                        }
                    }
                }
            }
            catch
            {

            }
            return maxVerison;
        }

        private string NetFrameworkVersionName(int releaseKey)
        {
            if (releaseKey >= 533320)
                return "4.8.1";
            if (releaseKey >= 528040)
                return "4.8";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            return "";
        }

        private string SqlLocalDbVersionName(int version)
        {
            if (version == 13)
                return "Microsoft SQL Server 2016";
            if (version == 15)
                return "Microsoft SQL Server 2019";
            return "";
        }

        private void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void EnsureShortcutExists()
        {
            var target = Path.Combine(_productDir, _productExe);

            IShellLink link = (IShellLink)new ShellLink();

            // setup shortcut information
            link.SetDescription(_productName);
            link.SetPath(target);

            // save it
            var file = (IPersistFile)link;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            file.Save(Path.Combine(desktopPath, _productName + ".lnk"), false);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            btnInstall.Enabled = false;
            try
            {
                EnsureDirectoryExists(Path.Combine(_programFilesDir, _company));
                EnsureDirectoryExists(_productDir);
                EnsureShortcutExists();
                DoUpdate(true);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Morix Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btnInstall.Enabled = true;
            Cursor = Cursors.Default;
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            btnUpdate.Enabled = false;

            try
            {
                EnsureShortcutExists();
                DoUpdate(false);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Morix Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btnUpdate.Enabled = true;
            Cursor = Cursors.Default;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void DoUpdate(bool install)
        {
            var file = Path.Combine(_productDir, "Morix.Updater.exe");
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile("https://updater.morix.al/Morix.Updater.exe", file);
                }
            }
            catch(Exception ex)
            {
              
            }

            if (File.Exists(file))
            {
                if (install)
                {
                    System.Diagnostics.Process.Start(file,
                          "\"" + _productName + "\" \"" + _productExePath + "\"");
                }
                else
                {
                    System.Diagnostics.Process.Start(file,
                         "\"" + _productName + "\" \"" + _productExePath + "\" \"" + _version.ToString() + "\"");
                }
            }
        }

        private void CbxProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedProduct = this.cbxProduct.SelectedIndex;

            if (_selectedProduct == 0)
            {
                this.btnInstall.Enabled = false;
                this.btnUpdate.Enabled = false;
            }
            else if (_selectedProduct == 1)
            {
                _company = "Morix";
                _productName = "Accounts Client";
                _productExe = "AccountsClient.exe";
                DetectProduct();
            }
            else if (_selectedProduct == 2)
            {
                _company = "Morix";
                _productName = "Morix POS";
                _productExe = "MorixPOS.exe";
                DetectProduct();
            }
            else if (_selectedProduct == 3)
            {
                _company = "Morix";
                _productName = "Morix Exchange";
                _productExe = "MorixExchange.exe";
                DetectProduct();
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
    }
}