using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VCM.POSBLUE.Model.POS.Gift
{
    public class GiftDataRequest : POSRequest
    {
        [Required]
        public string[] GiftCode { get; set; }
        [Required]
        public string Status { get; set; }
        public string GiftType { get; set; }
    }

    public class GiftDataResponse
    {
        public List<GiftDataRespone> GiftData { get; set; }
    }

    public class GiftDataRespone
    {
        public string GiftCode { get; set; }
        public string GiftStatus { get; set; }
        public string PosUsed { get; set; }
        public DateTime TimeUsed { get; set; }
    }
}
