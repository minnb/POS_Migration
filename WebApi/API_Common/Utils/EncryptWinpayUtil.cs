using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;

namespace TCX.API.Common.Utils
{
    public class EncryptWinpayUtil
    {
        public string RSA_IV(byte[] iv, string publicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                byte[] encryptedIv = RSAHelper.EncryptIvWithRSA(iv, publicKey);
                string encryptedIvBase64 = Convert.ToBase64String(encryptedIv);
                return encryptedIvBase64;
            }
        }
    }
}
