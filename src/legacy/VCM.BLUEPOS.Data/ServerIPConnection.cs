using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.EF.Central;



namespace VCM.BLUEPOS.Data
{
    public class ServerIPConnection
    {
        public static string GetIPServerByStore(string storeNo)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    var serverIP = db.StoreSetServers.FirstOrDefault(d => d.StoreNo == storeNo).ServerIP ?? "";
                    return serverIP;
                }
            }
            catch
            {
            }
            return "";
        }
        public static string GetIPServerReadByStore(string storeNo)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    var serverIPRead = db.StoreSetServers.FirstOrDefault(d => d.StoreNo == storeNo).ServerRead ?? "";
                    if (string.IsNullOrEmpty(serverIPRead))
                        return GetIPServerByStore(storeNo);
                    return serverIPRead;
                }
            }
            catch
            {
                return GetIPServerByStore(storeNo);
            }
        }


    }
}
