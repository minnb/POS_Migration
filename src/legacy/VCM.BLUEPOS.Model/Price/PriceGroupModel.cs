using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.PriceGroup
{
    public class CreateStorePriceGroupModel
    {
        public int ID { get; set; }
        public string Store { get; set; }             
        public string PriceGroupCode { get; set; } 
        public int Type { get; set; }
        public int Priority { get; set; } 
        public int ReplicationCounter { get; set; } 
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class CreateStorePriceGroupResponseModel
    {
        public int ID { get; set; }
        public string Store { get; set; }
        public string PriceGroupCode { get; set; }
        public int Type { get; set; }
        public int Priority { get; set; }
        public int ReplicationCounter { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class PriceGroupListResponseModel
    {
        public string PriceGroupCode { get; set; }
        public string PriceGroupName { get; set; }
        public int Priority { get; set; }
        public string EnabledStr { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }

    public class CreatePriceGroupModel
    {
        public string PriceGroupCode { get; set; }
        public string PriceGroupName { get; set; }
        public int Priority { get; set; }
        public string EnabledStr { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }
    public class AddStoreModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
    }
    public class AddStorePriceGroupModel
    {
        public List<AddStoreModel> Store { get; set; }
        public string PriceGroupCode { get; set; }
        public int Type { get; set; }
        public int? Priority { get; set; }
        public int ReplicationCounter { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public bool? IsSelectAll { get; set; }
    }

}
