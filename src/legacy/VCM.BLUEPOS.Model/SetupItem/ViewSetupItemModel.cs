using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class ViewSetupItemModel
    {
        public int TotalRow { get; set; }
        public int TotalPageNumber { get; set; }
        public int PageSize { get; set; }
        public string Keyword { get; set; }
        public bool IsButtonAdd { get; set; } 
        public List<ItemProcedureModel> ListItem { get; set; }
    }


    // San pham ban kenh doi tac
    public class ViewSetupItemPartnerModel
    {
        public int TotalRow { get; set; }
        public int TotalPageNumber { get; set; }
        public int PageSize { get; set; }
        public string SalesType { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
        public string Keyword { get; set; }
        public string MCH2 { get; set; }            // Mã nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang
        public List<ItemPartnerResponseModel> ListItem { get; set; }
        public string CupType { get; set; }

    }




}
