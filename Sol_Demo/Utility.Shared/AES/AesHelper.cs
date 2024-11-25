using Models.Shared.Constant;
using System.Security.Cryptography;
using System.Text;

namespace Utility.Shared.AES;

public class AesHelper
{
    private readonly int _ivLength = ConstantValue.IvLength;
    private readonly byte[] _encryptionKey;

    public AesHelper(string secretKey)
    {
        _encryptionKey = Encoding.UTF8.GetBytes(secretKey);
    }

    public async Task<string> EncryptAsync(string data)
    {
        return await Task.Run(() =>
        {
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = _encryptionKey;
                    aesAlg.GenerateIV();
                    byte[] iv = new byte[_ivLength];
                    Array.Copy(aesAlg.IV, iv, _ivLength);

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, iv);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(data);
                            }
                            byte[] encrypted = msEncrypt.ToArray();
                            return $"{BitConverter.ToString(iv).Replace("-", "")}:{BitConverter.ToString(encrypted).Replace("-", "")}";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Encryption failed.", e);
            }
        });
    }

    public async Task<string> DecryptAsync(string data)
    {
        return await Task.Run(() =>
        {
            try
            {
                string[] textParts = data.Split(':');
                byte[] iv = StringToByteArray(textParts[0]);
                byte[] encryptedText = StringToByteArray(textParts[1]);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = _encryptionKey;
                    aesAlg.IV = iv;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(encryptedText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Decryption failed.", e);
            }
        });
    }

    private static byte[] StringToByteArray(string hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}