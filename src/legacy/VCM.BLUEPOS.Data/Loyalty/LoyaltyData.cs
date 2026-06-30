using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.Loyalty;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Loyalty;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Model.OptionModel;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.Data.Entity;

namespace VCM.BLUEPOS.Data.Loyalty
{
    public interface ILoyaltyData
    {
        List<GiftRedeemResponseModel> GetSetupLoyaltyList(DateTime fromDate, DateTime toDate, string itemNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<SetupLoyaltyResponseModel> GetProductList(string textSearch, out int totalRecord, int pageNumber = 0, int pageSize = 100);
        ResultResponseModel CreateSetupLoyalty(CreateSetupLoyaltyModel req);
        ResultResponseModel UpdateSetupLoyalty(UpdateSetupLoyaltyModel req);
        ResultResponseModel DeleteSetupLoyalty(DeleteLoyaltyModel req);
        List<ListGiftRedeemModalModel> GetItemGiftRedeemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100);
        ResultResponseModel CreateSetupLoyaltyForTabGiftRedeem(CreateSetupLoyaltyTabGiftRedeemModel req);
        Tuple<int, string> ImportExcelGiftRedeem(string userName, DataTable dt);
        //Tuple<int, string> ImportExcelGiftRedeem_V2(string userName, List<ImportExcelGiftRedeemModel> listData);
        List<ExportExcelGiftRedeemResponseModel> ExportExcelGiftRedeemList(DateTime fromDate, DateTime toDate, string itemNo, string status);

