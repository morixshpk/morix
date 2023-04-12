using System;
using System.IO;
using System.Web;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

public class ServiceDotMorix : IHttpHandler
{
    public bool IsReusable { get { return false; } }

    public void ProcessRequest(HttpContext context)
    {
        try
        {
            var file = context.Request.Headers["morix"];
            if (!string.IsNullOrEmpty(file))
            {
                file = DecryptAes(file).ToLower();

                string filePath = HttpRuntime.AppDomainAppPath + "App_Data/" + file;

                if (File.Exists(filePath))
                {
                    if (file.EndsWith(".xml"))
                    {
                        Logs(file);
                        context.Response.ContentType = "application/xml";
                        string xml = File.ReadAllText(filePath);
                        string md5Value = string.Empty;
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(filePath.Replace(".xml", ".zip")))
                            {
                                md5Value = Convert.ToBase64String(md5.ComputeHash(stream));
                            }
                        }
                        context.Response.Write(EncryptAes(xml.Replace("_md5_", md5Value)));
                    }
                    else if (file.EndsWith(".zip"))
                    {
                        Logs(file);
                        context.Response.ContentType = "application/zip";
                        context.Response.WriteFile(filePath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex.ToString());
        }
    }

    [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
    public static string EncryptAes(string s)
    {
        string result = string.Empty;
        try
        {
            var key = Encoding.UTF8.GetBytes("d899d2ds3bbaf0da");
            var iv = Encoding.UTF8.GetBytes("94e7f58aaef6c2f2");
            var rijn = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros
            };
            using (var msEncrypt = new MemoryStream())
            {
                using (var encryptor = rijn.CreateEncryptor(key, iv))
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
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

    [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
    public static string DecryptAes(string s)
    {
        string result = string.Empty;
        try
        {
            var key = Encoding.UTF8.GetBytes("d899d2ds3bbaf0da");
            var iv = Encoding.UTF8.GetBytes("94e7f58aaef6c2f2");
            var rijn = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros
            };
            using (var msDecrypt = new MemoryStream(StringToByteArray(s)))
            {
                using (var decryptor = rijn.CreateDecryptor(key, iv))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var swDecrypt = new StreamReader(csDecrypt))
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

    private void Logs(string file)
    {
        string clientIP = string.Empty;
        if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            clientIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
        else if (HttpContext.Current.Request.UserHostAddress.Length != 0)
            clientIP = HttpContext.Current.Request.UserHostAddress;

        var path = HttpRuntime.AppDomainAppPath + "App_Data\\Logs";

        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);

        string content = "Request for update: " + file + ", IP: " + clientIP;

        Log.Debug(content);
    }
}