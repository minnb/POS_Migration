using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.POS.Gift;

namespace VCM.POSBLUE.Data.Reposites
{
    public class POSGiftInfoRepository
    {
        public bool UpdateGiftStatus(string[] giftCode, string status, string posUsed)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    var dataGift = db.POSGiftInfoes.Where(x => giftCode.Contains(x.GiftCode)).ToList();
                    if(dataGift != null && dataGift.Count > 0)
                    {
                        dataGift.ForEach(x => { x.GiftStatus = status; x.PosUsed = posUsed; x.UsedTime = DateTime.Now; });
                        db.SaveChanges();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("UpdateGiftStatus Exception: " + ex.Message.ToString());
                return false;
            }
        }
        public List<POSGiftInfo> GetByGiftCode(string[] giftCode)
        {
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    return db.POSGiftInfoes.Where(x => giftCode.Contains(x.GiftCode)).ToList();
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("GetByGiftCode Exception: " + ex.Message.ToString());
                return null;
            }
        }

        public List<POSGiftInfo> InsertPOSGiftInfo(List<POSGiftInfo> giftInfos)
        {
            List<POSGiftInfo> result = new List<POSGiftInfo>();
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    if (giftInfos != null && giftInfos.Count > 0)
                    {
                        var lstGiftCode = giftInfos.Select(x => x.GiftCode).ToList();

                        var checkGiftNotInDB = db.POSGiftInfoes.Where(x => lstGiftCode.Contains(x.GiftCode)).ToList();
                        if(checkGiftNotInDB.Count > 0)
                        {
                            var giftInsert = giftInfos.Where(x => !checkGiftNotInDB.Select(a => a.GiftCode).ToList().Contains(x.GiftCode)).ToList();
                            giftInsert.ForEach(x => db.POSGiftInfoes.Add(x));
                            result.AddRange(giftInsert);
                            result.AddRange(checkGiftNotInDB);
                        }
                        else
                        {
                            giftInfos.ForEach(x => db.POSGiftInfoes.Add(x));
                            result.AddRange(giftInfos);
                        }
                        
                        db.SaveChanges();
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("InsertPOSGiftInfo Exception: " + ex.Message.ToString());
                return null;
            }
        }
    }
}
