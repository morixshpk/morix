using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Morix
{
    public static class Program
    {
        internal static string TempFolder;

        private static string _serverUrl = string.Empty;
        private static RemoteFile _remoteFile = null;
        private static string _name = string.Empty;
        private static string _appId = string.Empty;
        private static string _executableFile = string.Empty;
        private static Version _versionLocal = null;
        private static string _baseDirectory = string.Empty;

        public static string Name
        {
            get { return _name; }
        }

        public static string ServerUrl
        {
            get { return _serverUrl; }
        }

        public static string BaseDirectory
        {
            get { return _baseDirectory; }
        }

        static void Main(string[] args)
        {
            Log("Morix Updater executed", true);

            _serverUrl = "https://updater.morix.al/Updater.morix";

            EnsureDirectoryExists("C:\\ProgramData\\Morix");
            EnsureDirectoryExists("C:\\ProgramData\\Morix\\MorixUpdater");

            TempFolder = ("C:\\ProgramData\\Morix\\MorixUpdater\\Temp");

            EnsureDirectoryExists(TempFolder);
#if DEBUG
            args = new string[2];
            args[0] = "Morix POS";
            args[1] = "C:\\Program Files (x86)\\Morix\\Morix POS\\MorixPOS.exe";
#endif

            if (args.Length >= 2)
            {
                Log("Arguments: " + string.Join(",", args));
                _name = args[0];
                _executableFile = args[1];

                _appId = Path.GetFileName(_executableFile).ToLower();

                bool install = true;
                if (File.Exists(_executableFile))
                {
                    install = false;

                    var fileVersion = FileVersionInfo.GetVersionInfo(_executableFile);
                    _versionLocal = new Version(fileVersion.FileVersion);
                }
                DoUpdate(install);
            }
            else
            {
                // check for pos
                _executableFile = Path.Combine(AssemblyDirectoryGet(), @"AccountsClient.exe");
                SoftwareCheck("Accounts Client");

                // check for pos
                _executableFile = Path.Combine(AssemblyDirectoryGet(), @"MorixPOS.exe");
                SoftwareCheck("Morix POS");

                // check for betsoft
                _executableFile = Path.Combine(AssemblyDirectoryGet(), @"MorixBetSoft.exe");
                SoftwareCheck("Morix Bet Soft");

                // check for betserver
                _executableFile = Path.Combine(AssemblyDirectoryGet(), @"MorixBetServer.exe");
                SoftwareCheck("Morix Bet Server");
            }
        }

        static void SoftwareCheck(string name)
        {
            if (File.Exists(_executableFile))
            {
                try
                {
                    _name = name;
                    _appId = Path.GetFileName(_executableFile).ToLower();
                    _baseDirectory = Path.GetDirectoryName(_executableFile);
                    _versionLocal = AssemblyName.GetAssemblyName(_executableFile).Version;
                    DoUpdate(false);
                }
                catch
                {

                }
            }
        }

        private static void DoUpdate(bool install)
        {
            _baseDirectory = Path.GetDirectoryName(_executableFile);

            var fs = new FrmStatus();
            try
            {
                Log("Administrator privilege check");
                if (!IsAdmin())
                {
                    MessageBox.Show("You must have Administrator privilege to complete this action!", _name, MessageBoxButtons.OK);
                    throw new MorixException("Administrator Privilage required!");
                }
                Log("Administrator privilege passed");


                Log("Remote info check");
                _remoteFile = ParseRemoteFileXml();
                if (_remoteFile == null)
                    throw new MorixException("Download remote info file faild");
                Log("Remote info downloaded");

                if (install)
                {
                }
                else
                {
                    Log("New version check");
                    var remoteVerion = new Version(_remoteFile.Version);

                    Log("Local version: " + _versionLocal.ToString());
                    Log("Remote version: " + remoteVerion.ToString());

                    if (remoteVerion.CompareTo(_versionLocal) <= 0)
                    {
                        MessageBox.Show("Your software is up to date.", Program.Name);
                        throw new MorixException("Software is up to date.");
                    }

                    Log("New version found, confirm to install");
                    var fc = new FrmConfirm();
                    if (DialogResult.OK != fc.ShowDialog())
                        throw new MorixException("Confirmation skiped");
                    Log("New version confirmed");
                }

                Log("Start download");
                var fd = new FrmDownload(_remoteFile);
                if (fd.ShowDialog() != DialogResult.OK)
                    throw new MorixException("Download new version failed!");
                Log("New version downloaded");

                fs.Show();
                fs.SetStatus("Installation in progress..");

                var tempZipFile = Path.Combine(TempFolder, _remoteFile.Name);

                Log("Check donwloaded file");
                if (!File.Exists(tempZipFile))
                    throw new MorixException("Downloaded file not found!");
                Log("Downloaded file found");

                Log("Extracting files...");
                fs.SetStatus("Extracting files...");
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZipFile, TempFolder);
                File.Delete(tempZipFile);
                Log("New file extracted");


                Log("Installing new version...");
                fs.SetStatus("Installing new version...");
                
                var file = BaseDirectory + "\\Morix.Updater.exe";
                try
                {
                    if (File.Exists(file))
                    {
                        var fileMorix = BaseDirectory + "\\Morix.Updater.exe.morix";
                        if (File.Exists(fileMorix))
                            File.Delete(fileMorix);

                        File.Move(file, fileMorix);
                    }

                    var processes = Process.GetProcessesByName(_appId.Replace(".exe", ""));

                    while (processes.Length > 0)
                    {
                        if (processes.Length > 0)
                        {
                            foreach (var process in processes)
                                process.Kill();
                        }
                        System.Threading.Thread.Sleep(500);
                        processes = Process.GetProcessesByName(_appId.Replace(".exe", ""));
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                }

                Log("Database check");
                DatabaseCheck(TempFolder, BaseDirectory);
                Log("Databse finished");

                DirectoryMove(TempFolder, BaseDirectory);

                Log("Database give permissions");
                DatabaseGivePermissions(BaseDirectory);
                Log("Database give permissions done");


                Directory.Delete(TempFolder, true);
                fs.SetStatus("Restarting the application");

                file = BaseDirectory + "\\Morix.Updater.exe";
                if (!File.Exists(file))
                {
                    var fileMorix = BaseDirectory + "\\Morix.Updater.exe.morix";
                    if (File.Exists(fileMorix))
                        File.Move(fileMorix, file);
                }

                Log("Restarting the application");
                Program.Restart();
            }
            catch (MorixException)
            {

            }
            catch (Exception ex)
            {
                Log("Update failed!");
                Log(ex);
                fs.SetStatus("Update failed!");
                fs.Close();
            }
            finally
            {
                fs.Close();
            }
        }

        private static void Restart()
        {
            Process.Start(_executableFile);
            Environment.Exit(0);
        }

        private static RemoteFile ParseRemoteFileXml()
        {
            RemoteFile remoteFile = null;
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("morix", Program.EncryptAes(_appId.Replace(".exe", "") + ".xml"));
                using (var response = httpClient.GetAsync(_serverUrl).Result)
                {
                    using (var content = response.Content)
                    {
                        var xml = content.ReadAsStringAsync().Result;
                        xml = DecryptAes(xml);
                        var document = new XmlDocument();
                        document.LoadXml(xml);
                        remoteFile = new RemoteFile(document.DocumentElement.FirstChild);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            return remoteFile;
        }

        private static bool DirectoryMove(string sourceDir, string targetDir)
        {
            try
            {
                foreach(var source in Directory.GetDirectories(sourceDir))
                {
                    string folder = Path.GetFileName(source);
                    var target = Path.Combine(targetDir, folder);
                    EnsureDirectoryExists(target);

                    FileMove(source, target);
                }
                FileMove(sourceDir, targetDir);
                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            return false;
        }

        private static bool FileMove(string sourceDir, string targetDir)
        {
            try
            {
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var fileName = Path.GetFileName(file);  
                    var targetFile = Path.Combine(targetDir, fileName);
                    File.Copy(file, targetFile, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            return false;
        }

        private static bool DatabaseCheck(string sourcePath, string targetPath)
        {
            try
            {
                // if program contains mdf or ldf
                bool containsMdfOrLdf = false;
                foreach (var file in Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".mdf") || file.EndsWith(".ldf"))
                    {
                        containsMdfOrLdf = true;
                    }
                }

                if (containsMdfOrLdf)
                {
                    // remove the data from downloaded
                    foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                    {
                        if (file.EndsWith(".mdf") || file.EndsWith(".ldf"))
                        {
                            File.Delete(file);
                            Log("Deleted " + file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            return false;
        }

        private static void DatabaseGivePermissions(string directory)
        {
            // give admin privilege to mdf and ldf
            foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".mdf") || file.EndsWith(".ldf"))
                {
                    var fileInfo = new FileInfo(file);
                    var accessControl = fileInfo.GetAccessControl();
                    accessControl.AddAccessRule(new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, AccessControlType.Allow));
                    accessControl.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
                    fileInfo.SetAccessControl(accessControl);
                }
            }
        }

        public static string EncryptAes(string s)
        {
            string result = string.Empty;
            try
            {
                var key = Encoding.UTF8.GetBytes("d899d2ds3bbaf0da");
                var iv = Encoding.UTF8.GetBytes("94e7f58aaef6c2f2");
                RijndaelManaged rijn = new RijndaelManaged
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.Zeros
                };
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (ICryptoTransform encryptor = rijn.CreateEncryptor(key, iv))
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(s);
                            }
                        }
                    }
                    result = BitConverter.ToString(msEncrypt.ToArray()).Replace("-", string.Empty).ToLower();
                }
                rijn.Clear();
            }
            catch
            {
                result = string.Empty;
            }
            return result;
        }

        public static string DecryptAes(string s)
        {
            string result = string.Empty;
            try
            {
                var key = Encoding.UTF8.GetBytes("d899d2ds3bbaf0da");
                var iv = Encoding.UTF8.GetBytes("94e7f58aaef6c2f2");
                RijndaelManaged rijn = new RijndaelManaged
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.Zeros
                };
                using (MemoryStream msDecrypt = new MemoryStream(StringToByteArray(s)))
                {
                    using (ICryptoTransform decryptor = rijn.CreateDecryptor(key, iv))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader swDecrypt = new StreamReader(csDecrypt))
                            {
                                result = swDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                rijn.Clear();

            }
            catch
            {
                result = string.Empty;
            }
            return result.Replace("\0", string.Empty);
        }

        public static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string AssemblyDirectoryGet()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        private static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        private static void Log(string content, bool clearContent = false)
        {
            try
            {
                string dir = AssemblyDirectoryGet() + "\\Logs";
                EnsureDirectoryExists(dir);

                var file = Path.Combine(dir, "MorixUpdater.txt");

                if (clearContent)
                    System.IO.File.WriteAllText(file, string.Empty);

                var sw = new StreamWriter(file, true);
                sw.WriteLine("{0:yyyy-MM-dd HH:mm:ss} => {1}", DateTime.Now, content);
                sw.Flush();
                sw.Close();
            }
            catch (Exception)
            {

            }
        }

        //private static void RollBack()
        //{
        //    string sourcePath = BaseDirectory + BackupFolder;
        //    string targetPath = BaseDirectory;

        //    try
        //    {
        //        if (Directory.Exists(sourcePath))
        //        {
        //            if (MoveFiles(sourcePath, targetPath))
        //                Directory.Delete(sourcePath, true);
        //        }

        //        sourcePath = BaseDirectory + TempFolder;
        //        if (Directory.Exists(sourcePath))
        //            Directory.Delete(sourcePath, true);
        //    }
        //    catch (Exception)
        //    {
        //        //log the error message,you can use the application's log code
        //    }
        //}
    }
}