using System;
using System.Collections.Generic;

namespace TCX.API.Common.Dtos.Loyalty.ProgramPoints
{
    public class ProgramPointsDto
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string ProgramType { get; set; }
        public string CumulativeType { get; set; }
        public string ClubCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<ProgramPointsItemDto> Items { get; set; }
        public List<ProgramPointsStoreDto> Stores { get; set; }

    }
    public class ProgramPointsItemDto
    {
        public int ProgramId { get; set; }
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal LimitedQty { get; set; }
    }
    public class ProgramPointsStoreDto
    {
        public int ProgramId { get; set; }
        public string StoreNo { get; set; }
        public int LimitedQty { get; set; }
    }
}
