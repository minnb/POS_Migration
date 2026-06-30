using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.SalesStaging;


namespace VCM.BLUEPOS.Data.SalesStaging
{
    public interface ISalesStagingData
    {
        List<StagingTransHeaderModel> StagingListOrder(string storeNo, string orderNo);
        List<StagingSyncTableFromPOSModel> StagingListTableFromPOS(string storeNo);
    }
    public class SalesStagingData : ISalesStagingData
    {
        public List<StagingTransHeaderModel> StagingListOrder(string storeNo, string orderNo)
        {
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            var result = new List<StagingTransHeaderModel>();
            try
            {
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.TransHeaders.Where(d => d.OrderNo == orderNo).Select(a => new StagingTransHeaderModel { OrderNo = a.OrderNo }).ToList();                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        public List<StagingSyncTableFromPOSModel> StagingListTableFromPOS(string storeNo)
        {
            var result = new List<StagingSyncTableFromPOSModel>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesStagingContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;

                    result = db.SyncTableFromPOS.Where(d => d.Status == 1 && d.GroupName == "SALE").OrderBy(d => d.OrdBy).
                    Select(a => new StagingSyncTableFromPOSModel
                    {
                        TableName = a.TableName,
                        DocumentNoName = a.DocumentNoName,
                        OrdBy = (int)a.OrdBy
                    }).ToList();

                    return result;
                }
            }
            catch(Exception e)
            {
                return result;
            }
        }
    }




}
