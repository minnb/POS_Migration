using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Shared
{
    public static class EncryptionHelper
    {
        
        public static string Base64Decode(string plainText)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(plainText));
            }
            catch
            {
                return null;
            }
        }
        public static string Base64Encode(string plainText)
        {
            try
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return System.Convert.ToBase64String(plainTextBytes);
            }
            catch
            {
                return null;
            }
        }
        public static string Md5(string sInput)
        {
            ASCIIEncoding enCoder = new ASCIIEncoding();
            byte[] valueByteArr = enCoder.GetBytes(sInput);
            // Encrypt Input string 
            HashAlgorithm algorithmType = new MD5CryptoServiceProvider();
            byte[] hashArray = algorithmType.ComputeHash(valueByteArr);
            //Convert byte hash to HEX
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashArray)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
        public static string CreateTokenCapillary(string userName, string password)
        {
            string plainTextBytes = userName + ":" + Md5(password);
            return Base64Encode(plainTextBytes);
        }
        public static string Md5Encrypt(string sInput)
        {
            // Chuyển đổi chuỗi input thành mảng byte
            byte[] bytes = Encoding.UTF8.GetBytes(sInput);

            // Tạo đối tượng MD5
            using (MD5 md5 = MD5.Create())
            {
                // Mã hóa mảng byte
                byte[] hashBytes = md5.ComputeHash(bytes);

                // Chuyển đổi mảng byte sang chuỗi hexa
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // Giải mã một chuỗi MD5 thành chuỗi ban đầu
        public static bool VerifyMd5Hash(string input, string hash)
        {
            // Mã hóa lại chuỗi input và so sánh với chuỗi hash đầu vào
            string hashOfInput = Md5Encrypt(input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }
        public static string ComputeHMACSHA256(string message, string secretKey)
        {
            // Chuyển đổi dữ liệu đầu vào và khóa thành byte array
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // Tạo đối tượng HMAC-SHA256 với khóa bí mật
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                // Tính toán HMAC
                byte[] hashBytes = hmac.ComputeHash(messageBytes);

                // Chuyển đổi byte array kết quả thành chuỗi hex
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
