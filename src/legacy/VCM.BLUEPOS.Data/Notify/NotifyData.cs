using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Notify;

namespace VCM.BLUEPOS.Data.Notify
{
    public interface INotifyData
    {
        List<NotifyModel> LoadNotify(string title, string status, out int totalRecord, int skip, int take);
        ViewNotifyModel ViewInfoNotify(string id);
        Tuple<bool, string, NotifyViewClientModel> ViewTop1NotifyClient(string userName);
        Tuple<bool, string, NotifySaveModel> SaveNotify(NotifySaveModel model);
        Tuple<bool, string, NotifyConfirmModel> ConfirmNotify(NotifyConfirmModel model);
        Tuple<bool, string> DeleteNotify(string ID);
    }
    public class NotifyData : INotifyData
    {
        private readonly object lockRequest = new object();
        public List<NotifyModel> LoadNotify(string title, string status, out int totalRecord, int skip, int take)
        {
            var result = new List<NotifyModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;

                    var data = from a in db.Notifies
                                   //join b in db.ExtraFeeStroreGroups on a.StoreGroup equals b.StoreGroup
                               select new NotifyModel
                               {
                                   ID = a.ID,
                                   Title = a.Title,
                                   Description = a.Description,
                                   Status = DbFunctions.TruncateTime(a.ToDate.Value) < date ? false : a.Status,
                                   CreatedUser = a.CreatedUser,
                                   FromDate = a.FromDate,
                                   ToDate = a.ToDate,
                                   CreatedDate = a.CreatedDate
                               };
                    if (!string.IsNullOrEmpty(title))
                    {
                        data = data.Where(m => m.Title == title);
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        bool stt = status == "1" ? true : false;
                        data = data.Where(m => m.Status == stt);
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

        public ViewNotifyModel ViewInfoNotify(string id)
        {
            var result = new ViewNotifyModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var info = new NotifyModel();
                    if (!string.IsNullOrEmpty(id))
                    {
                        var ID = int.Parse(id);
                        info = (from a in db.Notifies
                                where a.ID == ID
                                select new NotifyModel
                                {
                                    ID = a.ID,
                                    Title = a.Title,
                                    Description = a.Description,
                                    FromDate = a.FromDate,
                                    ToDate = a.ToDate,
                                    Status = a.Status,
                                    CreatedDate = a.CreatedDate,
                                    CreatedUser = a.CreatedUser
                                }).FirstOrDefault();
                    }

                    result = new ViewNotifyModel
                    {
                        Info = info
                    };
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public Tuple<bool, string, NotifyViewClientModel> ViewTop1NotifyClient(string userName)
        {
            var model = new NotifyViewClientModel();
            var result = new Tuple<bool, string, NotifyViewClientModel>(false, "", model);
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var info = new NotifyModel();
                    var date = DateTime.Now.Date;

                    model = (from a in db.Notifies
                             where a.Status == true && DbFunctions.TruncateTime(a.ToDate) >= date && DbFunctions.TruncateTime(a.FromDate) <= date
                             select new NotifyViewClientModel
                             {
                                 ID = a.ID,
                                 Title = a.Title,
                                 Description = a.Description

                             }).OrderByDescending(d => d.ID).FirstOrDefault();
                    if (model != null)
                    {
                        var checkConfirm = db.NotifyComfirms.Any(d => d.Username == userName && d.NotifyID == model.ID && d.Status == true);
                        if (!checkConfirm)
                        {
                            result = new Tuple<bool, string, NotifyViewClientModel>(true, "", model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }
        public Tuple<bool, string, NotifySaveModel> SaveNotify(NotifySaveModel model)
        {
            var result = new Tuple<bool, string, NotifySaveModel>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var checkNotify = db.Notifies.FirstOrDefault(d => d.ID == model.ID);
                            if (checkNotify != null)
                            {
                                checkNotify.Title = model.Title;
                                checkNotify.Description = model.Description;
                                checkNotify.FromDate = model.FromDate;
                                checkNotify.ToDate = model.ToDate;
                                checkNotify.Status = model.Status;
                            }
                            else
                            {
                                var insert = new EF.Central.Notify
                                {
                                    Title = model.Title,
                                    Description = model.Description,
                                    FromDate = model.FromDate,
                                    ToDate = model.ToDate,
                                    Status = model.Status,
                                    CreatedDate = model.CreatedDate,
                                    CreatedUser = model.CreatedUser
                                };
                                db.Notifies.Add(insert);
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, NotifySaveModel>(true, $"Cập nhật thành công thông báo", model);
                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, NotifySaveModel>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }
        public Tuple<bool, string, NotifyConfirmModel> ConfirmNotify(NotifyConfirmModel model)
        {
            var result = new Tuple<bool, string, NotifyConfirmModel>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    try
                    {
                        var insert = new NotifyComfirm
                        {
                            NotifyID = model.NotifyID,
                            Title = model.Title,
                            Status = model.Status,
                            Username = model.Username,
                            CreatedDate = model.CreatedDate
                        };
                        db.NotifyComfirms.Add(insert);
                        db.SaveChanges();

                        result = new Tuple<bool, string, NotifyConfirmModel>(true, $"Cập nhật thành công", model);
                    }
                    catch (Exception ex)
                    {
                        result = new Tuple<bool, string, NotifyConfirmModel>(false, JsonConvert.SerializeObject(ex), model);

                    }
                }
            }
            return result;
        }

        public Tuple<bool, string> DeleteNotify(string ID)
        {
            var result = new Tuple<bool, string>(false, $"");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    try
                    {
                        var id = int.Parse(ID);
                        var checkID = db.Notifies.FirstOrDefault(d => d.ID == id);
                        if (checkID != null)
                        {
                            db.Notifies.Remove(checkID);
                            db.SaveChanges();
                            return new Tuple<bool, string>(true, $"Success");
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tìm thấy dữ liệu cần xóa");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));

                        return result;
                    }

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                return result;
            }
        }
    }
}
