using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace VCM.BLUEPOS.Helpers
{
    public class LogsFile
    {
        public static void WriteLogFile(string ipServer, string browserPublicKey, string functionName, string message)
        {
            try
            {
                var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var content = $"{now}       {functionName}                      {message}";
                //var path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
                var path = @"\\10.235.64.104\PLHWeb\Logs\";
                //var path = @"\\10.233.9.121\ftpbluepos\Logs\";
                var filepath = path + "LogWebSlow_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";

                if (!File.Exists(filepath))
                {
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(content);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(content);
                    }
                }

                if (Environment.UserInteractive)
                {
                    Console.WriteLine(content);
                }

            }
            catch (Exception ex)
            {

            }
        }
    }
}