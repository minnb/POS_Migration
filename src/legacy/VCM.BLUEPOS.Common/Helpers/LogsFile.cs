using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Common.Helpers
{
    public class LogsFile
    {
        public static void WriteLogFile(string functionName, string message)
        {
            try
            {
                var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var content = $"{now} {functionName}         {message}";
                var path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
                var filepath = path + functionName + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";

                if (!System.IO.File.Exists(filepath))
                {
                    using (StreamWriter sw = System.IO.File.CreateText(filepath))
                    {
                        sw.WriteLine(content);
                    }
                }
                else
                {
                    using (StreamWriter sw = System.IO.File.AppendText(filepath))
                    {
                        sw.WriteLine(content);
                    }
                }

                if (Environment.UserInteractive)
                {
                    Console.WriteLine(content);
                }
            }
            catch (Exception ex) { }
        }

        public static void WriteLogFileV2(string functionName, string message, string ipWriteLog)
        {
            if (ipWriteLog == "10.235.64.104" || ipWriteLog == "10.235.64.105")
            {
                var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
                var content = $"{now}       {functionName}                      {message}";
                //var path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";

                var path = @"\\10.235.64.104\PLHWeb\Logs\";
                //var path = @"\\10.233.9.121\ftpbluepos\Logs\";
                var filepath = path + functionName + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";
                try
                {

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
                    var content2 = $"{now}       WriteFileLog                      {JsonConvert.SerializeObject(ex)}";
                    var path2 = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
                    var filepath2 = path2 + functionName + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";
                    if (!File.Exists(filepath2))
                    {
                        using (StreamWriter sw = File.CreateText(filepath2))
                        {
                            sw.WriteLine(content2);
                        }
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(filepath2))
                        {
                            sw.WriteLine(content2);
                        }
                    }

                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine(content2);
                    }
                }
            }
        }

    }
}
