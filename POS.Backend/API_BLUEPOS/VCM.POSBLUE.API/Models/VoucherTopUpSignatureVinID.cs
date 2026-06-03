using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Xml;

namespace VCM.POSBLUE.API.Models
{
    public class VoucherTopUpSignatureVinID
    {
        public static RSAParameters CreatePriFromXmlFile(string xmlFilePath)
        {
            //vao trang https://superdry.apphb.com/tools/online-rsa-key-converter de doi PEM to xml
            RSAParameters parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
            var readD = File.ReadAllText(xmlFilePath);
            xmlDoc.LoadXml(File.ReadAllText(xmlFilePath));
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Exponent": parameters.Exponent = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "P": parameters.P = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Q": parameters.Q = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DP": parameters.DP = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DQ": parameters.DQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "InverseQ": parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "D": parameters.D = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            return parameters;
        }
        public static string SignatureVinIDVoucherTopUp(string rawData, bool isProd = true)
        {
            string success = "";
            var privateKeyFile = "";
            if (isProd)
                privateKeyFile = Path.Combine(HostingEnvironment.MapPath("~/EncryptDescript/"), "winmart_voucher_topup_publickey_prod");
            else //UAT
                privateKeyFile = Path.Combine(HostingEnvironment.MapPath("~/EncryptDescript/"), "winmart_voucher_topup_publickey");
            RSAParameters rsaParameters = CreatePriFromXmlFile(privateKeyFile);
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    rsa.ImportParameters(rsaParameters);
                    success = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(rawData), CryptoConfig.MapNameToOID("SHA256")));
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
            return success;
        }
    }
}