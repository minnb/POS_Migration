using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.LogService;

namespace VCM.POSBLUE.Data.LogServiceAPI
{
    public interface ILogServiceData
    {
        void LogServiceInsert(LogServiceModel model);
    }
    public class LogServiceData : ILogServiceData
    {
        public void LogServiceInsert(LogServiceModel model)
        {
            using (var db = new LoyaltyEntityContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 60;
                    if (model != null)
                    {
                        var itemSave = new LogService
                        {
                            StoreNo = model.StoreNo,
                            PosTerminal = model.PosTerminal,
                            RequestPOS = model.RequestPOS,
                            RequestPartner = model.RequestPartner,
                            ResponsPOS = model.ResponsPOS,
                            ResponsePartner = model.ResponsePartner,
                            EndPointPartner = model.EndPointPartner,
                            MethodPartner = model.MethodPartner,
                            ActionController = model.ActionController,
                            TimeResponse = model.TimeResponse,
                            CreatedDate = model.CreatedDate
                        };
                        db.LogServices.Add(itemSave);
                        db.SaveChanges();
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
