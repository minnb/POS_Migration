using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.SyncData;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.SyncData;
using VCM.BLUEPOS.Model.OptionModel;


namespace VCM.BLUEPOS.Business.SyncData
{
    public interface ISyncDataBLO
    {
        List<SAPLogModel> ListLogSAP(string processID);
        List<SalePosMisModel> ListOrderCentral(string storeNo, string posID, DateTime bussinessDate);
        bool UpdateEOD(POSEODModel model);

        // 21/11/2024, tungnt8
        List<IPAddressPOSTerminalByStoreModel> GetPOSTerminalByStore(string storeNo);
        List<MissSalePosModel> ListOrderCentralV2(string storeNo, string posID, DateTime bussinessDate);


    }

    public class SyncDataBLO : ISyncDataBLO
    {
        private SyncIData _data { get; set; }
        public SyncDataBLO()
        {
            _data = new SyncIData();
        }
        public List<SAPLogModel> ListLogSAP(string processID)
        {
            return _data.ListLogSAP(processID);
        }
        public List<SalePosMisModel> ListOrderCentral(string storeNo, string posID, DateTime bussinessDate)
        {
            return _data.ListOrderCentral(storeNo, posID, bussinessDate);
        }
        public bool UpdateEOD(POSEODModel model)
        {
            return _data.UpdateEOD(model);
        }
        public List<IPAddressPOSTerminalByStoreModel> GetPOSTerminalByStore(string storeNo)
        {
            return _data.GetPOSTerminalByStore(storeNo);
        }
        public List<MissSalePosModel> ListOrderCentralV2(string storeNo,string posID, DateTime bussinessDate)
        {
            return _data.ListOrderCentralV2(storeNo, posID, bussinessDate);
        }






    }
}
