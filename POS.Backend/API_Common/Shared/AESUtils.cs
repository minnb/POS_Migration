using System;
using System.Security.Cryptography;
using System.Text;

public class AESUtils
{
    public static string EncryptWithIV(string input, string privateKey, byte[] iv)
    {
        byte[] key = Encoding.UTF8.GetBytes(privateKey);
        using (AesManaged cipher = new AesManaged())
        {
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv;

            using (ICryptoTransform encryptor = cipher.CreateEncryptor())
            {
                byte[] cipherText = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(input), 0, input.Length);
                return Convert.ToBase64String(cipherText);
            }
        }
    }
    public static string DecryptWithIV(string input, string privateKey, byte[] iv)
    {
        byte[] key = Encoding.UTF8.GetBytes(privateKey);
        byte[] cipherText = Convert.FromBase64String(input);

        using (AesManaged cipher = new AesManaged())
        {
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv;

            using (ICryptoTransform decryptor = cipher.CreateDecryptor())
            {
                byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                return Encoding.UTF8.GetString(plainText);
            }
        }
    }

    //public static string DecryptWithIV(string cipherText, string privateKey, byte[] iv)
    //{
    //    byte[] key = Encoding.UTF8.GetBytes(privateKey);
    //    using (AesManaged cipher = new AesManaged())
    //    {
    //        cipher.Mode = CipherMode.CBC;
    //        cipher.Padding = PaddingMode.PKCS7;
    //        cipher.Key = key;
    //        cipher.IV = iv;

    //        using (ICryptoTransform decryptor = cipher.CreateDecryptor())
    //        {
    //            byte[] plainText = decryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(cipherText), 0, cipherText.Length);
    //            return Convert.ToBase64String(plainText);
    //        }
    //    }
    //}

    public static string Encrypt(string input, string privateKey)
    {
        byte[] key = ConvertStringToSecretKey(privateKey);
        IvParameterSpec iv = GenerateIv();
        using (AesManaged cipher = new AesManaged())
        {
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv.GetIV();

            using (ICryptoTransform encryptor = cipher.CreateEncryptor())
            {
                byte[] cipherText = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(input), 0, input.Length);
                return Convert.ToBase64String(cipherText);
            }
        }
    }

    public static string Decrypt(string cipherText, string privateKey)
    {
        byte[] key = ConvertStringToSecretKey(privateKey);
        IvParameterSpec iv = GenerateIv();
        using (AesManaged cipher = new AesManaged())
        {
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv.GetIV();

            using (ICryptoTransform decryptor = cipher.CreateDecryptor())
            {
                byte[] plainText = decryptor.TransformFinalBlock(Convert.FromBase64String(cipherText), 0, cipherText.Length);
                return Encoding.UTF8.GetString(plainText);
            }
        }
    }

    public static IvParameterSpec GenerateIv()
    {
        byte[] iv = new byte[16];
        return new IvParameterSpec(iv);
    }

    public static byte[] ConvertStringToSecretKey(string encodedKey)
    {
        return Convert.FromBase64String(encodedKey);
    }
}
public class IvParameterSpec
{
    private byte[] iv;

    public IvParameterSpec(byte[] iv)
    {
        this.iv = iv ?? throw new ArgumentNullException(nameof(iv));
    }

    public byte[] GetIV()
    {
        return iv;
    }
}
