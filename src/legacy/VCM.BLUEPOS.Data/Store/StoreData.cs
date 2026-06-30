using VCM.BLUEPOS.Data.EF.PLG;
using VCM.BLUEPOS.Model.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Common;

namespace VCM.BLUEPOS.Data.Store
{
    public interface IStoreData
    {
        List<StoreModel> ListStore(string appCode);
        List<StorePartnerModel> ListStorePartner();
        List<IPAddressModel> LoadTerminalByStoreNo(string storeNo);
    }
    public class StoreData : IStoreData
    {
        public List<StoreModel> ListStore(string appCode)
        {
            var result = new List<StoreModel>();
            using (var db = new PLGContainer())
            {
                try
                {
                    result = db.Stores.Where(d => d.AppCode == appCode)
                              .Select(a => new StoreModel
                              {
                                  Id = a.Id,
                                  AppCode = a.AppCode,
                                  StoreNo = a.StoreNo,
                                  ObjectID = a.ObjectID,
                                  StoreName = a.StoreName,
                                  Blocked = a.Blocked

                              }).ToList();
                }
                catch
                {

                }
                return result;
            }
        }
        public List<StorePartnerModel> ListStorePartner()
        {
            var result = new List<StorePartnerModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Stores.Where(d => d.ClosingMethod == 0)
                              .Select(a => new StorePartnerModel
                              {
                                  SiteNo = a.No,
                                  SiteName = a.Name,
                                  StyleProfile = a.StyleProfile
                              }).ToList();
                }
                catch
                {
                }
                return result;
            }
        }

        public List<IPAddressModel> LoadTerminalByStoreNo(string storeNo)
        {
            try
            {
                var result = new List<IPAddressModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSTerminals.Where(d => d.StoreNo == storeNo).ToList().Select(a => new IPAddressModel
                    {
                        StoreNo = a.StoreNo,
                        IpAddress = a.IPAddress,
                        PosTerminalID = a.No
                    }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                return (default(List<IPAddressModel>));
            }
        }
    }
}
