using System;
using System.Collections.Generic;
using System.Linq;
using TCX.API.Common.Helpers;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Data.Redis;

namespace VCM.POSBLUE.Data
{
    public class ServerIPConnection
    {
        public static List<StoreSetServer> GetStoreSetServers()
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    return db.StoreSetServers.ToList();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetStoreSetServers", ex);
                return null;
            }
        }

        public static string GetIPServerByStore(string storeNo)
        {
            try
            {
                RedisConnect redisConnect = new RedisConnect();
                var serverIP = redisConnect.HashGet<string>(RedisKeyConst.Redis_GetIPServerByStore, storeNo, true);
                if (string.IsNullOrEmpty(serverIP)) 
                {
                    using (var db = new CentralGeneralContainer())
                    {
                        var dataStoreSetServers = db.StoreSetServers.FirstOrDefault(d => d.StoreNo.ToUpper() == storeNo.ToUpper());
                        if (dataStoreSetServers != null)
                        {
                            serverIP = dataStoreSetServers.ServerIP ?? "";
                            redisConnect.HashSet<string>(RedisKeyConst.Redis_GetIPServerByStore, storeNo, serverIP, false);
                        }
                    }
                }
                return serverIP;
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("GetIPServerByStore: " + storeNo, ex);
                return "";
            }
        }
    }
}
