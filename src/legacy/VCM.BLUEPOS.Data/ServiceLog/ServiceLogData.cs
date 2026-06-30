using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.ServiceLog;

namespace VCM.BLUEPOS.Data.ServiceLog
{
    public interface IServiceLogData
    {
        ResultResponseModel WriteStackTraceLog(StackTraceLogModel req);
    }
    public class ServiceLogData : IServiceLogData
    {
        public ResultResponseModel WriteStackTraceLog(StackTraceLogModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 5 * 60;
                try
                {
                    if (req.UserName == null)
                    {
                        return null;
                    }
                    var logList = new StackTraceLog
                    {
                        UserName = req.UserName,
                        StoreNo = req.StoreNo,
                        POSTerminal = req.POSTerminal,
                        Description = req.Description,
                        Controller = req.Controller,
                        Action = req.Action,
                        ActionType = req.ActionType,
                        JSONModel = req.JSONModel,
                        MessageError = req.MessageError,
                        ErrorDateTime = DateTime.Now,
                        IPServer = req.IPServer,
                        URL = req.URL,
                        Source = req.Source,
                        RefKey1 = string.Empty,
                        RefKey2 = string.Empty,
                        RefKey3 = string.Empty,
                        RefKey4 = string.Empty,
                        RefKey5 = string.Empty,
                        CreatedDate = DateTime.Now,
                        CreatedBy = req.UserName,
                        UpdatedDate = DateTime.Now,
                        UpdatedBy = req.UserName
                    };

                    db.StackTraceLogs.Add(logList);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.UserName,
                        Item2 = req.StoreNo,
                        Item3 = req.POSTerminal,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Ghi logs thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }

    }

}
