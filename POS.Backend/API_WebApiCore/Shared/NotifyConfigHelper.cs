using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore.Shared
{
    public static class NotifyConfigHelper
    {
        public static string GetNotifyConfigByStatus(MemoryCacheService memoryCacheService, string appCode, string status, string code = "", string actionType = "Check")
        {
            try
            {
                var messConfig = memoryCacheService.GetNotifyConfig(memoryCacheService.GetConnectStringCentralMD()).Where(x => x.AppCode.ToUpper() == appCode.ToUpper()).ToList();
                if (messConfig != null && messConfig.Count > 0)
                {
                    var messData = messConfig.FirstOrDefault(x => x.Status == status && x.ActionType == actionType);
                    if (messData != null)
                    {
                        string message = messData.Message;
                        if (!string.IsNullOrEmpty(code) && message.Contains("[code]"))
                        {
                            message = StringHelper.ReplaceString(message, "[code]", code);
                        }
                        else if (string.IsNullOrEmpty(code) && message.Contains("{code}"))
                        {
                            messData.Message = StringHelper.ReplaceString(message, "[code]", "");
                        }
                        return message;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("NotifyConfigHelper.GetNotifyConfigByStatus", ex);
            }
            return null;
        }
    }
}
