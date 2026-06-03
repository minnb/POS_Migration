using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace POS.Infrastructure.Helpers;

public static class RsaSignatureHelper
{
    /// <summary>
    /// Signs data using RSA SHA256. privateKeyXml is the XML RSA private key content string.
    /// </summary>
    public static string Sign(string data, string privateKeyXml, bool useAscii = false)
    {
        var parameters = ParseXmlKey(privateKeyXml);
        using var rsa = new RSACryptoServiceProvider(2048);
        try
        {
            rsa.ImportParameters(parameters);
            var bytes = useAscii
                ? Encoding.ASCII.GetBytes(data)
                : Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(rsa.SignData(bytes, CryptoConfig.MapNameToOID("SHA256")!));
        }
        finally
        {
            rsa.PersistKeyInCsp = false;
        }
    }

    private static RSAParameters ParseXmlKey(string xmlContent)
    {
        var parameters = new RSAParameters();
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);
        if (!xmlDoc.DocumentElement!.Name.Equals("RSAKeyValue"))
            throw new InvalidOperationException("Invalid XML RSA key.");

        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
        {
            switch (node.Name)
            {
                case "Modulus": parameters.Modulus = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "Exponent": parameters.Exponent = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "P": parameters.P = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "Q": parameters.Q = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "DP": parameters.DP = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "DQ": parameters.DQ = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "InverseQ": parameters.InverseQ = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
                case "D": parameters.D = string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText); break;
            }
        }
        return parameters;
    }
}
