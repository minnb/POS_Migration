using Dapper;
using Elasticsearch.Net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.DbContext;

namespace TCX.WebApiCore.Repository
{
    public class CentralMDRepository
    {
        public CentralMDRepository()
        {
        }
        public async Task<MMLSchemeHeader> GetMMLSchemeHeader(RedisManager redisManager, string code)
        {
            try
            {
                var data = await redisManager.HashGetAsync<MMLSchemeHeader>(RedisConst.Redis_Key_MMLSchemeHeader, code);
                if (data != null)
                {
                    return data;
                }
                var queryData = @"SELECT [Code] AS HeaderCode,[FromDate],[ToDate],[IsMember],[IsCallAPI],[MinAmount],[Enabled],[Ref1],[Ref2],[Ref3],[Ref4],[Ref5] 
                                FROM MMLSchemeHeader (NOLOCK) 
                                  WHERE CAST([FromDate] AS DATE) <= CAST(getdate() AS DATE) AND CAST([ToDate] AS DATE) >= CAST(getdate() AS DATE) 
                                    AND ISNULL([Enabled], 0) = 1 AND Code = @code;";

                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    data = await db.QueryFirstOrDefaultAsync<MMLSchemeHeader>(queryData, new { code });
                    if (data == null)
                    {
                        return null;
                    }
                    redisManager.HashSet<MMLSchemeHeader>(RedisConst.Redis_Key_MMLSchemeHeader, code, data);
                    return data;
                }

            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetMMLSchemeHeader.Exception", ex);
                return null;
            }
        }
        public async Task<List<MMLSchemeItem>> GetMMLSchemeItem(RedisManager redisManager)
        {
            try
            {
                var data = await redisManager.StringGetAsync<List<MMLSchemeItem>>(RedisConst.Redis_Key_MMLSchemeItem);
                if (data != null && data.Count > 0)
                {
                    return data;
                }
                var queryData = @"SELECT I.*
                                    FROM MMLSchemeHeader (NOLOCK) H
                                    INNER JOIN MMLSchemeItem (NOLOCK) I ON H.Code = I.HeaderCode
                                    WHERE CAST(H.[FromDate] AS DATE) <= CAST(getdate() AS DATE) AND CAST(H.[ToDate] AS DATE) >= CAST(getdate() AS DATE) 
	                                    AND ISNULL(H.[Enabled], 0) = 1;";

                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    data = db.Query<MMLSchemeItem>(queryData).ToList();
                    if (data == null)
                    {
                        return null;
                    }
                    redisManager.StringSet<List<MMLSchemeItem>>(RedisConst.Redis_Key_MMLSchemeItem, data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetMMLSchemeItem.Exception", ex);
                return null;
            }
        }
        public async Task<MMLSchemeResponse> GetMMLSchemeResponse(RedisManager redisManager, string headerCode, string code)
        {
            string key = $"{headerCode}-{code}";
            try
            {
                var data = redisManager.HashGet<MMLSchemeResponse>(RedisConst.Redis_Key_MMLSchemeResponse, key);
                if (data != null)
                {
                    return data;
                }
                var queryData = @"SELECT DISTINCT I.*
                                    FROM MMLSchemeHeader (NOLOCK) H
                                    INNER JOIN MMLSchemeResponse (NOLOCK) I ON H.Code = I.HeaderCode
                                    WHERE CAST(H.[FromDate] AS DATE) <= CAST(getdate() AS DATE) AND CAST(H.[ToDate] AS DATE) >= CAST(getdate() AS DATE) 
	                                    AND ISNULL(H.[Enabled], 0) = 1 AND I.HeaderCode = @headerCode AND I.Code = @code;";

                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    data = await db.QueryFirstOrDefaultAsync<MMLSchemeResponse>(queryData, new { headerCode, code });
                    if (data == null)
                    {
                        return null;
                    }
                    redisManager.HashSet<MMLSchemeResponse>(RedisConst.Redis_Key_MMLSchemeResponse, key, data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetMMLSchemeResponse.Exception", ex);
                return null;
            }
        }
        public LoyaltyRateDto GetLoyaltyRateData(RedisManager redisManager, string code)
        {
            try
            {
                var data = redisManager.HashGet<LoyaltyRateDto>(RedisConst.Redis_Key_LoyaltyRate, code);
                if (data != null)
                {
                    return data;
                }
                var queryData = @"SELECT FromDate, ToDate, Code, Rate, IIF([Enable] = 1, 0, 1) AS Blocked, Pkey, CardType FROM LoyaltyRate (NOLOCK) WHERE [Enable] = 1 AND [Code] = @code;";
                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    data = db.QueryFirstOrDefault<LoyaltyRateDto>(queryData, new { code });
                    if (data == null)
                    {
                        return null;
                    }
                    redisManager.HashSet<LoyaltyRateDto>(RedisConst.Redis_Key_LoyaltyRate, code, data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetLoyaltyRateData.Exception", ex);
                return null;
            }
        }
        public List<string> GetSyncTableList()
        {
            try
            {
                var queryData = @"SELECT TableName FROM SyncTableList (NOLOCK) WHERE IsAll = 1 GROUP BY TableName ORDER BY TableName;";
                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    return db.Query<string>(queryData).ToList();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetSyncTableList.Exception", ex);
                return null;
            }
        }
        public ItemPointsMemberDto GetItemPointsMember(RedisManager redisManager, string pointsCode, string itemNo, string uom)
        {
            try
            {
                string hashField = $"{pointsCode}-{itemNo}-{uom}";
                var data = redisManager.HashGet<ItemPointsMemberDto>(RedisConst.Redis_Key_ItemPointsMember, hashField);
                if (data != null)
                {
                    return data;
                }

                var queryData = @"SELECT * FROM ItemPointsMember (NOLOCK) WHERE Blocked = 0 AND PointsCode = @pointsCode AND ItemNo = @itemNo AND Uom = @uom;";
                using (IDbConnection db = DapperCentralMDFactory.CreateConnection())
                {
                    db.Open();
                    data = db.QueryFirstOrDefault<ItemPointsMemberDto>(queryData, new { pointsCode, itemNo, uom });
                    if (data == null)
                    {
                        return null;
                    }
                    redisManager.HashSet<ItemPointsMemberDto>(RedisConst.Redis_Key_ItemPointsMember, hashField, data, timeSeconds: 360);
                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CentralMDRepository.GetItemPointsMember.Exception", ex);
                return null;
            }
        }
    }
}