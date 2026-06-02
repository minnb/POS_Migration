using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos.Ops;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.AppServices;

namespace TCX.WebApiCore.Shared
{
    public static class OpsMonitoringHelper
    {
        public static async Task OpsLogging(Ops_Logging_Data ops_Logging_Data)
        {
            try
            {
                if(ops_Logging_Data == null) return;

                ops_Logging_Data.Group_name= "WCM.POSBLUE.API";
                KibanaService kibanaService = new KibanaService();
                var messData = new Ops_Logging
                {
                    Table_name = "webapi_error_logs",
                    Action = "insert",
                    Data = ops_Logging_Data
                };

                await Task.Run(() =>
                {
                    RabbitMQProducer.ProducerRabbtMQCluster(kibanaService,
                        RabitMQConst.Queue_Ops_Logging,
                        "",
                        StringHelper.ObjectToStringLowercase(messData));
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("OpsMonitoringHelper.OpsLogging", ex);
            }
        }
        public static async Task OpsMonitoring(MemoryCacheService memoryCacheService, string group_name)
        {
            try
            {
                KibanaService kibanaService = new KibanaService();
                var messData = new Ops_Monitoring
                {
                    Table_name = "webapi_status",
                    Action = OpsActionEnum.upsert.ToString(),
                    Data = new Ops_Monitoring_Data()
                    {
                        Server_code = HostHelper.GetIpAddress(),
                        Status = OpsStatusEnum.running.ToString(),
                        Group_name = group_name,
                        Version = memoryCacheService.GetVersion("bin\\VCM.POSBLUE.API.dll"),
                        Cpu_percent = (decimal) HostHelper.GetTotalCpuCores(),
                        Memory_mb = (decimal) HostHelper.GetTotalRamMB(),
                    }
                };

                await Task.Run(() =>
                {
                    RabbitMQProducer.ProducerRabbtMQCluster(kibanaService, 
                        RabitMQConst.Queue_Ops_Logging, 
                        "", 
                        StringHelper.ObjectToStringLowercase(messData));
                });
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("OpsMonitoringHelper.OpsMonitoring", ex);
            }
        }
    }
}