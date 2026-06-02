using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Shared
{
    public static class MessageHelper
    {
        public static string PhoneNumberNotFormat(string phoneNumber)
        {
            return string.Format("Số điện thoại {0} không đúng", phoneNumber);
        }
        public static string TemplateSMS(string subject, string content, string endMessage = "", bool isServer = true)
        {
            string text = subject;
            if(isServer)
            {
                text += $"_server: {ComputerHelper.GetIpServer()} at {DateTime.Now:yyyy-MM-dd HH:mm:sss}";
            }
            text += "_" + content; // + Environment.NewLine;
            text += "_" + endMessage;
            return text;
        }
    }
}
