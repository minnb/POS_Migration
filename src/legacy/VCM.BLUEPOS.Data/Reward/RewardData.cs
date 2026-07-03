using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Reward;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Data.Reward
{
    public interface IRewardData
    {

        List<RewardHeaderModel> LoadReward(string code, string title, string offerNo, string status, out int totalRecord, int skip, int take);
        ViewRewardModel InfoReward(string code);
        List<RewardCodeModel> RewardCodeLoad(string rewardNo, out int totalRecord, int skip, int take);
        Tuple<bool, string, RewardHeaderRequest> SaveReward(RewardHeaderRequest model);
        List<OfferHeaderModel> LoadOfferReward(string offerType, string offerNo, string offerName, out int total, int skip, int take);
    }
    public class RewardData : IRewardData
    {
        private readonly object lockRequest = new object();
        public List<RewardHeaderModel> LoadReward(string code, string title, string offerNo, string status, out int totalRecord, int skip, int take)
        {
            var result = new List<RewardHeaderModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;

                    var data = db.RewardHeaders.AsNoTracking()
                               .Select(a => new RewardHeaderModel
                               {
                                   RewardNo = a.RewardNo,
                                   Title = a.Title,
                                   OfferNo = a.OfferNo,
                                   Link = a.Link,
                                   Description = a.Description,
                                   //Enabled = a.Enabled,
                                   FromDate = a.FromDate,
                                   ToDate = a.ToDate,
                                   CreatedDate = a.CreatedDate,
                                   Enabled = a.Enabled == false ? false : DbFunctions.TruncateTime(a.ToDate) < DbFunctions.TruncateTime(date) ? false : true
                               });
                    if (!string.IsNullOrEmpty(code))
                    {
                        data = data.Where(m => m.RewardNo == code);
                    }
                    if (!string.IsNullOrEmpty(title))
                    {
                        data = data.Where(m => m.Title.ToLower().Contains(title.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(offerNo))
                    {
                        data = data.Where(m => m.OfferNo == offerNo);
                    }
                    if (!string.IsNullOrEmpty(status))
                    {
                        var enable = status == "0" ? false : true;
                        data = data.Where(m => m.Enabled == enable);
                    }

                    totalRecord = data.Count();
                    result = data.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public ViewRewardModel InfoReward(string code)
        {
            var result = new ViewRewardModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;
                    var info = db.RewardHeaders.Where(d => d.RewardNo == code)
                        .Select(a => new RewardHeaderModel
                        {
                            RewardNo = a.RewardNo,
                            Title = a.Title,
                            Description = a.Description,
                            FromDate = a.FromDate,
                            ToDate = a.ToDate,
                            OfferNo = a.OfferNo,
                            Link = a.Link,
                            Enabled = a.Enabled == false ? false : DbFunctions.TruncateTime(a.ToDate) < DbFunctions.TruncateTime(date) ? false : true,
                            IsReward = a.IsReward

                        }).FirstOrDefault();

                    result.infoReward = info;
                }
            }
            catch
            {

            }
            return result;
        }

        public List<RewardCodeModel> RewardCodeLoad(string rewardNo, out int totalRecord, int skip, int take)
        {
            var result = new List<RewardCodeModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var data = from a in db.RewardCodes
                               join b in db.RewardHeaders on a.RewardNo equals b.RewardNo
                               join c in db.RewardCodeSends on a.Code equals c.Code into t
                               from nt in t.DefaultIfEmpty()
                               where a.RewardNo == rewardNo
                               select new RewardCodeModel
                               {
                                   RewardNo = a.RewardNo,
                                   Code = a.Code,
                                   Enabled = a.Enabled ?? false,
                                   OrderNo = nt.OrderNo,
                                   OfferNo = nt.OfferNo
                               };

                    totalRecord = data.Count();
                    result = data.OrderBy(d => d.Code).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public Tuple<bool, string, RewardHeaderRequest> SaveReward(RewardHeaderRequest model)
        {
            var result = new Tuple<bool, string, RewardHeaderRequest>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var checkHeader = db.RewardHeaders.FirstOrDefault(d => d.RewardNo == model.RewardNo);
                            var listCode = model.listCode;
                            var dateTime = DateTime.Now;
                            var counterHeader = db.RewardHeaders.Max(a => a.Counter) ?? 0;
                            if (!string.IsNullOrEmpty(model.RewardNo))
                            {
                                checkHeader.Title = model.Title;
                                checkHeader.Description = model.Description;
                                checkHeader.Link = model.Link;
                                checkHeader.Enabled = model.Enabled;
                                checkHeader.FromDate = model.FromDate;
                                checkHeader.ToDate = model.ToDate;
                                checkHeader.IsReward = model.IsReward;
                            }
                            else
                            {
                                var rewardNo = "R0000001";
                                var listRewardHeader = db.RewardHeaders.Where(d => d.RewardNo.Substring(0, 1) == "R").Select(d => d.RewardNo.Substring(1, d.RewardNo.Length - 1)).ToList();
                                if (listRewardHeader != null || listRewardHeader.Count > 0)
                                {
                                    //0000001
                                    //3
                                    var listNo = listRewardHeader.Select(int.Parse).ToList();
                                    if (listNo.Count > 0)
                                    {
                                        var maxRewardNo = "" + (listRewardHeader.Select(int.Parse).ToList().Max() + 1);

                                        rewardNo = rewardNo.Substring(0, rewardNo.Length - maxRewardNo.Length) + maxRewardNo;
                                    }
                                }

                                var counterCode = db.RewardCodes.Max(a => a.Counter) ?? 0;
                                var insertHeader = new RewardHeader
                                {
                                    RewardNo = rewardNo,
                                    Title = model.Title,
                                    FromDate = model.FromDate,
                                    ToDate = model.ToDate,
                                    OfferNo = model.OfferNo,
                                    Link = model.Link,
                                    Description = model.Description,
                                    Enabled = model.Enabled,
                                    CreatedDate = dateTime,
                                    Counter = counterHeader,
                                    Pkey = rewardNo,
                                    IsReward=model.IsReward
                                };
                                db.RewardHeaders.Add(insertHeader);

                                if (listCode != null && listCode.Count > 0)
                                {
                                    var listCodeInsert = listCode.Select(a => new RewardCode
                                    {
                                        RewardNo = rewardNo,
                                        Code = a.MaDuThuong,
                                        Enabled = true,
                                        CreatedDate = dateTime,
                                        Counter = counterCode,
                                        Pkey = rewardNo
                                    });

                                    db.RewardCodes.AddRange(listCodeInsert);
                                }
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, RewardHeaderRequest>(true, $"Cập nhật thành công mã dự thưởng", model);
                        }

                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, RewardHeaderRequest>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }

            return result;
        }

        public List<OfferHeaderModel> LoadOfferReward(string offerType, string offerNo, string offerName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.OfferHeaders.Where(d => d.Status == 0 && d.OfferType == offerType).Select(a => new OfferHeaderModel
                    {
                        No = a.No,
                        Description = a.Description,
                        OfferType = a.OfferType
                    });
                    if (!string.IsNullOrEmpty(offerNo))
                    {
                        data = data.Where(m => m.No == offerNo);
                    }
                    if (!string.IsNullOrEmpty(offerName))
                    {
                        data = data.Where(m => m.Description.ToLower().Contains(offerName.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.No).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<OfferHeaderModel>();
            }
        }
    }
}
