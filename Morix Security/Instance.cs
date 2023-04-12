using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Morix.Security
{
    public class Instance
    {
        private static readonly string _salt;
        private static readonly byte[] _aesKey;
        private static readonly byte[] _aesSalt;

        static Instance()
        {
            _salt = "morix";
            _aesKey = Encoding.UTF8.GetBytes("voKBDhsmuoupw4JU6XlR9H1ktsF2Cdzo");
            _aesSalt = new byte[] { 30, 21, 50, 230, 5, 70, 1, 99 };
        }

        public static string Hash(string data)
        {
            try
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data + _salt));

                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                        builder.Append(bytes[i].ToString("X2"));

                    return builder.ToString();
                }
            }
            catch
            { }

            return string.Empty;
        }

        public static string Encrypt(string plainText)
        {
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(plainText);
            byte[] bytesEncrypted;
            using (var ms = new MemoryStream())
            {
                using (var AES = Aes.Create("AesManaged"))
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(_aesKey, _aesSalt, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    bytesEncrypted = ms.ToArray();
                }
            }
            return Convert.ToBase64String(bytesEncrypted);
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] bytesToBeDecrypted = Convert.FromBase64String(encryptedText);

            byte[] bytesDecrypted;
            using (var ms = new MemoryStream())
            {
                using (var AES = Aes.Create("AesManaged"))
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(_aesKey, _aesSalt, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    bytesDecrypted = ms.ToArray();
                }
            }
            return Encoding.UTF8.GetString(bytesDecrypted);
        }
    }
}
