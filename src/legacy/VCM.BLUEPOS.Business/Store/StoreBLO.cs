using VCM.BLUEPOS.Data.Store;
using VCM.BLUEPOS.Model.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Common;

namespace VCM.BLUEPOS.Store
{
    public interface IStoreBLO
    {
        List<StoreModel> ListStore(string appCode);
        List<StorePartnerModel> ListStorePartner();
        List<IPAddressModel> LoadTerminalByStoreNo(string storeNo);
    }
    public class StoreBLO : IStoreBLO
    {
        private StoreData data { get; set; }
        public StoreBLO()
        {
            data = new StoreData();
        }

        public List<StoreModel> ListStore(string appCode)
        {
            return data.ListStore(appCode);
        }

        public List<StorePartnerModel> ListStorePartner()
        {
            return data.ListStorePartner();
        }

        public List<IPAddressModel> LoadTerminalByStoreNo(string storeNo)
        {
            return data.LoadTerminalByStoreNo(storeNo);
        }
    }
}
