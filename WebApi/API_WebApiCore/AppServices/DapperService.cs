using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;

namespace TCX.WebApiCore.AppServices
{
    public class DapperService
    {
        private readonly RedisCacheService _redisCacheService;
        public DapperService()
        {
            _redisCacheService = new RedisCacheService();
        }
        private string GetConnectString(string key)
        {
            string conStr = string.Empty;
            switch (key)
            {
                case "DAPPER_CENTRAL_MD":
                    conStr = ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString;
                    break;
                case "DAPPER_LOYALTY":
                    conStr = ConfigurationManager.ConnectionStrings["DAPPER_LOYALTY"].ConnectionString;
                    break;
                case "DAPPER_PARTNER":
                    conStr = ConfigurationManager.ConnectionStrings["DAPPER_PARTNER"].ConnectionString;
                    break;
                case "DAPPER_PLHLog":
                    conStr = ConfigurationManager.ConnectionStrings["DAPPER_PLHLog"].ConnectionString;
                    break;
            }
            return conStr;
        }
        public T RunProcedureSQL<T>(string conStr, string procedure, string keyRedisDelete = "")
        {
            if (string.IsNullOrEmpty(conStr)) { return (T)Convert.ChangeType(conStr, typeof(T)); }
            
            var dapperDB = new DapperContext(GetConnectString(conStr));
            using (IDbConnection db = dapperDB.CreateConnDB)
            {
                db.Open();
                try
                {
                    var result = db.Query<T>(procedure, commandTimeout: 36000);
                    if (result != null)
                    {
                        if (!string.IsNullOrEmpty(keyRedisDelete))
                        {
                            _redisCacheService.Delete(keyRedisDelete);
                        }
                    }
                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch (Exception ex)
                {
                    FileHelper.WriteExpLogs($"EXEC {procedure} ==> RunProcedureSQL.Exception", ex);
                    return (T)Convert.ChangeType(ex, typeof(T));
                }
            }
        }
    }
}
