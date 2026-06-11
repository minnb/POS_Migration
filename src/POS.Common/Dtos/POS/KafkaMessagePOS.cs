using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Common.Dtos.POS
{
    public class QueryResult
    {
        public int STATUS { get; set; } = 9;
    }
    public class KafkaMessagePOS
    {
        public string Type { get; set; } = string.Empty;
        public Object? Data { get; set; }
    }
}
