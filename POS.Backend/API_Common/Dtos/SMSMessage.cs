using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos
{
    public class SMSMessage
    {
        public string AppCode { get; set; }
        public string MessageType { get; set; } //Error, Info, Warning
        public string Subject { get; set; } //tiêu đề
        public string Content { get; set; } // nội dung lỗi
        public string Text { get; set; }
    }
    public class SMSMessageV1
    {
        public string ChanelId { get; set; }
        public string MessageType { get; set; }
        public string Subject { get; set; }
        public string Conntent { get; set; }
    }
    public class SMSMessageV2
    {
        public string AppCode { get; set; }
        public string MessageType { get; set; } = "Info"; // Default to Info
        public string Subject { get; set; }
        public string Conntent { get; set; }
    }
}