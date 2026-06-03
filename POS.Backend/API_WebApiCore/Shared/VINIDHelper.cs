using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;
using System.Xml;
using TCX.API.Common.Dtos;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;


public static class VINIDHelper
{
    public static string SignMessage(string message, string privateKeyFile)
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

        // rsa.ImportParameters(parameters);
        return parameters;
    }
    public static string VinPaySignature(string plainTextData)
    {
        //var pathSignature = Path.Combine(HostingEnvironment.MapPath("~/EncryptDescript/"), "winmart_xml_privatekey");
        var pathSignature = Path.Combine(HostingEnvironment.MapPath("~/EncryptDescript/"), "extral_xml_privatekey");
        var csp = new RSACryptoServiceProvider(2048);

        var privKey = csp.ExportParameters(true);
        var pubKey = csp.ExportParameters(false);

        csp = new RSACryptoServiceProvider();
        csp.ImportParameters(pubKey);
        var bytesPlainTextData = Encoding.Unicode.GetBytes(plainTextData);
        var bytesCypherText = csp.Encrypt(bytesPlainTextData, false);
        var cypherText = Convert.ToBase64String(bytesCypherText);
        bytesCypherText = Convert.FromBase64String(cypherText);
        csp = new RSACryptoServiceProvider();
        csp.ImportParameters(privKey);
        bytesPlainTextData = csp.Decrypt(bytesCypherText, false);
        string str = Encoding.Unicode.GetString(bytesPlainTextData);

        return SignMessage(str, pathSignature);
    }
    public static ResultResponse GetMessConfig(List<NotifyConfigDto> notifyConfigDtos, string statusCode, object data)
    {
        try
        {
            var vinidConfig = notifyConfigDtos.Where(x=>x.AppCode == "VINID" && x.Status == statusCode).FirstOrDefault();
            if(vinidConfig != null )
            {
                return new ResultResponse
                {
                    Data = data,
                    Message = vinidConfig.Message,
                    Status = EnumHelper.GetEnumValue<HttpStatusCode>(vinidConfig.ActionType) 
                };
            }
            else
            {
                return new ResultResponse
                {
                    Data = data,
                    Message = statusCode,
                    Status = HttpStatusCode.Conflict
                };
            }
        }
        catch 
        {
            return new ResultResponse
            {
                Data = data,
                Message = statusCode,
                Status = HttpStatusCode.Conflict
            };
        }
    }

}