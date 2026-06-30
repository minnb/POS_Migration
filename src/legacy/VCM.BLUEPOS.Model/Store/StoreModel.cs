using Newtonsoft.Json;

namespace VCM.BLUEPOS.Model.Store
{
    public class StoreModel
    {
        public int Id { get; set; }
        public string AppCode { get; set; }
        public string StoreNo { get; set; }
        public string ObjectID { get; set; }
        public string StoreName { get; set; }
        public string ServerIP { get; set; }
        public bool Blocked { get; set; }
        public string ValueJson
        {
            get
            {
                return JsonConvert.SerializeObject(new ParamJsonStoreList { SiteNo = StoreNo, SiteName = StoreName });
            }
        }
        public string DisplayStore
        {
            get
            {
                return $"{StoreNo}:{StoreName}";
            }
        }
    }
    public class ParamJsonStoreList
    {
        public string SiteNo { get; set; }
        public string SiteName { get; set; }
    }

    
}