        List<MemberEarnItemResponseModel> SetupMemberEarnItemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100);
        List<ExportMemberEarnItemResponseModel> ExportExcel_MemberEarnItemList(string textSearch, string status);
        ResultResponseModel UpdateSetupMemberEarnItem(UpdateSetupMemberEarnItemModel req);
        ResultResponseModel DeleteSetupMemberEarnItem(DeleteSetupMemberEarnItemModel req);
        Tuple<int, string, string> ImportExcelMemberEarnItem(string userName, List<ImportExcelSetupMemberEarnItemModel> listData);
        List<GiftCouponResponseModel> GetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcelGiftCouponResponseModel> ExportExcelGetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX);




    }

    public class LoyaltyData : ILoyaltyData
    {
        public List<GiftRedeemResponseModel> GetSetupLoyaltyList(DateTime fromDate, DateTime toDate, string itemNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<GiftRedeemResponseModel>("[dbo].[GET_GIFT_REDEEM_LOYALTY_LIST] @FromDate, @ToDate, @ItemNo, @Status",
                    new SqlParameter("@FromDate", fromDate),
                    new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                    new SqlParameter("@Status", status)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GiftRedeemResponseModel>();
                    }
                    var listdata = (from s in data
                                    select new GiftRedeemResponseModel
                                    {
                                        ID = s.ID,
                                        ItemNo = s.ItemNo,
                                        ItemName = s.ItemName,
                                        PointRedeem = s.PointRedeem,
                                        FromDateStr = s.FromDateStr,
                                        ToDateStr = s.ToDateStr,
                                        StatusStr = s.StatusStr,
                                        SalesUnitOfMeasure = s.SalesUnitOfMeasure,
                                        CreatedDate = s.CreatedDate,
                                        CreatedUser = s.CreatedUser,
                                        UpdatedDate = s.UpdatedDate,
                                        UpdatedUser = s.UpdatedUser,
                                        Pkey = s.Pkey
                                    });

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        listdata = listdata.Where(x => x.ItemNo.ToLower().Contains(searchText.ToLower().Trim()) || x.ItemName.ToLower().Contains(searchText.ToLower().Trim()));
                    }

                    totalRecord = listdata.Count();
                    var result = skip == 0 ? listdata.Take(take).ToList() : listdata.Skip(skip).Take(take).ToList();
                    return result;

                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<GiftRedeemResponseModel>();
            }
        }

        public List<SetupLoyaltyResponseModel> GetProductList(string textSearch, out int totalRecord, int pageNumber = 0, int pageSize = 100)
        {
            try
            {
                var data = new List<SetupLoyaltyResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<SetupLoyaltyResponseModel>("[dbo].[GET_ITEM_TABLE_NOT_IN_GIFT_REDEEM_TABLE] @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SetupLoyaltyResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SetupLoyaltyResponseModel>();
            }
        }

        public List<ListGiftRedeemModalModel> GetItemGiftRedeemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100)
        {
            try
            {
                var data = new List<ListGiftRedeemModalModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    data = db.Database.SqlQuery<ListGiftRedeemModalModel>("[dbo].[GET_GIFT_REDEEM_LIST] @TextSearch, @Status, @PageSize, @PageNumber",
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@Status", status),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ListGiftRedeemModalModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ListGiftRedeemModalModel>();
            }
        }
        public ResultResponseModel CreateSetupLoyalty(CreateSetupLoyaltyModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var totalRow = db.GiftRedeems.Any();

                    if (!totalRow) // = false : khong co dong nao
                    {
                        foreach (var item in req.ListItem)
                        {
                            if (req.FromDate.Date < DateTime.Now.Date)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = "Thêm mới không thành công. Vì ngày bắt đầu có hiệu lực nhỏ hơn ngày hiện tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;

                            var insert = new EF.Central.GiftRedeem
                            {
                                ItemNo = item.ItemNo,
                                Point = req.Point,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                Status = 1,    // true = 1, false = 0
                                Counter = maxCounter + 1,
                                Pkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}"
                            };

                            db.GiftRedeems.Add(insert);
                            db.SaveChanges();

                            var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                            var insertLog = new EF.Central.GiftRedeemModifyLog
                            {
                                ItemNo = item.ItemNo,
                                Point = req.Point,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                Status = req.Status == 1 ? true:false,
                                Counter = maxCounterLog + 1,
                                Pkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}", 
                                CreatedDate = DateTime.Now,
                                CreatedUser = req.CreatedUser
                            };

                            db.GiftRedeemModifyLogs.Add(insertLog);
                            db.SaveChanges();

                        }

                        return new ResultResponseModel
                        {
                            Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                            Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                            Item3 = "1",
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Lưu tỷ lệ đổi quà thành công"
                        };

                    }
                    else
                    {
                        foreach (var item in req.ListItem)
                        {
                            var checkPkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}";

                            var data = db.GiftRedeems.FirstOrDefault(x => x.Pkey == checkPkey);
                            var dataLog = db.GiftRedeemModifyLogs.FirstOrDefault(x => x.Pkey == checkPkey);

                            if (data == null) // insert
                            {
                                // Check ko cho tao chuong trinh trong qua khu

                                if (req.FromDate.Date < DateTime.Now.Date)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = "Không được phép tạo chương trình trong quá khứ (FromDate < Ngày hiện tại). Vui lòng kiểm tra lại"
                                    };
                                }

                                var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;

                                var insert = new EF.Central.GiftRedeem
                                {
                                    ItemNo = item.ItemNo,
                                    Point = req.Point,
                                    FromDate = req.FromDate,
                                    ToDate = req.ToDate,
                                    Status = 1,  // true
                                    Counter = maxCounter + 1,
                                    Pkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}",
                                };

                                db.GiftRedeems.Add(insert);
                                db.SaveChanges();

                                // Insert Table GiftRedeemModifyLog

                                var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;

                                var insertLog = new EF.Central.GiftRedeemModifyLog
                                {
                                    ItemNo = item.ItemNo,
                                    Point = req.Point,
                                    FromDate = req.FromDate,
                                    ToDate = req.ToDate,
                                    Status = true, // 1
                                    Counter = maxCounterLog + 1,
                                    Pkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}",
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = req.CreatedUser
                                };

                                db.GiftRedeemModifyLogs.Add(insertLog);
                                db.SaveChanges();
                            }
                            else  
                            {
                                if (req.FromDate.Date < DateTime.Now.Date)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = "Thêm mới không thành công. Vì ngày bắt đầu có hiệu lực nhỏ hơn ngày hiện tại. Vui lòng kiểm tra lại"
                                    };
                                }

                                var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                data.Status = Convert.ToInt16(req.Status);
                                data.Counter = maxCounter + 1;
                                data.Pkey = checkPkey;
                                db.SaveChanges();

                                var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                dataLog.Counter = maxCounterLog + 1;
                                dataLog.Pkey = checkPkey;
                                dataLog.UpdatedUser = req.CreatedUser;
                                dataLog.UpdatedDate = req.CreatedDate;
                                db.SaveChanges();

                            }
                        }

                        return new ResultResponseModel
                        {
                            Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                            Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                            Item3 = "1",
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Lưu tỷ lệ đổi quà thành công"
                        };

                    }
                }
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
        public ResultResponseModel CreateSetupLoyaltyForTabGiftRedeem(CreateSetupLoyaltyTabGiftRedeemModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var totalRow = db.GiftRedeems.Any();

                    if (!totalRow)
                    {
                        foreach (var item in req.ListItem)
                        {
                            if (req.FromDate.Date < DateTime.Now.Date)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = "Thêm mới không thành công. Vì ngày bắt đầu có hiệu lực nhỏ hơn ngày hiện tại. Vui lòng kiểm tra lại"
                                };
                            }

                            var maxCounter = db.GiftRedeems.Max(x =>x.Counter) ?? 0;
                            var insert = new EF.Central.GiftRedeem
                            {
                                ItemNo = item.ItemNo,
                                Point = req.Point,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                Status = 1, // true, co hieu luc
                                Counter = maxCounter + 1,
                                Pkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}"
                            };

                            db.GiftRedeems.Add(insert);
                            db.SaveChanges();

                            var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;
                            var insertLog = new EF.Central.GiftRedeemModifyLog
                            {
                                ItemNo = item.ItemNo,
                                Point = req.Point,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                Status = true, // 1 co hieu luc
                                Counter = maxCounterLog + 1,
                                Pkey = $"{item.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}",
                                CreatedDate = DateTime.Now,
                                CreatedUser = req.CreatedUser
                            };

                            db.GiftRedeemModifyLogs.Add(insertLog);
                            db.SaveChanges();

                        }

                        return new ResultResponseModel
                        {
                            Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                            Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                            Item3 = "1",
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Lưu tỷ lệ đổi quà thành công"
                        };

                    }
                    else  // TH: Update
                    {
                        foreach (var item in req.ListItem)
                        {
                            var _itemNo = item.ItemNo.ToString().Trim();
                            var _FromDate = DateTime.ParseExact(item.FromDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            var _ToDate = DateTime.ParseExact(item.ToDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

                            var fromDateNew = DateTime.ParseExact(req.FromDateStr.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            var toDateNew = DateTime.ParseExact(req.ToDateStr.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

                            var listItem = db.GiftRedeems.Where(x =>x.ItemNo == _itemNo);
                            var listItemLog = db.GiftRedeemModifyLogs.Where(x =>x.ItemNo == _itemNo);

                            var maxToDate = listItem.Max(d => d.ToDate);

                            if (fromDateNew <= DateTime.Now)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Message = $"Pkey {_itemNo}-{fromDateNew.ToString("yyyyMMdd")} có ngày băt đầu hiệu lực nhỏ hơn (hoặc bằng) ngày hiện tại / hoặc hết hiệu lực. Vui lòng kiểm tra lại"
                                };
                            }
                            else if (fromDateNew > DateTime.Now)
                            {                 
                                if (fromDateNew > _ToDate)  // Insert
                                {
                                    var pkeyNew = $"{_itemNo}-{fromDateNew.ToString("yyyyMMdd")}";
                                    var listItemNew = db.GiftRedeems.Where(d =>d.Pkey == pkeyNew).FirstOrDefault()?.Pkey;

                                    if (listItemNew == null || listItemNew == "")    // Insert
                                    {
                                        var maxCounter = db.GiftRedeems.Max(x =>x.Counter) ?? 0;
                                        var insert = new GiftRedeem
                                        {
                                            ItemNo = _itemNo,
                                            Point = req.Point,
                                            FromDate = fromDateNew,
                                            ToDate = toDateNew,
                                            Status = 1,
                                            Counter = maxCounter + 1,
                                            Pkey = pkeyNew
                                        };

                                        db.GiftRedeems.Add(insert);
                                        

                                        var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;
                                        var insertLog = new GiftRedeemModifyLog
                                        {
                                            ItemNo = _itemNo,
                                            Point = req.Point,
                                            FromDate = fromDateNew,
                                            ToDate = toDateNew,
                                            Status = true,          // 1 :co hieu luc
                                            Counter = maxCounterLog + 1,
                                            Pkey = pkeyNew,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser
                                        };

                                        db.GiftRedeemModifyLogs.Add(insertLog);
                                        
                                    }

                                    db.SaveChanges();
                                    return new ResultResponseModel
                                    {
                                        Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                                        Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                                        Item3 = "1",
                                        Status = Model.Enums.ResultEnum.Success,
                                        Message = "Cập nhật thành công"
                                    };

                                }
                                else if ((_ToDate <= DateTime.Now) || (_FromDate <= DateTime.Now)) // Truong hop het hieu luc/ngay bat dau co hieu luc <= ngay hien tai
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.Fail,
                                        Message = $"Pkey {_itemNo}-{_FromDate.ToString("yyyyMMdd")} có ngày băt đầu hiệu lực nhỏ hơn (hoặc bằng) ngày hiện tại / hoặc hết hiệu lực. Vui lòng kiểm tra lại"
                                    };
                                }
                                else 
                                {
                                    if (fromDateNew < _ToDate || fromDateNew == _ToDate)
                                    {
                                        return new ResultResponseModel
                                        {
                                            Status = Model.Enums.ResultEnum.Fail,
                                            Message = $"Pkey {_itemNo}-{fromDateNew.ToString("yyyyMMdd")} đã tồn tại (hoặc không thỏa điều kiện về thời gian hiệu lực) trong hệ thống. Vui lòng kiểm tra lại"
                                        };
                                    }

                                    var pkey1 = $"{_itemNo}-{fromDateNew.ToString("yyyyMMdd")}";
                                    var listData = db.GiftRedeems.Where(d =>d.Pkey == pkey1);

                                    if (listData == null)    // Insert
                                    {
                                        var maxCounter = db.GiftRedeems.Max(x =>x.Counter) ?? 0;
                                        var insert = new GiftRedeem
                                        {
                                            ItemNo = _itemNo,
                                            Point = req.Point,
                                            FromDate = fromDateNew,
                                            ToDate = toDateNew,
                                            Status = 1,
                                            Counter = Convert.ToUInt32(maxCounter) + 1,
                                            Pkey = pkey1
                                        };

                                        db.GiftRedeems.Add(insert);
                                        
                                        var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;
                                        var insertLog = new GiftRedeemModifyLog
                                        {
                                            ItemNo = _itemNo,
                                            Point = req.Point,
                                            FromDate = fromDateNew,
                                            ToDate = toDateNew,
                                            Status = true, 
                                            Counter = maxCounterLog + 1,
                                            Pkey = pkey1,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser
                                        };

                                        db.GiftRedeemModifyLogs.Add(insertLog);

                                        db.SaveChanges();
                                        return new ResultResponseModel
                                        {
                                            Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                                            Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                                            Item3 = "1",
                                            Status = Model.Enums.ResultEnum.Success,
                                            Message = "Cập nhật thành công"
                                        };

                                    }
                                    else // Update
                                    {
                                        var listGiftLog = db.GiftRedeemModifyLogs.AsNoTracking().Where(d =>d.Pkey == pkey1);
                                        var maxCounter = db.GiftRedeems.Max(x =>x.Counter) ?? 0;

                                        listData.FirstOrDefault().ItemNo = _itemNo;
                                        listData.FirstOrDefault().Point = req.Point; // Điểm mới
                                        listData.FirstOrDefault().Status = 1;        // true
                                        listData.FirstOrDefault().FromDate = fromDateNew;
                                        listData.FirstOrDefault().ToDate = toDateNew;
                                        listData.FirstOrDefault().Counter = maxCounter + 1;
                                        listData.FirstOrDefault().Pkey = pkey1;

                                        db.SaveChanges();

                                        var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;

                                        listGiftLog.FirstOrDefault().ItemNo = _itemNo;
                                        listGiftLog.FirstOrDefault().Point = req.Point;  // Điểm mới
                                        listGiftLog.FirstOrDefault().Status = true;      // 1
                                        listGiftLog.FirstOrDefault().FromDate = fromDateNew;
                                        listGiftLog.FirstOrDefault().ToDate = toDateNew;
                                        listGiftLog.FirstOrDefault().Counter = maxCounterLog + 1;
                                        listGiftLog.FirstOrDefault().Pkey = pkey1;
                                        listGiftLog.FirstOrDefault().UpdatedDate = DateTime.Now;
                                        listGiftLog.FirstOrDefault().UpdatedUser = req.UpdatedUser;

                                        db.SaveChanges();
                                    }                    
                                }                               
                            }  
                        }

                        return new ResultResponseModel
                        {
                            Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                            Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                            Item3 = "1",
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Cập nhật thành công"
                        };
                    }
                } 
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
        public ResultResponseModel UpdateSetupLoyalty(UpdateSetupLoyaltyModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;                    
                    var totalRow = db.GiftRedeems.Any();

                    if (!totalRow)
                    {
                        var maxCounter = db.GiftRedeems.Max(x =>x.Counter) ?? 0;
                        var toDate = req.ToDate;

                        var insert = new EF.Central.GiftRedeem
                        {
                            ItemNo = req.ItemNo,
                            Point = req.Point,
                            FromDate = req.FromDate,
                            ToDate = toDate,
                            Status = Convert.ToInt16(req.Status), 
                            Counter = maxCounter + 1,
                            Pkey = $"{req.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}"
                        };

                        db.GiftRedeems.Add(insert);
                        db.SaveChanges();

                        // Insert Table GiftRedeemModifyLog

                        var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;
                        var insertLog = new GiftRedeemModifyLog
                        {
                            ItemNo = req.ItemNo,
                            Point = req.Point,
                            FromDate = req.FromDate,
                            ToDate = req.ToDate,
                            Status = req.Status == 1 ? true:false,
                            Counter = maxCounterLog + 1,
                            Pkey = $"{req.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}",
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser
                        };

                        db.GiftRedeemModifyLogs.Add(insertLog);
                        db.SaveChanges();

                        return new ResultResponseModel
                        {
                            Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                            Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                            Item3 = req.Status == 1 ? "1" : "0",
                            Item4 = req.ItemNo,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Cập nhật thành công"
                        };
                    }
                    else
                    {                       
                        var checkPkey = req.Pkey;
                        var data = db.GiftRedeems.FirstOrDefault(x =>x.Pkey == checkPkey);
                        var dataLog = db.GiftRedeemModifyLogs.FirstOrDefault(x =>x.Pkey == checkPkey);

                        var toDate = req.ToDate;
                        var yearToDate = DateTime.Parse(Convert.ToString(toDate), new CultureInfo("en-US")).Year;
                        var isStatus = req.Status;

                        if (data == null) 
                        {
                            var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                            var insert = new GiftRedeem
                            {
                                ItemNo = req.ItemNo,
                                Point = req.Point,
                                FromDate = req.FromDate,
                                ToDate = toDate,
                                Status = Convert.ToInt16(req.Status),
                                Counter = maxCounter + 1,
                                Pkey = $"{req.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}"
                            };

                            db.GiftRedeems.Add(insert);
                            db.SaveChanges();

                            // Insert Table GiftRedeemModifyLog

                            var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;
                            var insertLog = new GiftRedeemModifyLog
                            {
                                ItemNo = req.ItemNo,
                                Point = req.Point,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                Status = req.Status == 1 ? true : false,
                                Counter = maxCounterLog + 1,
                                Pkey = $"{req.ItemNo}-{req.FromDate.ToString("yyyyMMdd")}",
                                UpdatedDate = DateTime.Now,
                                UpdatedUser = req.CreatedUser
                            };

                            db.GiftRedeemModifyLogs.Add(insertLog);
                            db.SaveChanges();

                            return new ResultResponseModel
                            {
                                Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                                Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                                Item3 = req.Status == 1 ? "1" : "0",
                                Item4 = req.ItemNo,
                                Status = Model.Enums.ResultEnum.Success,
                                Message = "Cập nhật thành công"
                            };
                        }
                        else 
                        {
                            // Update: Table GiftRedeem

                            var maxCounter = db.GiftRedeems.Max(x =>x.Counter) ?? 0;
                            data.Point = req.Point;
                            data.ToDate = toDate;
                            data.Status = Convert.ToInt16(req.Status);
                            data.Counter = maxCounter + 1;
                            db.SaveChanges();

                            // Update: Table GiftRedeemModifyLog

                            var maxCounterLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;
                            dataLog.Point = req.Point;
                            dataLog.Counter = maxCounterLog + 1;
                            dataLog.Pkey = checkPkey;
                            dataLog.UpdatedUser = req.UpdatedUser;
                            dataLog.UpdatedDate = req.UpdatedDate;
                            db.SaveChanges();

                            return new ResultResponseModel
                            {
                                Item1 = req.FromDate.ToString("dd/MM/yyyy"),
                                Item2 = req.ToDate.ToString("dd/MM/yyyy"),
                                Item3 = req.Status == 1 ? "1" : "0",
                                Item4 = req.ItemNo,
                                Status = Model.Enums.ResultEnum.Success,
                                Message = "Cập nhật thành công"
                            };
                        }
                    }
                }
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

        public ResultResponseModel DeleteSetupLoyalty(DeleteLoyaltyModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var dataGift = db.GiftRedeems.FirstOrDefault(x => x.Pkey == req.Pkey);
                    var dataGiftLog = db.GiftRedeemModifyLogs.FirstOrDefault(x => x.Pkey == req.Pkey);

                    if (dataGift == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var fromDate = dataGift.FromDate;
                    var toDate = dataGift.ToDate;

                    if (fromDate < DateTime.Now || fromDate == DateTime.Now)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Không được xóa, vì ngày bắt đầu có hiệu lực {fromDate} này nhỏ hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại"
                        };
                    }

                    db.GiftRedeems.Remove(dataGift);
                    db.GiftRedeemModifyLogs.Remove(dataGiftLog);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa tỷ lệ đổi quà thành công"
                    };
                }
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


        public Tuple<int, string> ImportExcelGiftRedeem(string userName, DataTable dt)
        {
            // Trong 1 khoang thoi gian thi chi co duy nhat 01 ty le doi qua ap dung cho 1 ItemNo

            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 5 * 60;

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        //var data = db.GiftRedeems.AsNoTracking().Select(x => x.Id).FirstOrDefault();

                        var data = db.GiftRedeems.Any();

                        if (dt.Rows.Count > 0)
                        {
                            if (data.ToString() == null || data.ToString() == "")
                            {
                                for (int j = 0; j < dt.Rows.Count; j++)
                                {
                                    var excel_ItemNo = dt.Rows[j]["ItemNo"].ToString().Trim();
                                    var excel_PointRedeem = dt.Rows[j]["PointRedeem"].ToString().Trim();
                                    var intPointRedeem = Int32.Parse(dt.Rows[j]["PointRedeem"].ToString().Trim());
                                    var excel_FromDate = DateTime.ParseExact(dt.Rows[j]["FromDate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                    var excel_ToDate = DateTime.ParseExact(dt.Rows[j]["ToDate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

                                    if (excel_FromDate.Date <= DateTime.Now.Date)
                                    {
                                        return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không được phép cập nhật. Vì ngày bắt đầu có hiệu lực nhỏ hơn (hoặc bằng) ngày hiện tại. Vui lòng kiểm tra lại");
                                    }

                                    var maxToDate = DateTime.Now;
                                    var status = 1;
                                    if (j == 0)
                                    {
                                        maxToDate = DateTime.Now;
                                        status = 1;
                                    }
                                    else
                                    {
                                        var listItem = db.GiftRedeems.Where(d => d.ItemNo == excel_ItemNo).ToList();
                                        var _maxToDate = listItem.Max(d => d.ToDate);
                                        if (_maxToDate == null)
                                        {
                                            maxToDate = DateTime.Now;
                                            status = 1;
                                        }
                                        else
                                        {
                                            maxToDate = Convert.ToDateTime(_maxToDate.ToString());
                                            var _status = listItem.Where(d => d.ToDate == maxToDate).FirstOrDefault().Status;
                                            status = int.Parse(_status.ToString());
                                        }
                                    }

                                    var _pkey = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                    var pkey = db.GiftRedeems.FirstOrDefault(d => d.Pkey == _pkey);
                                    var pkeyLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == _pkey);
                                    var _maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;

                                    if (excel_ToDate < DateTime.Now.Date) // Ngày kết thúc hiệu lực < Ngày hiện tại
                                    {
                                        if (pkey != null)
                                        {
                                            var _maxCounterUpdate = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                            pkey.ItemNo = excel_ItemNo;
                                            pkey.Point = int.Parse(excel_PointRedeem);
                                            pkey.FromDate = excel_FromDate;
                                            pkey.ToDate = excel_ToDate;
                                            pkey.Status = 0;  // het hieu luc
                                            pkey.Counter = Convert.ToInt32(_maxCounterUpdate) + 1;
                                            pkey.Pkey = _pkey;

                                            db.SaveChanges();

                                            var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                            pkeyLog.ItemNo = excel_ItemNo;
                                            pkeyLog.Point = int.Parse(excel_PointRedeem);
                                            pkeyLog.FromDate = excel_FromDate;
                                            pkeyLog.ToDate = excel_ToDate;
                                            pkeyLog.Status = false;  // het hieu luc
                                            pkeyLog.Counter = Convert.ToInt32(_maxCounterLog) + 1;
                                            pkeyLog.Pkey = _pkey;
                                            pkeyLog.UpdatedDate = DateTime.Now;
                                            pkeyLog.UpdatedUser = userName;

                                            db.SaveChanges();
                                        }
                                        else
                                        {
                                            return new Tuple<int, string>(0, $"Pkey {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} này không tồn tại trong hệ thống. Vui lòng kiểm tra lại");
                                        }
                                    }
                                    else if (excel_ToDate >= DateTime.Now.Date) // Ngày kết thúc hiệu lực lớn hơn hoặc bằng Ngày hiện tại
                                    {
                                        if (pkey == null)
                                        {
                                            // Check theo ItemNo
                                            var checkItem = db.GiftRedeems.Where(d => d.ItemNo == excel_ItemNo && d.FromDate == excel_FromDate).ToList();

                                            if (checkItem.Count > 0)
                                            {
                                                foreach (var item in checkItem)
                                                {
                                                    var istoDate = item.ToDate;
                                                    if (excel_FromDate <= istoDate)
                                                    {
                                                        return new Tuple<int, string>(0, $"Pkey {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} này bị trùng. Vui lòng kiểm tra lại");
                                                    }
                                                }
                                            }

                                            var insert = db.GiftRedeems.Add(new GiftRedeem
                                            {
                                                ItemNo = excel_ItemNo,
                                                Point = int.Parse(excel_PointRedeem),
                                                FromDate = excel_FromDate,
                                                ToDate = excel_ToDate,
                                                Status = 1, // true
                                                Counter = Convert.ToInt32(_maxCounter) + 1,
                                                Pkey = _pkey
                                            });

                                            db.GiftRedeems.Add(insert);
                                            db.SaveChanges();

                                            var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                            var insertLog = new EF.Central.GiftRedeemModifyLog
                                            {
                                                ItemNo = excel_ItemNo,
                                                Point = int.Parse(excel_PointRedeem),
                                                FromDate = excel_FromDate,
                                                ToDate = excel_ToDate,
                                                Status = true, // 1
                                                Counter = _maxCounterLog + 1,
                                                Pkey = _pkey,
                                                CreatedDate = DateTime.Now,
                                                CreatedUser = userName
                                            };

                                            db.GiftRedeemModifyLogs.Add(insertLog);
                                            db.SaveChanges();

                                        }
                                        else // Update
                                        {
                                            var _maxCounterUpdate = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                            pkey.ItemNo = excel_ItemNo;
                                            pkey.Point = int.Parse(excel_PointRedeem);
                                            pkey.FromDate = excel_FromDate;
                                            pkey.ToDate = excel_ToDate;
                                            pkey.Status = 1; // true
                                            pkey.Counter = Convert.ToInt32(_maxCounterUpdate) + 1;
                                            pkey.Pkey = _pkey;

                                            db.SaveChanges();

                                            var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
     
                                            pkeyLog.ItemNo = excel_ItemNo;
                                            pkeyLog.Point = int.Parse(excel_PointRedeem);
                                            pkeyLog.FromDate = excel_FromDate;
                                            pkeyLog.ToDate = excel_ToDate;
                                            pkeyLog.Status = true; // 1
                                            pkeyLog.Counter = Convert.ToInt32(_maxCounterLog) + 1;
                                            pkeyLog.Pkey = _pkey;
                                            pkeyLog.UpdatedDate = DateTime.Now;
                                            pkeyLog.UpdatedUser = userName;

                                            db.SaveChanges();
                                        }
                                    }
                                    db.SaveChanges();
                                }
                                transaction.Commit();
                            }
                            else if (data.ToString() != null || data.ToString() != "")
                            {
                                for (int j = 0; j < dt.Rows.Count; j++)
                                {
                                    var excel_ItemNo = dt.Rows[j]["ItemNo"].ToString().Trim();
                                    var excel_PointRedeem = dt.Rows[j]["PointRedeem"].ToString().Trim();
                                    var intPointRedeem = Int32.Parse(dt.Rows[j]["PointRedeem"].ToString().Trim());
                                    var excel_FromDate = DateTime.ParseExact(dt.Rows[j]["FromDate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                    var excel_ToDate = DateTime.ParseExact(dt.Rows[j]["ToDate"].ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

                                    var listItemNo = db.GiftRedeems.Where(d => d.ItemNo == excel_ItemNo).ToList();
                                    var maxFromDate = listItemNo.Max(d => d.FromDate);

                                    if (excel_FromDate < DateTime.Now.Date)
                                    {
                                        return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không được phép cập nhật. Vì ngày bắt đầu có hiệu lực nhỏ hơn (hoặc bằng) ngày hiện tại. Vui lòng kiểm tra lại");
                                    }

                                    var listItem = db.GiftRedeems.Where(d => d.ItemNo == excel_ItemNo).ToList();
                                    var maxToDate = DateTime.Now;
                                    var status = 1;
                                    var _maxToDate = listItem.Max(d => d.ToDate);

                                    if (_maxToDate == null)
                                    {
                                        maxToDate = DateTime.Now;
                                        status = 1;
                                    }
                                    else
                                    {
                                        maxToDate = Convert.ToDateTime(_maxToDate.ToString());
                                        var _status = listItem.Where(d => d.ToDate == maxToDate).FirstOrDefault().Status;
                                        status = int.Parse(_status.ToString());
                                    }

                                    if (listItem.Count > 0)  // TH: Update
                                    {
                                        if ((excel_ToDate >= excel_FromDate) && (excel_FromDate > DateTime.Now.Date))
                                        {
                                            if (excel_FromDate >= maxFromDate)
                                            {
                                                if (excel_ToDate <= maxToDate)
                                                {
                                                    // Không cho cập nhật chương trình mới, mà chỉ cho cập nhật điểm quy đổi nếu có sự thay đổi vể điẻm
                                                    // return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} đã tồn tại (hoặc không thỏa về thời gian hiệu lực) trong hệ thống. Vui lòng kiểm tra lại");

                                                    var pkeyUpdate1 = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                                    var dataUpdate1 = db.GiftRedeems.FirstOrDefault(d => d.Pkey == pkeyUpdate1);

                                                    if (dataUpdate1 == null)
                                                    {
                                                        return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không tồn tại trong hệ thống. Vui lòng kiểm tra lại");
                                                    }

                                                    if (excel_FromDate == dataUpdate1.FromDate && excel_ToDate == dataUpdate1.ToDate)
                                                    {
                                                        var dataUpdateLog1 = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == pkeyUpdate1);
                                                        var maxCounter1 = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                                        dataUpdate1.ItemNo = excel_ItemNo;
                                                        dataUpdate1.Point = intPointRedeem;
                                                        dataUpdate1.Status = 1;    // Có hiệu lực
                                                        dataUpdate1.Counter = Convert.ToInt32(maxCounter1) + 1;

                                                        db.SaveChanges();

                                                        var maxCounterLog1 = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                        dataUpdateLog1.ItemNo = excel_ItemNo;
                                                        dataUpdateLog1.Point = intPointRedeem;
                                                        dataUpdateLog1.Status = true;  // Có hieu luc
                                                        dataUpdateLog1.Counter = Convert.ToInt32(maxCounterLog1) + 1;
                                                        dataUpdateLog1.UpdatedDate = DateTime.Now;
                                                        dataUpdateLog1.UpdatedUser = userName;

                                                        db.SaveChanges();
                                                    }
                                                }
                                            }
                                        }

                                        // Trường hợp có hiệu lực
                                        if ((excel_ToDate > DateTime.Now.Date) || (excel_ToDate == DateTime.Now.Date))
                                        {
                                            // Cho phep cap nhat khi ngay bat bau co hieu luc > Ngay hien tai
                                            if ((excel_FromDate > DateTime.Now.Date) && (excel_FromDate <= excel_ToDate))
                                            {
                                                var checkPkey = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                                var listData = db.GiftRedeems.FirstOrDefault(d => d.Pkey == checkPkey);

                                                if (listData == null) // Insert
                                                {
                                                    var maxCounter = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                    var Insert = db.GiftRedeems.Add(new GiftRedeem
                                                    {
                                                        ItemNo = excel_ItemNo,
                                                        Point = int.Parse(excel_PointRedeem),
                                                        FromDate = excel_FromDate,
                                                        ToDate = excel_ToDate,
                                                        Status = 1,             // Co hieu luc
                                                        Counter = Convert.ToInt32(maxCounter) + 1,
                                                        Pkey = checkPkey
                                                    });

                                                    db.GiftRedeems.Add(Insert);
                                                    db.SaveChanges();

                                                    var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                    var InsertLog = new EF.Central.GiftRedeemModifyLog
                                                    {
                                                        ItemNo = excel_ItemNo,
                                                        Point = int.Parse(excel_PointRedeem),
                                                        FromDate = excel_FromDate,
                                                        ToDate = excel_ToDate,
                                                        Status = true,      // Co hieu luc
                                                        Counter = _maxCounterLog + 1,
                                                        Pkey = checkPkey,
                                                        CreatedDate = DateTime.Now,
                                                        CreatedUser = userName
                                                    };

                                                    db.GiftRedeemModifyLogs.Add(InsertLog);
                                                    db.SaveChanges();
                                                }
                                                else
                                                {
                                                    var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                                    listData.ItemNo = excel_ItemNo;
                                                    listData.Point = int.Parse(excel_PointRedeem);
                                                    listData.FromDate = excel_FromDate;
                                                    listData.ToDate = excel_ToDate;
                                                    listData.Status = 1;            // Co hieu luc
                                                    listData.Counter = Convert.ToInt32(maxCounter) + 1;
                                                    listData.Pkey = checkPkey;

                                                    db.SaveChanges();

                                                    var dataUpdateLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == checkPkey);
                                                    var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                    dataUpdateLog.ItemNo = excel_ItemNo;
                                                    dataUpdateLog.Point = int.Parse(excel_PointRedeem);
                                                    dataUpdateLog.FromDate = excel_FromDate;
                                                    dataUpdateLog.ToDate = excel_ToDate;
                                                    dataUpdateLog.Status = true;            // co hieu luc
                                                    dataUpdateLog.Counter = Convert.ToInt32(maxCounterLog) + 1;
                                                    dataUpdateLog.Pkey = checkPkey;
                                                    dataUpdateLog.UpdatedDate = DateTime.Now;
                                                    dataUpdateLog.UpdatedUser = userName;

                                                    db.SaveChanges();
                                                }
                                            }
                                            else
                                            {
                                                if (excel_ToDate < excel_FromDate) // cap nhat cac sp ve het hieu luc
                                                {
                                                    var pkeyUpdate = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                                    var dataUpdate = db.GiftRedeems.FirstOrDefault(d => d.Pkey == pkeyUpdate);

                                                    if (dataUpdate == null)
                                                    {
                                                        return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không tồn tại chương trình này trong hệ thống. Vui lòng kiểm tra lại");
                                                    }

                                                    var dataUpdateLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == pkeyUpdate);
                                                    var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;  
                                                    dataUpdate.ItemNo = excel_ItemNo;
                                                    dataUpdate.ToDate = excel_ToDate;
                                                    dataUpdate.Status = 0;    // Het hieu luc
                                                    dataUpdate.Counter = Convert.ToInt32(maxCounter) + 1;

                                                    db.SaveChanges();

                                                    var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                    dataUpdateLog.ItemNo = excel_ItemNo;
                                                    dataUpdateLog.ToDate = excel_ToDate;
                                                    dataUpdateLog.Status = false;  // Het hieu luc
                                                    dataUpdateLog.Counter = Convert.ToInt32(maxCounterLog) + 1;
                                                    dataUpdateLog.UpdatedDate = DateTime.Now;
                                                    dataUpdateLog.UpdatedUser = userName;

                                                    db.SaveChanges();

                                                }
                                                else // TH: FromDate > Datetime.Now và ToDate > Datetime.Now và ToDate > = FromDate
                                                {
                                                    var pkeyUpdate1 = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                                    var dataUpdate1 = db.GiftRedeems.FirstOrDefault(d => d.Pkey == pkeyUpdate1);

                                                    // Cập nhật điểm quy đổi ( FromDate, ToDate không được thay đổi trong file import, chi thay đổi điểm
                                                    if (excel_FromDate > DateTime.Now.Date)
                                                    {
                                                        if (dataUpdate1 == null)
                                                        {
                                                            return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không tồn tại trong hệ thống. Vui lòng kiểm tra lại");
                                                        }

                                                        if (excel_FromDate == dataUpdate1.FromDate && excel_ToDate == dataUpdate1.ToDate)
                                                        {                                                           
                                                            var dataUpdateLog1 = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == pkeyUpdate1);
                                                            var maxCounter1 = db.GiftRedeems.Max(x => x.Counter) ?? 0; 
                                                            
                                                            dataUpdate1.ItemNo = excel_ItemNo;
                                                            dataUpdate1.Point = intPointRedeem;
                                                            dataUpdate1.Status = 1;    // Có hiệu lực
                                                            dataUpdate1.Counter = Convert.ToInt32(maxCounter1) + 1;

                                                            db.SaveChanges();

                                                            var maxCounterLog1 = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;

                                                            dataUpdateLog1.ItemNo = excel_ItemNo;
                                                            dataUpdateLog1.Point = intPointRedeem;
                                                            dataUpdateLog1.Status = true;  // Có hieu luc
                                                            dataUpdateLog1.Counter = Convert.ToInt32(maxCounterLog1) + 1;
                                                            dataUpdateLog1.UpdatedDate = DateTime.Now;
                                                            dataUpdateLog1.UpdatedUser = userName;

                                                            db.SaveChanges();
                                                        }
                                                    }

                                                    if (excel_ToDate <= maxToDate)
                                                    {
                                                        return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} đã tồn tại (hoặc không thỏa về thời gian hiệu lực) trong hệ thống. Vui lòng kiểm tra lại");
                                                    }

                                                    var pkeyUpdate = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                                    var dataUpdate = db.GiftRedeems.FirstOrDefault(d => d.Pkey == pkeyUpdate);

                                                    if (dataUpdate == null)
                                                    {
                                                        return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không tồn tại chương trình này trong hệ thống. Vui lòng kiểm tra lại");
                                                    }

                                                    var dataUpdateLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == pkeyUpdate);
                                                    var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                                    dataUpdate.ItemNo = excel_ItemNo;
                                                    dataUpdate.Point = intPointRedeem;
                                                    dataUpdate.ToDate = excel_ToDate;
                                                    dataUpdate.Status = 1;    // Có hiệu lực
                                                    dataUpdate.Counter = Convert.ToInt32(maxCounter) + 1;

                                                    db.SaveChanges();

                                                    var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                    dataUpdateLog.ItemNo = excel_ItemNo;
                                                    dataUpdateLog.Point = intPointRedeem;
                                                    dataUpdateLog.ToDate = excel_ToDate;
                                                    dataUpdateLog.Status = true;  // Có hieu luc
                                                    dataUpdateLog.Counter = Convert.ToInt32(maxCounterLog) + 1;
                                                    dataUpdateLog.UpdatedDate = DateTime.Now;
                                                    dataUpdateLog.UpdatedUser = userName;

                                                    db.SaveChanges();
                                                }
                                            }
                                        }
                                        else if (excel_ToDate < DateTime.Now.Date) // TH: update het hieu luc, khong cap nhat : PointRedeem,FromDate,Pkey
                                        {
                                            var pkeyUpdate = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                            var dataUpdate = db.GiftRedeems.FirstOrDefault(d => d.Pkey == pkeyUpdate);

                                            if (dataUpdate == null)
                                            {
                                                return new Tuple<int, string>(0, $"Pkey: {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không tồn tại chương trình này trong hệ thống. Vui lòng kiểm tra lại");
                                            }

                                            var dataUpdateLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == pkeyUpdate);
                                            var maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;

                                            dataUpdate.ItemNo = excel_ItemNo;
                                            dataUpdate.ToDate = excel_ToDate;
                                            dataUpdate.Status = 0; // Het hieu luc
                                            dataUpdate.Counter = Convert.ToInt32(maxCounter) + 1;

                                            db.SaveChanges();

                                            var maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;

                                            dataUpdateLog.ItemNo = excel_ItemNo;
                                            dataUpdateLog.ToDate = excel_ToDate;
                                            dataUpdateLog.Status = false;  // Het hieu luc
                                            dataUpdateLog.Counter = Convert.ToInt32(maxCounterLog) + 1;
                                            dataUpdateLog.UpdatedDate = DateTime.Now;
                                            dataUpdateLog.UpdatedUser = userName;

                                            db.SaveChanges();
                                        }
                                    }
                                    else  // Insert
                                    {
                                        var _pkey = $"{excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")}";
                                        var pkey = db.GiftRedeems.FirstOrDefault(d => d.Pkey == _pkey);
                                        var pkeyLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == _pkey);
                                        var _maxCounter = db.GiftRedeems.Max(x => x.Counter) ?? 0;

                                        if (excel_ToDate < DateTime.Now.Date)  // Truong hop het hieu luc
                                        {
                                            if (pkey != null)
                                            {
                                                var _maxCounterUpdate = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                                pkey.ItemNo = excel_ItemNo;
                                                pkey.Point = int.Parse(excel_PointRedeem);
                                                pkey.FromDate = excel_FromDate;
                                                pkey.ToDate = excel_ToDate;
                                                pkey.Status = 0; 
                                                pkey.Counter = Convert.ToInt32(_maxCounterUpdate) + 1;
                                                pkey.Pkey = _pkey;

                                                db.SaveChanges();

                                                var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                pkeyLog.ItemNo = excel_ItemNo;
                                                pkeyLog.Point = int.Parse(excel_PointRedeem);
                                                pkeyLog.FromDate = excel_FromDate;
                                                pkeyLog.ToDate = excel_ToDate;
                                                pkeyLog.Status = false; 
                                                pkeyLog.Counter = Convert.ToInt32(_maxCounterLog) + 1;
                                                pkeyLog.Pkey = _pkey;
                                                pkeyLog.UpdatedDate = DateTime.Now;
                                                pkeyLog.UpdatedUser = userName;

                                                db.SaveChanges();

                                            }
                                            else
                                            {
                                                return new Tuple<int, string>(0, $"Pkey {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} không tồn tại trong hệ thống. Vui lòng kiểm tra lại");
                                            }
                                        }
                                        else if (excel_ToDate >= DateTime.Now.Date) // Truong hop con hieu luc
                                        {
                                            if (pkey == null) 
                                            {
                                                var checkItem = db.GiftRedeems.Where(d => d.ItemNo == excel_ItemNo && d.FromDate == excel_FromDate).ToList();

                                                if (checkItem.Count > 0)
                                                {
                                                    foreach (var item in checkItem)
                                                    {
                                                        var istoDate = item.ToDate;
                                                        if (excel_FromDate <= istoDate)
                                                        {
                                                            return new Tuple<int, string>(0, $"Pkey {excel_ItemNo}-{excel_FromDate.ToString("yyyyMMdd")} này bị trùng. Vui lòng kiểm tra lại file import excel");
                                                        }
                                                    }
                                                }

                                                var insert = db.GiftRedeems.Add(new GiftRedeem
                                                {
                                                    ItemNo = excel_ItemNo,
                                                    Point = int.Parse(excel_PointRedeem),
                                                    FromDate = excel_FromDate,
                                                    ToDate = excel_ToDate,
                                                    Status = 1,
                                                    Counter = Convert.ToInt32(_maxCounter) + 1,
                                                    Pkey = _pkey
                                                });

                                                db.GiftRedeems.Add(insert);
                                                db.SaveChanges();

                                                var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;
                                                var insertLog = new EF.Central.GiftRedeemModifyLog
                                                {
                                                    ItemNo = excel_ItemNo,
                                                    Point = int.Parse(excel_PointRedeem),
                                                    FromDate = excel_FromDate,
                                                    ToDate = excel_ToDate,
                                                    Status = true,
                                                    Counter = _maxCounterLog + 1,
                                                    Pkey = _pkey,
                                                    CreatedDate = DateTime.Now,
                                                    CreatedUser = userName
                                                };

                                                db.GiftRedeemModifyLogs.Add(insertLog);
                                                db.SaveChanges();

                                            }
                                            else // Update
                                            {
                                                var _maxCounterUpdate = db.GiftRedeems.Max(x => x.Counter) ?? 0;
                                                
                                                pkey.ItemNo = excel_ItemNo;
                                                pkey.Point = int.Parse(excel_PointRedeem);
                                                pkey.FromDate = excel_FromDate;
                                                pkey.ToDate = excel_ToDate;
                                                pkey.Status = 1; // true
                                                pkey.Counter = Convert.ToInt32(_maxCounterUpdate) + 1;
                                                pkey.Pkey = _pkey;

                                                db.SaveChanges();

                                                var _maxCounterLog = db.GiftRedeemModifyLogs.Max(x => x.Counter) ?? 0;

                                                pkeyLog.ItemNo = excel_ItemNo;
                                                pkeyLog.Point = int.Parse(excel_PointRedeem);
                                                pkeyLog.FromDate = excel_FromDate;
                                                pkeyLog.ToDate = excel_ToDate;
                                                pkeyLog.Status = true;
                                                pkeyLog.Counter = Convert.ToInt32(_maxCounterLog) + 1;
                                                pkeyLog.Pkey = _pkey;
                                                pkeyLog.UpdatedDate = DateTime.Now;
                                                pkeyLog.UpdatedUser = userName;

                                                db.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                        return new Tuple<int, string>(dt.Rows.Count, "Import file excel thành công");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new Tuple<int, string>(0, "Lỗi hệ thống, Import file excel không thành công");
                    }
                }
            }
        }

        //public Tuple<int, string> ImportExcelGiftRedeem_V2(string userName, List<ImportExcelGiftRedeemModel> listData)
        //{
        //    using (var db = new CentralMDPartnerContainer())
        //    {
        //        db.Database.CommandTimeout = 5 * 60;

        //        using (var transaction = db.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                var data = db.GiftRedeems.Any();

        //                if (!data) // = null --- > thêm mới
        //                {
        //                    var insertGiftRedeem = listData.Select(x => new GiftRedeem
        //                    {
        //                        ItemNo = x.ItemNo,
        //                        Point = int.Parse(x.PointRedeemStr.ToString()),
        //                        FromDate = x.FromDate,
        //                        ToDate = x.ToDate,
        //                        Status = 1, // true
        //                        Counter = 1,
        //                        Pkey = $"{x.ItemNo}-{x.FromDateStr}-{x.ClubCode}",
        //                        ClubCode = x.ClubCode
        //                    }).ToList();

        //                    db.GiftRedeems.AddRange(insertGiftRedeem);

        //                    var insertLog = listData.Select(x => new GiftRedeemModifyLog
        //                    {
        //                        ItemNo = x.ItemNo,
        //                        Point = int.Parse(x.PointRedeemStr.ToString()),
        //                        FromDate = x.FromDate,
        //                        ToDate = x.ToDate,
        //                        Status = true, // 1
        //                        Counter = 1,
        //                        Pkey = $"{x.ItemNo}-{x.FromDateStr}-{x.ClubCode}",
        //                        CreatedDate = DateTime.Now,
        //                        CreatedUser = userName,
        //                        ClubCode = x.ClubCode
        //                    }).ToList();

        //                    db.GiftRedeemModifyLogs.AddRange(insertLog);
        //                }

        //                var maxCounter_GiftRedeem = db.GiftRedeems.Max(x =>x.Counter) ?? 0;
        //                var maxCounter_GiftRedeemLog = db.GiftRedeemModifyLogs.Max(x =>x.Counter) ?? 0;

        //                foreach (var item in listData)
        //                {
        //                    var pkey = $"{item.ItemNo}-{item.FromDateStr}-{item.ClubCode}";
        //                    var listGiftRedeem = db.GiftRedeems.FirstOrDefault(d => d.Pkey == pkey);
        //                    if (listGiftRedeem == null)
        //                    {
        //                        // insert
        //                        var insert = db.GiftRedeems.Add(new GiftRedeem
        //                        {
        //                            ItemNo = item.ItemNo,
        //                            Point = int.Parse(item.PointRedeemStr.ToString()),
        //                            FromDate = item.FromDate,
        //                            ToDate = item.ToDate,
        //                            Status = 1,                 // true : có hiệu lực
        //                            Counter = Convert.ToInt32(maxCounter_GiftRedeem) + 1,
        //                            Pkey = pkey,
        //                            ClubCode = item.ClubCode
        //                        });

        //                        db.GiftRedeems.Add(insert);
        //                    }
        //                    else
        //                    {
        //                        // Update : cap nhat ve het hieu luc ---> sau do cap nhat thong tin moi va co hieu luc
                                 
        //                        listGiftRedeem.Status = 0;    // Het hieu luc
                               
        //                        //---------------------------------------

        //                        listGiftRedeem.ItemNo = item.ItemNo;
        //                        listGiftRedeem.Point = int.Parse(item.PointRedeemStr.ToString());
        //                        listGiftRedeem.FromDate = item.FromDate;
        //                        listGiftRedeem.ToDate = item.ToDate;                                
        //                        listGiftRedeem.Status = 1;    // Co hieu luc
        //                        listGiftRedeem.Counter = Convert.ToInt32(maxCounter_GiftRedeem) + 1;
        //                        listGiftRedeem.ClubCode = item.ClubCode;
        //                    }

        //                    var listGiftRedeemLog = db.GiftRedeemModifyLogs.FirstOrDefault(d => d.Pkey == pkey);
        //                    if (listGiftRedeemLog == null)
        //                    {
        //                        // insert
        //                        var insertLog = new EF.Central.GiftRedeemModifyLog
        //                        {
        //                            ItemNo = item.ItemNo,
        //                            Point = int.Parse(item.PointRedeemStr.ToString()),
        //                            FromDate = item.FromDate,
        //                            ToDate = item.ToDate,
        //                            Status = true,  // 1
        //                            Counter = Convert.ToInt32(maxCounter_GiftRedeemLog) + 1,
        //                            Pkey = pkey,
        //                            CreatedDate = DateTime.Now,
        //                            CreatedUser = userName,
        //                            ClubCode = item.ClubCode
        //                        };

        //                        db.GiftRedeemModifyLogs.Add(insertLog);
        //                    }
        //                    else
        //                    {
        //                        // Update : cap nhat ve het hieu luc ---> sau do cap nhat thong tin moi va co hieu luc

        //                        listGiftRedeemLog.Status = false;    // Het hieu luc

        //                        //---------------------------------------

        //                        listGiftRedeemLog.ItemNo = item.ItemNo;
        //                        listGiftRedeemLog.Point = int.Parse(item.PointRedeemStr.ToString());
        //                        listGiftRedeemLog.FromDate = item.FromDate;
        //                        listGiftRedeemLog.ToDate = item.ToDate;
        //                        listGiftRedeemLog.Status = true;    // Co hieu luc
        //                        listGiftRedeemLog.Counter = Convert.ToInt32(maxCounter_GiftRedeemLog) + 1;
        //                        listGiftRedeemLog.ClubCode = item.ClubCode;

        //                    }
        //                }

        //                db.SaveChanges();
        //                transaction.Commit();
        //                return new Tuple<int, string>(listData.Count, "Import file excel thành công");
        //                //return new Tuple<int, string>(dt.Rows.Count, "Import file excel thành công");

        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                return new Tuple<int, string>(0, "Lỗi hệ thống, Import file excel không thành công"); 
        //            }

        //        }
        //    }
        //}

        public List<ExportExcelGiftRedeemResponseModel> ExportExcelGiftRedeemList(DateTime fromDate, DateTime toDate, string itemNo, string status)
        {
            try
            {
                var data = new List<ExportExcelGiftRedeemResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportExcelGiftRedeemResponseModel>("[dbo].[GET_GIFT_REDEEM_LOYALTY_LIST] @FromDate, @ToDate, @ItemNo, @Status",
                    new SqlParameter("@FromDate", fromDate),
                    new SqlParameter("@ToDate", toDate),
                    new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                    new SqlParameter("@Status", status)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelGiftRedeemResponseModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelGiftRedeemResponseModel>();
            }
        }

        //--- 22/05/2024 ---

        public List<MemberEarnItemResponseModel> SetupMemberEarnItemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<MemberEarnItemResponseModel>("[dbo].[GetMemberEarnItemList] @TextSearch, @Status, @Exp, @PageSize, @PageNumber",
                    new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                    new SqlParameter("@Status", status ?? string.Empty),
                    new SqlParameter("@Exp", '1'),
                    new SqlParameter("@PageSize", pageSize),
                    new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<MemberEarnItemResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<MemberEarnItemResponseModel>();
            }
        }

        public List<ExportMemberEarnItemResponseModel> ExportExcel_MemberEarnItemList(string textSearch, string status)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.Database.SqlQuery<ExportMemberEarnItemResponseModel>("[dbo].[GetMemberEarnItemList] @TextSearch, @Status, @Exp, @PageSize, @PageNumber",
                    new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                    new SqlParameter("@Status", status ?? string.Empty),
                    new SqlParameter("@Exp", '2'),
                    new SqlParameter("@PageSize", string.Empty),
                    new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportMemberEarnItemResponseModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportMemberEarnItemResponseModel>();
            }
        }

        public ResultResponseModel DeleteSetupMemberEarnItem(DeleteSetupMemberEarnItemModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var data = db.MemberEarnItems.FirstOrDefault(x => x.ItemNo == req.ItemNo && x.Uom == req.Uom);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var fromDate = data.FromDate.Date;
                    var toDate = data.ToDate.Date;

                    if (fromDate < DateTime.Now || fromDate == DateTime.Now) // đang được sử dụng
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Không được xóa, vì ngày bắt đầu có hiệu lực {fromDate} này nhỏ hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại"
                        };
                    }

                    db.MemberEarnItems.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
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
        public Tuple<int, string, string> ImportExcelMemberEarnItem(string userName, List<ImportExcelSetupMemberEarnItemModel> listData)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    // Check existed Item

                    var listItemNo = listData.Select(x => x.ItemNo).ToList();

                    var checkExistedItem = db.Items.AsNoTracking()
                        .Where(x => listItemNo.Contains(x.No))
                        .Select(x => x.No)
                        .ToList();

                    var listNotExisted = listItemNo.Except(checkExistedItem);

                    if (listNotExisted != null && listNotExisted.Count() > 0)
                    {
                        var listCondition = listData.Where(a =>listNotExisted.Contains(a.ItemNo))
                            .Select(x => $"{x.ItemNo}")
                            .ToList();
                        return new Tuple<int, string, string>(3, string.Empty, string.Join("|", listCondition));
                    }

                    // Check ItemNo het hieu luc kinh doanh

                    var checkBlock = db.Items.AsNoTracking()
                        .Where(x => listItemNo.Contains(x.No) && x.Blocked == 1) // Hết hiệu lực kinh doanh
                        .Select(x => x.No)
                        .ToList();

                    if (checkBlock != null && checkBlock.Count > 0)
                    {
                        return new Tuple<int, string, string>(0, $"Mã sản phẩm: {string.Join(", ", checkBlock).TrimEnd()} này hết hiệu lực kinh doanh. Vui lòng kiểm tra lại file import", string.Empty);
                    }

                    // Check ĐVT

                    var listUOM = listData.Select(x => x.Uom).ToList();
                    var checkUOM = db.Barcodes.AsNoTracking()
                        .Where(x => listUOM.Contains(x.UnitOfMeasureCode) && x.Blocked == 0)
                        .Select(x => x.UnitOfMeasureCode)
                        .ToList();

                    if (listUOM == null || listUOM.Count == 0)
                    {
                        return new Tuple<int, string, string>(0, $"Danh sách các ĐVT: {string.Join(", ", listUOM).TrimEnd()} này không tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Empty);
                    }

                    foreach (var item in listData)
                    {
                        var checkItemNo = db.MemberEarnItems.AsNoTracking().FirstOrDefault(x =>x.ItemNo == item.ItemNo && x.Uom == item.Uom && x.FromDate == item.FromDate && x.Blocked == false); // có hiệu lực, ko check itemno het hieu luc
                        if (checkItemNo != null)
                        {
                            return new Tuple<int, string, string>(0, $"Pkey : {item.ItemNo}-{item.Uom}-{item.FromDate.Date} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Empty);
                        }

                        //var checkItem = db.MemberEarnItems.AsNoTracking()
                        //   .Any(x => (item.FromDate.Date >= DbFunctions.TruncateTime(x.FromDate) && item.FromDate.Date <= DbFunctions.TruncateTime(x.ToDate) && x.Blocked == false)
                        //    || (item.ToDate.Date >= DbFunctions.TruncateTime(x.FromDate) && item.ToDate.Date <= DbFunctions.TruncateTime(x.ToDate) && x.Blocked == false)
                        //    || (item.FromDate.Date <= DbFunctions.TruncateTime(x.ToDate) && DbFunctions.TruncateTime(x.ToDate) <= item.ToDate.Date && x.Blocked == false)
                        //    || (item.FromDate.Date <= DbFunctions.TruncateTime(x.FromDate) && DbFunctions.TruncateTime(x.FromDate) <= item.ToDate.Date && x.Blocked == false)
                        //   );

                        //if (checkItem)
                        //{
                        //    return new Tuple<int, string, string>(0, $"Danh sách các Pkey này {item.ItemNo}{'-'}{item.Uom}{'-'}{item.FromDate.Date} bị trùng thời gian hiệu lực, khoảng thời gian hiệu lực này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Empty);
                        //}

                        // Buoc 1 : xoa cac itmeno bi khóa nhung thời gian hieu luc la tuong lai, chua ap dung

                        var listdata1 = db.MemberEarnItems.Where(x =>x.ItemNo == item.ItemNo && x.Uom == item.Uom && x.Blocked == true).ToList();
                        if (listdata1.Count > 0)
                        {
                            var _minFromDate = listdata1.Select(x => x.FromDate).Min().Date;
                            var _maxFromDate = listdata1.Select(x => x.FromDate).Max().Date;
                            var _minToDate = listdata1.Select(x => x.ToDate).Min().Date;
                            var _maxToDate = listdata1.Select(x => x.ToDate).Max().Date;

                            if (item.FromDate.Date >= _minFromDate && item.FromDate.Date <= _maxFromDate
                               || item.ToDate.Date >= _minToDate && item.ToDate.Date <= _maxToDate)
                            {
                                // xoa truoc khi them
                                db.MemberEarnItems.RemoveRange(db.MemberEarnItems.Where(x => x.ItemNo == item.ItemNo && x.Uom == item.Uom && x.Blocked == true));
                            }
                        }

                        // Buoc 2: 

                        var listdata2 = db.MemberEarnItems.AsNoTracking().Where(x =>x.ItemNo == item.ItemNo && x.Uom == item.Uom && x.Blocked == false).ToList();
                        if (listdata2.Count > 0)
                        {
                            var _minFromDate = listdata2.Select(x => x.FromDate).Min().Date;
                            var _maxFromDate = listdata2.Select(x => x.FromDate).Max().Date;
                            var _minToDate = listdata2.Select(x => x.ToDate).Min().Date;
                            var _maxToDate = listdata2.Select(x => x.ToDate).Max().Date;

                            if (item.FromDate.Date >= _minFromDate && item.FromDate.Date <= _maxFromDate
                               || item.ToDate.Date >= _minToDate && item.ToDate.Date <= _maxToDate)
                            {
                                return new Tuple<int, string, string>(0, $"Pkey này {item.ItemNo}{'-'}{item.Uom}{'-'}{item.FromDate.Date} bị trùng thời gian hiệu lực, khoảng thời gian hiệu lực này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Empty);
                            }
                        }
                    }

                    // Insert DB = MemberEarnItem
                    var listEntities = listData.Select(x => new MemberEarnItem
                    {
                        ItemNo = x.ItemNo,
                        Uom = x.Uom.ToUpper(),
                        Size = db.Items.Where(a =>a.No == x.ItemNo).FirstOrDefault()?.Size,
                        SaleQty = x.SaleQty,
                        StampsQty = x.StampsQty,
                        FromDate = x.FromDate,
                        ToDate = x.ToDate,
                        Blocked = (x.Blocked == 0) ? false : true,
                        CrtUser = userName,
                        CrtdDate = DateTime.Now,
                        UpdDate = DateTime.Now
                    }).ToList();

                    // Insert DB = MemberEarnItemLog
                    var maxCounterLog = db.MemberEarnItemLogs.Max(x =>x.Counter) ?? 0;
                    var listEntitiesLog = listData.Select(x => new MemberEarnItemLog
                    {
                        StoreNo = string.Empty,
                        ItemNo = x.ItemNo,
                        Description = db.Items.Where(a =>a.No == x.ItemNo).FirstOrDefault()?.Description,
                        Uom = x.Uom.ToUpper(),
                        Size = db.Items.Where(a =>a.No == x.ItemNo).FirstOrDefault()?.Size,
                        SaleQty = x.SaleQty,
                        StampsQty = x.StampsQty,
                        FromDate = x.FromDate,
                        ToDate = x.ToDate,
                        Blocked = (x.Blocked == 0) ? false : true,
                        CreatedUser = userName,
                        CreatedDate = DateTime.Now,
                        UpdatedUser = userName,
                        UpdatedDate = DateTime.Now,
                        Pkey = $"{x.ItemNo}-{x.Uom}-{x.FromDate.ToString("yyyyMMdd")}",
                        Counter = maxCounterLog + 1
                    }).ToList();

                    db.MemberEarnItems.AddRange(listEntities);
                    db.MemberEarnItemLogs.AddRange(listEntitiesLog); 
                    db.SaveChanges();
                    return new Tuple<int, string, string>(1, $"Import danh sách thành công", string.Empty);

                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string, string>(0, "Có lỗi xảy ra, Import danh sách thất bại", string.Empty);
            }
        }
        public ResultResponseModel UpdateSetupMemberEarnItem(UpdateSetupMemberEarnItemModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _fromDate = Convert.ToDateTime(req.FromDateStr);
                    var _toDate = Convert.ToDateTime(req.ToDateStr);

                    if (req.Blocked == "0")
                    {
                        if (_toDate < DateTime.Now.Date)
                        {
                            return new ResultResponseModel
                            {
                                Item1 = req.ItemNo,
                                Item2 = req.Uom,
                                Item3 = req.FromDate.ToString(),
                                Item4 = req.Blocked.ToString(),
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"{req.ItemNo}-{req.Uom}-{req.FromDateStr} có thời gian đến ngày < ngày hiện tại, nên không Active được. Vui lòng kiểm tra lại"
                            };
                        }

                        // Check : nếu trùng thời gian với các item đang co hieu luc thì không cho Acvite lại
                        var checkItemNo = db.MemberEarnItems.AsNoTracking().Where(a =>a.ItemNo == req.ItemNo && a.Uom == req.Uom && a.Blocked == false).ToList();
                        var minFromDate = checkItemNo.Select(x =>x.FromDate).Min().Date;
                        var maxToDate = checkItemNo.Select(x =>x.ToDate).Max().Date;
                        if ((_fromDate >= minFromDate && _fromDate <= maxToDate) || ( _toDate >= minFromDate && _toDate <= maxToDate))
                        {
                            return new ResultResponseModel
                            {
                                Item1 = req.ItemNo,
                                Item2 = req.Uom,
                                Item3 = req.FromDate.ToString(),
                                Item4 = req.Blocked.ToString(),
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"{req.ItemNo}-{req.Uom}-{req.FromDateStr} không Active lại được, vì trùng thời gian hiệu lực với chương trình đang tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }
                    }

                    var data = db.MemberEarnItems.Where(x =>x.ItemNo == req.ItemNo && x.Uom == req.Uom && x.FromDate == _fromDate).FirstOrDefault();

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Item1 = string.Empty,
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy thông tin. Vui lòng kiểm tra lại"
                        };
                    }

                    data.SaleQty = req.SaleQty;
                    data.StampsQty = req.StampsQty;
                    data.Blocked = (req.Blocked == "1") ? true : false;
                    data.CrtUser = req.CrtUser;
                    data.UpdDate = DateTime.Now;

                    var dataLog = db.MemberEarnItemLogs.Where(x =>x.ItemNo == req.ItemNo && x.Uom == req.Uom && x.FromDate == _fromDate).FirstOrDefault();

                    if (dataLog == null)
                    {
                        // Insert vào table MemberEarnItemLogs

                        var listLog =  new MemberEarnItemLog
                        {
                            StoreNo = string.Empty,
                            ItemNo = req.ItemNo,
                            Description = db.Items.AsNoTracking().Where(a =>a.No == req.ItemNo).FirstOrDefault()?.Description,
                            Uom = req.Uom.ToUpper(),
                            Size = db.Items.AsNoTracking().Where(a =>a.No == req.ItemNo).FirstOrDefault()?.Size,
                            SaleQty = req.SaleQty,
                            StampsQty = req.StampsQty,
                            FromDate = _fromDate,
                            ToDate = _toDate,
                            Blocked = (req.Blocked == "0") ? false : true,
                            CreatedUser = req.CrtUser,
                            CreatedDate = DateTime.Now,
                            UpdatedUser = req.CrtUser,
                            UpdatedDate = DateTime.Now,
                            Pkey = $"{req.ItemNo}-{req.Uom}-{req.FromDate.ToString("yyyyMMdd")}"
                        };

                        db.MemberEarnItemLogs.Add(listLog);

                    }
                    else
                    {
                        dataLog.SaleQty = req.SaleQty;
                        dataLog.StampsQty = req.StampsQty;
                        dataLog.Blocked = (req.Blocked == "1") ? true : false;
                        dataLog.UpdatedUser = req.CrtUser;
                        dataLog.UpdatedDate = DateTime.Now;
                    }

                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.ItemNo,
                        Item2 = req.Uom,
                        Item3 = req.FromDate.ToString(),
                        Item4 = req.Blocked.ToString(),
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };

                }
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


        public List<GiftCouponResponseModel> GetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<GiftCouponResponseModel>("[dbo].[GetGiftCouponList] @FromDate, @ToDate, @OrderNo, @Coupon, @TextSearch, @IsUsed, @SendCX, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Coupon", coupon ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@IsUsed", isUsed ?? string.Empty),
                        new SqlParameter("@SendCX", sendCX ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GiftCouponResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<GiftCouponResponseModel>();
            }
        }

        public List<ExportExcelGiftCouponResponseModel> ExportExcelGetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX)
        {
            try
            {
                var data = new List<ExportExcelGiftCouponResponseModel>();
                using (var db = new LoyaltyContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportExcelGiftCouponResponseModel>("[dbo].[GetGiftCouponList] @FromDate, @ToDate, @OrderNo, @Coupon, @TextSearch, @IsUsed, @SendCX, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@OrderNo", orderNo ?? string.Empty),
                        new SqlParameter("@Coupon", coupon ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@IsUsed", isUsed ?? string.Empty),
                        new SqlParameter("@SendCX", sendCX ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelGiftCouponResponseModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelGiftCouponResponseModel>();
            }
        }






    }
}
