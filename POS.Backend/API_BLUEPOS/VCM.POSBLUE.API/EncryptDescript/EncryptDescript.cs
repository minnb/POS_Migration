using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;

namespace VCM.POSBLUE.API.EncryptDescript
{
    public class EncryptDescript
    {
        public string SignMessage(string message, string privateKeyFile)
        {
            string signedMessage;
            try
            {
                RSACryptoServiceProvider p = new RSACryptoServiceProvider();
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
                RSAParameters ParamPrivateKey = CreatePriFromXmlFile(privateKeyFile);
                rsa.ImportParameters(ParamPrivateKey);
                signedMessage = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(message), CryptoConfig.MapNameToOID("SHA256")));
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("Request TransactionIssue Exception: " + ex.Message.ToString());
                signedMessage = string.Empty;
            }
            return signedMessage;
        }
        public RSAParameters CreatePriFromXmlFile(string xmlFilePath)
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

            // rsa.ImportParameters(parameters);
            return parameters;
        }
    }
}