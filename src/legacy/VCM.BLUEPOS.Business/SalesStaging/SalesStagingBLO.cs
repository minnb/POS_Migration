using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.SalesStaging;
using VCM.BLUEPOS.Data.SalesStaging;



namespace VCM.BLUEPOS.Business.SalesStaging
{
    public interface ISalesStagingBLO
    {
        List<StagingTransHeaderModel> StagingListOrder(string storeNo, string orderNo);
        List<StagingSyncTableFromPOSModel> StagingListTableFromPOS(string storeNo);
    }
    public class SalesStagingBLO : ISalesStagingBLO
    {
        private  SalesStagingData _data { get; set; }
        public SalesStagingBLO()
        {
            _data = new SalesStagingData();
        }
        
        public List<StagingTransHeaderModel> StagingListOrder(string storeNo, string orderNo)
        {
            return _data.StagingListOrder(storeNo, orderNo);
        }
        public List<StagingSyncTableFromPOSModel> StagingListTableFromPOS(string storeNo)
        {
            return _data.StagingListTableFromPOS(storeNo);
        }
    }


}
