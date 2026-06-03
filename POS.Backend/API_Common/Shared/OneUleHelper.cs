using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml;
using VCM.POSBLUE.Model.VINID;

namespace TCX.API.Common.Helpers
{
    public static class OneUleHelper
    {
        public static RSAParameters CreatePriFromXmlFile(string xmlFilePath)
        {
            RSAParameters parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlFilePath);
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
        public static string SignatureOneU(string rawData, string privateKeyFile)
        {
            string signature = "";
            RSAParameters rsaParameters = CreatePriFromXmlFile(privateKeyFile);
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    rsa.ImportParameters(rsaParameters);
                    //signature = Convert.ToBase64String(rsa.SignData(Encoding.ASCII.GetBytes(rawData), CryptoConfig.MapNameToOID("SHA256")));
                    return System.Convert.ToBase64String(rsa.SignData(Encoding.ASCII.GetBytes(rawData), CryptoConfig.MapNameToOID("SHA256")));
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
            return signature;
        }
    }
}
