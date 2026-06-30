using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignStoreFilterModel: BaseFilterModel
    {
        public List<string> SeletedList { get; set; }
        public List<string> UnSeletedList { get; set; }
        public bool IsCheckAll { get; set; }
        public bool IsLoadMore { get; set; }
    }

    public class PagingComboxOptionModel
    {
        public int Total { get; set; }
        public int PageIndex { get; set; }
        public int Limit { get; set; }
        public string SearchText { get; set; }
        public bool IsNextPage { get; set; }
        public List<ComboxOptionModel> SelectedOptions { get; set; }
        public List<string> UnSelectedOptions { get; set; }
        public List<ComboxOptionModel> ComboxOptions { get; set; }
        public string Message { get; set; }
    }

    public class ComboxOptionModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public string Permission { get; set; }

    }
}
