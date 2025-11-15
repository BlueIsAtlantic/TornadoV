using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TornadoScript.ScriptMain.CrashHandling;

namespace TornadoScript.ScriptCore.IO
{
    public class EncryptedFileStream
    {
        private readonly FileStream stream;
        private readonly string DataHash = "dkfcn7tz";
        private readonly string Salt = "Delta0xa44";
        private readonly string VIKey = "@pQsQDF6vpfJA84A";

        public EncryptedFileStream(string filePath)
        {
            try
            {
                stream = new FileStream(filePath, FileMode.OpenOrCreate);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"Failed to open or create file: {filePath}");
            }
        }

        public async Task WriteValueAsync(string key, int value)
        {
            try
            {
                string str = Encrypt($"{key}-{value}");
                int seekPos = 0;
                byte[] buffer = new byte[24];

                while (seekPos < stream.Length)
                {
                    stream.Seek(seekPos, SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, 24);
                    var line = Decipher(Encoding.ASCII.GetString(buffer));
                    var keyVal = line.Substring(0, line.IndexOf('-'));

                    if (keyVal == key)
                    {
                        using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII, 24, true))
                        {
                            writer.BaseStream.Seek(seekPos, SeekOrigin.Begin);
                            writer.BaseStream.Write(Encoding.ASCII.GetBytes(str), 0, 24);
                        }
                        return;
                    }

                    seekPos += 24;
                }

                if (stream.CanWrite)
                    stream.Write(Encoding.ASCII.GetBytes(str), (int)stream.Length, 24);
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"WriteValueAsync failed for key '{key}'");
            }
        }

        public async Task<int> ReadValueAsync(string key)
        {
            try
            {
                int seekPos = 0;
                byte[] buffer = new byte[24];

                while (seekPos < stream.Length)
                {
                    stream.Seek(seekPos, SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, 24);
                    var line = Decipher(Encoding.ASCII.GetString(buffer));
                    var keyVal = line.Substring(0, line.IndexOf('-'));
                    var value = line.Substring(line.IndexOf('-') + 1);

                    if (keyVal == key)
                        return Convert.ToInt32(value);

                    seekPos += 24;
                }

                return 0;
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, $"ReadValueAsync failed for key '{key}'");
                return 0;
            }
        }

        private string Encrypt(string plainText)
        {
            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] keyBytes = new Rfc2898DeriveBytes(DataHash, Encoding.ASCII.GetBytes(Salt)).GetBytes(256 / 8);
                var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
                var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

                using var memoryStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                }

                return Convert.ToBase64String(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "Encrypt failed");
                return string.Empty;
            }
        }

        private string Decipher(string encryptedText)
        {
            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
                byte[] keyBytes = new Rfc2898DeriveBytes(DataHash, Encoding.ASCII.GetBytes(Salt)).GetBytes(256 / 8);
                var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None };
                var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

                using var memoryStream = new MemoryStream(cipherTextBytes);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd('\0');
            }
            catch (Exception ex)
            {
                CrashLogger.LogError(ex, "Decipher failed");
                return string.Empty;
            }
        }
    }
}
