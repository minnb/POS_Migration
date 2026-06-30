using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.SetupCoupon;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Data.SetupCoupon
{
    public interface ISetupCouponData
    {
        ViewSetupCouponModel ViewInfoICoupon(string itemNo);
        ViewSetupCpnVchModel ViewInfoICpnVch(string itemNo);
        List<CpnVchBOMIssueRuleModel> LoadCouponRule(string issueType, string itemNo, string description, string blocked, out int totalRecord, int skip, int take);
        List<CpnVchBOMIssueRuleModel> GetIssueRuleByPrefix(string prefix);
        Tuple<bool, string, SaveIssueCouponRequest> SaveCoupon(SaveIssueCouponRequest model);
        Tuple<bool, string, SetupAdvancedCouponRequest> SaveAdvancedCoupon(SetupAdvancedCouponRequest model);
        List<LoadCouponCodeModel> CouponCodeLoad(string itemNo, out int totalRecord, int skip, int take);
        List<string> CheckExistsCodeVoucher(List<RandomCode> listCode);
        List<ExcelExampleCouponModel> ExcelExampleCoupon();
        Tuple<bool, string> DeleteItemCoupon(string itemNo);
        Tuple<bool, string, List<CpnItemModel>> CheckListItemExists(List<string> listItem);
        Tuple<bool, string, CpnVchBOMHeaderRequestModel> SaveCpnVch(CpnVchBOMHeaderRequestModel model);
        List<CpnVchBOMHeaderModel> LoadCpnVch(string itemNo, string description, string blocked, out int totalRecord, int skip, int take);
        List<Model.SetupItem.ItemModel> LoadItemByArticleType(string articleType, string itemNo, string itemName, out int totalRecord, int skip, int take);
    }
    public class SetupCouponData : ISetupCouponData
    {
        private readonly object lockRequest = new object();

        public ViewSetupCouponModel ViewInfoICoupon(string itemNo)
        {
            var result = new ViewSetupCouponModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listUOM = db.UnitOfMeasures.Select(a => new UnitOfMeasureModel
                    {
                        Code = a.Code,
                        Description = a.Description
                    }).ToList();

                    var itemCouponRule = new CpnVchBOMIssueRuleModel();
                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        itemCouponRule = (from a in db.CpnVchBOMIssueRules
                                          join b in db.CpnVchBOMHeaders on a.ItemNo equals b.ItemNo
                                          where a.ItemNo == itemNo
                                          select new CpnVchBOMIssueRuleModel
                                          {
                                              ItemNo = a.ItemNo,
                                              Description = b.ItemName,
                                              UOM = b.UnitOfMeasure,
                                              DiscountType = b.DiscountType,
                                              DiscountValue = (double)b.DiscountValue,
                                              StartingDate = b.StartingDate,
                                              EndingDate = b.EndingDate,
                                              CpnVchType = b.CpnVchType,
                                              Prefix = a.Prefix,
                                              LenCode = a.LenCode,
                                              IssueType = a.IssueType,
                                              CharOfNumber = a.CharOfNumber,
                                              CharPosition = a.CharPosition,
                                              CreatedDate = a.CreatedDate,
                                              Counter = a.Counter,
                                              IsMultiUse = b.IsMultiUse,
                                              Pkey = a.Pkey,
                                              Blocked = b.Blocked,
                                              IsCheckAPI = b.IsCheckAPI,
                                              IsCheckItem = b.IsCheckItem,
                                              LimitQty = b.LimitQty,
                                              LimitQtyUsed = b.LimitQtyUsed,
                                              MaxAmount = b.MaxAmount,
                                              SaleType = b.SaleType,
                                              StoreGroupCode = b.StoreGroupCode

                                          }).FirstOrDefault();
                    }

                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypeModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();

                    var storeGroup = db.StoreGroups.Where(d => d.Status == true).Select(a => new StoreGroupCodeModel
                    {
                        StoreNo = a.StoreNo,
                        GroupCode = a.GroupCode,
                        Pkey = a.Pkey,
                        Counter = a.Counter,
                        CreatedDate = a.CreatedDate
                    }).ToList();
                    var qtyCode = db.CpnVchBOMCodeIssues.Where(d => d.ItemNo == itemNo).Count();

                    var listCpnItemLine = db.CpnVchBOMLines.Where(d => d.ItemNo == itemNo)
                        .Select(a => new CpnItemModel
                        {
                            ItemNo = a.LineItemNo,
                            ItemName = a.Description,
                            UOM = a.UnitOfMeasure,
                            Barcode = a.Barcode

                        }).ToList();

                    result = new ViewSetupCouponModel
                    {
                        ItemNo = itemNo,
                        QuantityCode = qtyCode,
                        ListUOM = listUOM,
                        CpnRule = itemCouponRule,
                        ListSalesType = salesType,
                        ListStoreGroup = storeGroup,
                        ListItemLine = listCpnItemLine
                    };
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public ViewSetupCpnVchModel ViewInfoICpnVch(string itemNo)
        {
            var result = new ViewSetupCpnVchModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listUOM = db.UnitOfMeasures.Select(a => new UnitOfMeasureModel
                    {
                        Code = a.Code,
                        Description = a.Description
                    }).ToList();

                    var itemHeader = new CpnVchBOMHeaderModel();
                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        itemHeader = (from a in db.CpnVchBOMHeaders
                                      where a.ItemNo == itemNo
                                      select new CpnVchBOMHeaderModel
                                      {
                                          ItemNo = a.ItemNo,
                                          ItemName = a.ItemName,
                                          ArticleType = a.ArticleType,
                                          UnitOfMeasure = a.UnitOfMeasure,
                                          DiscountType = a.DiscountType,
                                          DiscountValue = a.DiscountValue,
                                          StartingDate = a.StartingDate,
                                          EndingDate = a.EndingDate,
                                          CpnVchType = a.CpnVchType,
                                          Counter = a.Counter,
                                          CouponCode = a.CouponCode,
                                          IsMultiUse = a.IsMultiUse,
                                          Pkey = a.Pkey,
                                          Blocked = a.Blocked,
                                          IsCheckItem = a.IsCheckItem,
                                          IsCheckAPI = a.IsCheckAPI,
                                          LimitQty = a.LimitQty,
                                          LimitQtyUsed = a.LimitQtyUsed,
                                          MaxAmount = a.MaxAmount,
                                          SaleType = a.SaleType,
                                          StoreGroupCode = a.StoreGroupCode,
                                          LastDateModified = a.LastDateModified

                                      }).FirstOrDefault();
                    }

                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypeModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();

                    var storeGroup = db.StoreGroups.Where(d => d.Status == true).Select(a => new StoreGroupCodeModel
                    {
                        StoreNo = a.StoreNo,
                        GroupCode = a.GroupCode,
                        Pkey = a.Pkey,
                        Counter = a.Counter,
                        CreatedDate = a.CreatedDate
                    }).ToList();

                    // var qtyCode = db.CpnVchBOMCodeIssues.Where(d => d.ItemNo == itemNo).Count();

                    var listCpnItemLine = db.CpnVchBOMLines.Where(d => d.ItemNo == itemNo)
                        .Select(a => new CpnItemModel
                        {
                            ItemNo = a.LineItemNo,
                            ItemName = a.Description,
                            UOM = a.UnitOfMeasure,
                            Barcode = a.Barcode

                        }).ToList();

                    result = new ViewSetupCpnVchModel
                    {
                        ItemNo = itemNo,
                        CpnVchHeader = itemHeader,
                        ListUOM = listUOM,
                        ListSalesType = salesType,
                        ListStoreGroup = storeGroup,
                        ListItemLine = listCpnItemLine
                    };
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }
        public List<CpnVchBOMIssueRuleModel> LoadCouponRule(string issueType, string itemNo, string description, string blocked, out int totalRecord, int skip, int take)
        {
            var result = new List<CpnVchBOMIssueRuleModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;

                    var data = from a in db.CpnVchBOMIssueRules
                               join b in db.CpnVchBOMHeaders on a.ItemNo equals b.ItemNo
                               select new CpnVchBOMIssueRuleModel
                               {
                                   ItemNo = a.ItemNo,
                                   Description = b.ItemName,
                                   Prefix = a.Prefix,
                                   LenCode = a.LenCode,
                                   IssueType = a.IssueType,
                                   CharOfNumber = a.CharOfNumber,
                                   CharPosition = a.CharPosition,
                                   CreatedDate = a.CreatedDate,
                                   Counter = a.Counter,
                                   Pkey = a.Pkey,
                                   Blocked = b.Blocked,
                                   QtyCoupon = db.CpnVchBOMCodeIssues.Count(d => d.ItemNo == a.ItemNo),
                                   StartingDate = b.StartingDate,
                                   EndingDate = b.EndingDate,
                                   Status = b.Blocked == 1 ? false : DbFunctions.TruncateTime(b.EndingDate) < DbFunctions.TruncateTime(date) ? false : true

                               };
                    if (!string.IsNullOrEmpty(issueType))
                    {
                        data = data.Where(m => m.IssueType == issueType);
                    }
                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(m => m.ItemNo == itemNo);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        data = data.Where(m => m.Description.ToLower().Contains(description.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(blocked))
                    {
                        var block = blocked == "0" ? false : true;
                        data = data.Where(m => m.Status == block);
                    }

                    totalRecord = data.Count();
                    result = data.OrderByDescending(d => d.ItemNo).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public List<CpnVchBOMIssueRuleModel> GetIssueRuleByPrefix(string prefix)
        {
            var result = new List<CpnVchBOMIssueRuleModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    result = (from a in db.CpnVchBOMIssueRules
                              join b in db.CpnVchBOMHeaders on a.ItemNo equals b.ItemNo
                              where a.Prefix.ToLower() == prefix.ToLower()
                              select new CpnVchBOMIssueRuleModel
                              {
                                  ItemNo = a.ItemNo,
                                  Description = b.ItemName,
                                  Prefix = a.Prefix,
                                  LenCode = a.LenCode,
                                  IssueType = a.IssueType,
                                  CharOfNumber = a.CharOfNumber,
                                  CharPosition = a.CharPosition,
                                  CreatedDate = a.CreatedDate,
                                  Counter = a.Counter,
                                  Pkey = a.Pkey

                              }).ToList();
                }

            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public Tuple<bool, string, SaveIssueCouponRequest> SaveCoupon(SaveIssueCouponRequest model)
        {
            var result = new Tuple<bool, string, SaveIssueCouponRequest>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model;

                            var checkItem = db.CpnVchBOMIssueRules.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                            var listCode = model.ListCode;
                            var dateTime = DateTime.Now;
                            var counterItem = db.CpnVchBOMIssueRules.Max(a => a.Counter) ?? 0;
                            var counterCpnHeader = db.CpnVchBOMHeaders.Max(a => a.Counter) ?? 0;

                            if (checkItem != null)
                            {
                                checkItem.Prefix = item.Prefix;
                                checkItem.LenCode = item.LenCode;
                                checkItem.IssueType = item.IssueType;
                                checkItem.CharOfNumber = item.CharOfNumber;
                                checkItem.CharPosition = item.CharPosition;
                                checkItem.Counter = counterItem + 1;
                                checkItem.CreatedDate = dateTime;

                                var checkHeader = db.CpnVchBOMHeaders.FirstOrDefault(d => d.ItemNo == item.ItemNo);

                                if (checkHeader != null)
                                {
                                    checkHeader.ItemName = item.Description;
                                    checkHeader.StartingDate = item.StartingDate;
                                    checkHeader.EndingDate = item.EndingDate;
                                    checkHeader.LastDateModified = dateTime;
                                    checkHeader.SaleType = item.SaleType;
                                    checkHeader.StoreGroupCode = item.StoreGroupCode;
                                    checkHeader.ArticleType = item.ArticleType;
                                    checkHeader.IsCheckItem = item.IsCheckItem;
                                    checkHeader.Counter = counterCpnHeader + 1;
                                }

                                var checkIssueCode = db.CpnVchBOMCodeIssues.Count(d => d.ItemNo == item.ItemNo);
                                if (checkIssueCode == 0)
                                {
                                    var counterIssueCode = db.CpnVchBOMCodeIssues.Max(a => a.Counter) ?? 0;
                                    var insertIssueCode = listCode.Select(a => new CpnVchBOMCodeIssue
                                    {
                                        ItemNo = item.ItemNo,
                                        Code = a.Code,
                                        Enabled = true,
                                        CreatedDate = dateTime,
                                        Counter = counterIssueCode + 1,
                                        Pkey = item.ItemNo
                                    });
                                    db.CpnVchBOMCodeIssues.AddRange(insertIssueCode);
                                }
                            }
                            else
                            {
                                //var countHeader = db.CpnVchBOMHeaders.Count();
                                //var countIssueRule = db.CpnVchBOMIssueRules.Count();
                                //if (countHeader != countIssueRule)
                                //{
                                //    return new Tuple<bool, string, SaveIssueCouponRequest>(false, $"Vui lòng kiểm tra lại số lượng 2 table CpnVchBOMHeader({countHeader}) và CpnVchBOMIssueRule({countIssueRule}) có tổng coupon lệch nhau", model);
                                //}
                                var maxCode = "C70000001";
                                var listCpnVchHeader = db.CpnVchBOMHeaders.Where(d => d.ItemNo.Substring(0, 1) == "C").Select(d => d.ItemNo.Substring(1, d.ItemNo.Length - 1)).ToList();
                                if (listCpnVchHeader.Count > 0)
                                {
                                    //2=>C70000002
                                    maxCode = "C" + (listCpnVchHeader.Select(int.Parse).ToList().Max() + 1);
                                }

                                //var noAuto = db.CpnVchBOMHeaders.Max(d => d.ItemNo);
                                //var noSave = string.IsNullOrEmpty(maxCode) ? "C" + Convert.ToInt64(noNew) : "C" + (Convert.ToInt64(maxCode) + 1);
                                var counterItemIssue = db.CpnVchBOMIssueRules.Max(a => a.Counter) ?? 0;
                                var itemIssueInsert = new CpnVchBOMIssueRule
                                {
                                    ItemNo = maxCode,
                                    Prefix = item.Prefix,
                                    LenCode = item.LenCode,
                                    IssueType = item.IssueType,
                                    CharOfNumber = item.CharOfNumber,
                                    CharPosition = item.CharPosition,
                                    Counter = counterItemIssue + 1,
                                    CreatedDate = dateTime,
                                    Pkey = maxCode
                                };
                                db.CpnVchBOMIssueRules.Add(itemIssueInsert);

                                var checkHeader = db.CpnVchBOMHeaders.Any(d => d.ItemNo == maxCode);

                                if (!checkHeader)
                                {

                                    var insertCpnHeader = new CpnVchBOMHeader
                                    {
                                        ItemNo = maxCode,
                                        ItemName = item.Description,
                                        UnitOfMeasure = "",
                                        DiscountType = 0,
                                        DiscountValue = 0,
                                        ValueOfVoucher = 0,
                                        StartingDate = item.StartingDate,
                                        EndingDate = item.EndingDate,
                                        MaxAmount = 0,
                                        LastDateModified = dateTime,
                                        Blocked = byte.Parse("0"),
                                        LimitQty = 0,
                                        IsCheckAPI = true,
                                        IsMultiUse = true,
                                        LimitQtyUsed = 0,
                                        SaleType = item.SaleType,
                                        StoreGroupCode = item.StoreGroupCode,
                                        Pkey = maxCode,
                                        ArticleType = item.ArticleType,
                                        CpnVchType = "",
                                        CouponCode = "",
                                        IsCheckItem = false,
                                        Counter = counterCpnHeader + 1
                                    };
                                    db.CpnVchBOMHeaders.Add(insertCpnHeader);
                                    model.ItemNo = maxCode;
                                }

                                var counterIssueCode = db.CpnVchBOMCodeIssues.Max(a => a.Counter) ?? 0;

                                var insertIssueCode = listCode.Select(a => new CpnVchBOMCodeIssue
                                {
                                    ItemNo = maxCode,
                                    Code = a.Code,
                                    Enabled = true,
                                    CreatedDate = dateTime,
                                    Counter = counterIssueCode + 1,
                                    Pkey = maxCode
                                });
                                db.CpnVchBOMCodeIssues.AddRange(insertIssueCode);
                                model.QuantityCodeInDB = listCode.Count;
                            }

                            var listItem = JsonConvert.DeserializeObject<List<CpnItemModel>>(model.JsonItem);
                            var checkCpnLine = db.CpnVchBOMLines.Where(d => d.ItemNo == item.ItemNo).ToList();
                            if (checkCpnLine != null && checkCpnLine.Count > 0)
                            {
                                db.CpnVchBOMLines.RemoveRange(checkCpnLine);
                            }
                            if (listItem != null && listItem.Count > 0)
                            {
                                var counterCpnLine = db.CpnVchBOMLines.Max(a => a.Counter) ?? 0;
                                var lineNo = 1;

                                var lineCpnByItem = db.CpnVchBOMLines.Where(d => d.ItemNo == model.ItemNo).ToList();
                                if (lineCpnByItem != null && lineCpnByItem.Count > 0)
                                {
                                    lineNo = lineCpnByItem.Max(a => a.LineNo);
                                }

                                var insertCpnItem = listItem.Select(a => new CpnVchBOMLine
                                {
                                    ItemNo = model.ItemNo,
                                    LineNo = lineNo,
                                    LineItemNo = a.ItemNo,
                                    Description = a.ItemName,
                                    UnitOfMeasure = a.UOM,
                                    Counter = counterCpnLine + 1,
                                    Barcode = a.Barcode,
                                    Pkey = $"{model.ItemNo}-{a.ItemNo}-{a.Barcode}"
                                });

                                db.CpnVchBOMLines.AddRange(insertCpnItem);
                            }

                            var storeGroup = item.StoreGroupCode;
                            var checkBOMStore = db.CpnVchBOMStores.Where(d => d.ItemNo == item.ItemNo);
                            var counterStore = db.CpnVchBOMStores.Max(a => a.Counter) ?? 0;
                            if (checkBOMStore != null && checkBOMStore.Count() > 0)
                            {
                                db.CpnVchBOMStores.RemoveRange(checkBOMStore);
                            }
                            if (storeGroup == "ALL")
                            {
                                var insertStore = new CpnVchBOMStore
                                {
                                    ItemNo = item.ItemNo,
                                    StoreNo = storeGroup,
                                    Enabled = true,
                                    Counter = counterStore + 1,
                                    CreatedDate = dateTime,
                                    Pkey = item.ItemNo
                                };
                                db.CpnVchBOMStores.Add(insertStore);
                            }
                            else
                            {
                                var listStore = db.StoreGroups.Where(d => d.GroupCode == storeGroup).Select(a => a.StoreNo).ToList();
                                if (listStore.Count > 0)
                                {
                                    var insertStore = listStore.Select(a => new CpnVchBOMStore
                                    {
                                        ItemNo = item.ItemNo,
                                        StoreNo = a,
                                        Enabled = true,
                                        Counter = counterStore + 1,
                                        CreatedDate = dateTime,
                                        Pkey = item.ItemNo
                                    });
                                    db.CpnVchBOMStores.AddRange(insertStore);
                                }
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SaveIssueCouponRequest>(true, $"Cập nhật thành công coupon {item.ItemNo}", model);
                        }

                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SaveIssueCouponRequest>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }

            return result;
        }

        public Tuple<bool, string, SetupAdvancedCouponRequest> SaveAdvancedCoupon(SetupAdvancedCouponRequest model)
        {
            var result = new Tuple<bool, string, SetupAdvancedCouponRequest>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model;
                            var checkItem = db.CpnVchBOMIssueRules.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                            var counterCpnHeader = db.CpnVchBOMHeaders.Max(a => a.Counter) ?? 0;
                            var dateTime = DateTime.Now;
                            if (checkItem != null)
                            {
                                checkItem.IssueType = item.IssueType;
                                checkItem.CreatedDate = dateTime;

                                var checkHeader = db.CpnVchBOMHeaders.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                                if (checkHeader != null)
                                {
                                    checkHeader.ItemName = item.Description;
                                    checkHeader.UnitOfMeasure = item.UOM;
                                    checkHeader.DiscountType = item.DiscountType;
                                    checkHeader.DiscountValue = (float)item.DiscountValue;
                                    checkHeader.ValueOfVoucher = (float)item.DiscountValue;
                                    checkHeader.StartingDate = item.StartingDate;
                                    checkHeader.EndingDate = item.EndingDate;
                                    checkHeader.MaxAmount = (float)item.MaxValue;
                                    checkHeader.LastDateModified = dateTime;
                                    checkHeader.Blocked = item.Blocked == true ? byte.Parse("1") : byte.Parse("0");
                                    checkHeader.LimitQty = item.LimitQty;
                                    checkHeader.IsCheckAPI = item.IsCheckAPI;
                                    checkHeader.IsMultiUse = item.IsMultiUsed;
                                    checkHeader.LimitQtyUsed = item.LimitQtyUsed;
                                    checkHeader.SaleType = item.SaleType;
                                    checkHeader.StoreGroupCode = item.StoreGroupCode;
                                    checkHeader.ArticleType = item.ArticleType;
                                    checkHeader.CpnVchType = item.CpnVchType;
                                    checkHeader.Counter = counterCpnHeader + 1;
                                }
                            }
                            else
                            {
                                var maxCode = "C70000001";
                                var listCpnVchHeader = db.CpnVchBOMHeaders.Where(d => d.ItemNo.Substring(0, 1) == "C").Select(d => d.ItemNo.Substring(1, d.ItemNo.Length - 1)).ToList();
                                if (listCpnVchHeader != null || listCpnVchHeader.Count > 0)
                                {
                                    maxCode = "C" + (listCpnVchHeader.Select(int.Parse).ToList().Max() + 1);
                                }

                                //var noNew = "70000001";
                                //var listCpnVchHeader = db.CpnVchBOMHeaders.Select(d => d.ItemNo.Substring(1, d.ItemNo.Length - 1)).ToList();
                                //var maxCode = "" + listCpnVchHeader.Select(int.Parse).ToList().Max();

                                // var noAuto = db.CpnVchBOMIssueRules.Max(d => d.ItemNo);
                                //var noSave = string.IsNullOrEmpty(maxCode) ? "C" + Convert.ToInt64(noNew) : "C" + (Convert.ToInt64(maxCode) + 1);
                                //var counterItemIssue = db.CpnVchBOMIssueRules.Max(a => a.Counter) ?? 0;
                                var itemIssueInsert = new CpnVchBOMIssueRule
                                {
                                    ItemNo = maxCode,
                                    IssueType = item.IssueType,
                                    CreatedDate = dateTime,
                                    Counter = 0,
                                    Pkey = maxCode
                                };
                                db.CpnVchBOMIssueRules.Add(itemIssueInsert);


                                var insertCpnHeader = new CpnVchBOMHeader
                                {
                                    ItemNo = maxCode,
                                    ItemName = item.Description,
                                    UnitOfMeasure = item.UOM,
                                    DiscountType = item.DiscountType,
                                    DiscountValue = (float)item.DiscountValue,
                                    ValueOfVoucher = (float)item.DiscountValue,
                                    StartingDate = item.StartingDate,
                                    EndingDate = item.EndingDate,
                                    MaxAmount = (float)item.MaxValue,
                                    LastDateModified = dateTime,
                                    Blocked = item.Blocked == true ? byte.Parse("1") : byte.Parse("0"),
                                    LimitQty = item.LimitQty,
                                    IsCheckAPI = item.IsCheckAPI,
                                    IsMultiUse = item.IsMultiUsed,
                                    LimitQtyUsed = item.LimitQtyUsed,
                                    SaleType = item.SaleType,
                                    StoreGroupCode = item.StoreGroupCode,
                                    Pkey = maxCode,
                                    ArticleType = item.ArticleType,
                                    CpnVchType = item.CpnVchType,
                                    CouponCode = "",
                                    IsCheckItem = false,
                                    Counter = counterCpnHeader + 1
                                };
                                db.CpnVchBOMHeaders.Add(insertCpnHeader);
                                model.ItemNo = maxCode;
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SetupAdvancedCouponRequest>(true, $"Cập nhật thành công coupon {item.ItemNo}", model);
                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SetupAdvancedCouponRequest>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }

        public List<LoadCouponCodeModel> CouponCodeLoad(string itemNo, out int totalRecord, int skip, int take)
        {
            var result = new List<LoadCouponCodeModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var data = from a in db.CpnVchBOMCodeIssues
                               join b in db.CpnVchBOMHeaders on a.ItemNo equals b.ItemNo
                               where a.ItemNo == itemNo
                               select new LoadCouponCodeModel
                               {
                                   ItemNo = a.ItemNo,
                                   Code = a.Code,
                                   Enable = a.Enabled ?? false

                               };

                    totalRecord = data.Count();
                    result = data.OrderBy(d => d.ItemNo).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public List<string> CheckExistsCodeVoucher(List<RandomCode> listCode)
        {
            var result = new List<string>();
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var listCodeStr = listCode.Select(a => a.Code).ToList();
                    var checkDB = db.CpnVchBOMCodeIssues.AsNoTracking().Where(d => listCodeStr.Any(a => a == d.Code)).ToList();
                    if (checkDB != null && checkDB.Count > 0)
                    {
                        result = checkDB.Select(a => a.Code).ToList();
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }

        public List<ExcelExampleCouponModel> ExcelExampleCoupon()
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.ExcelExampleCoupons.Select(a => new ExcelExampleCouponModel
                    {
                        CodeCoupon = a.CodeCoupon

                    }).ToList();
                    return data;
                }
                catch (Exception ex)
                {
                }
                return new List<ExcelExampleCouponModel>();
            }
        }

        public Tuple<bool, string> DeleteItemCoupon(string itemNo)
        {
            var result = new Tuple<bool, string>(false, $"ItemNo={itemNo}");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var checkID = db.CpnVchBOMIssueRules.FirstOrDefault(d => d.ItemNo == itemNo);
                            if (checkID != null)
                            {
                                var checkCode = db.CpnVchBOMCodeIssues.Where(d => d.ItemNo == itemNo).ToList();
                                if (checkCode != null && checkCode.Count > 0)
                                {
                                    result = new Tuple<bool, string>(false, $"Item này {itemNo} đã tồn tại {checkCode.Count} mã coupon");
                                    return result;
                                }
                                else
                                {
                                    db.CpnVchBOMIssueRules.Remove(checkID);
                                    var checkHeader = db.CpnVchBOMHeaders.FirstOrDefault(d => d.ItemNo == itemNo);
                                    if (checkHeader != null)
                                    {
                                        db.CpnVchBOMHeaders.Remove(checkHeader);
                                    }
                                    db.SaveChanges();
                                    transaction.Commit();
                                    return new Tuple<bool, string>(true, $"Success");
                                }
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
                            transaction.Rollback();
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                return result;
            }
        }

        public Tuple<bool, string, List<CpnItemModel>> CheckListItemExists(List<string> listItem)
        {
            var listItemModel = listItem.Select(a => new CpnItemModel
            {
                ItemNo = a
            }).ToList();

            var result = new Tuple<bool, string, List<CpnItemModel>>(false, "", listItemModel);
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {

                    var dbItem = (from a in db.Items.AsNoTracking().Where(d => listItem.Any(a => a == d.No))
                                  select new CpnItemModel
                                  {
                                      ItemNo = a.No,
                                      ItemName = a.Description,
                                      UOM = a.BaseUnitOfMeasure,
                                      Barcode = db.Barcodes.Any(d => d.ItemNo == a.No) ? db.Barcodes.Where(d => d.ItemNo == a.No).FirstOrDefault().BarcodeNo : ""
                                  }
                                  ).ToList();

                    result = new Tuple<bool, string, List<CpnItemModel>>(true, $"", dbItem);
                    return result;
                }
                catch (Exception ex)
                {
                    var message = $"Lỗi hệ thống:{JsonConvert.SerializeObject(ex)}";
                    result = new Tuple<bool, string, List<CpnItemModel>>(false, $"{message}", listItemModel);
                    return result;
                }
            }
        }
        public Tuple<bool, string, CpnVchBOMHeaderRequestModel> SaveCpnVch(CpnVchBOMHeaderRequestModel model)
        {
            var result = new Tuple<bool, string, CpnVchBOMHeaderRequestModel>(false, "", model);

            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model;
                            var checkHeader = db.CpnVchBOMHeaders.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                            var dateTime = DateTime.Now;
                            var counterCpnHeader = db.CpnVchBOMHeaders.Max(a => a.Counter) ?? 0;

                            if (checkHeader != null)
                            {
                                checkHeader.ItemName = item.ItemName;
                                checkHeader.UnitOfMeasure = item.UnitOfMeasure;
                                checkHeader.StartingDate = item.StartingDate;
                                checkHeader.EndingDate = item.EndingDate;
                                //checkHeader.LastDateModified = dateTime;
                                checkHeader.SaleType = item.SaleType;
                                checkHeader.DiscountType = item.DiscountType;
                                checkHeader.DiscountValue = item.DiscountValue;
                                checkHeader.ValueOfVoucher = item.DiscountValue;
                                checkHeader.Blocked = item.Blocked;
                                checkHeader.CouponCode = item.CouponCode;
                                checkHeader.LimitQty = item.LimitQty;
                                checkHeader.IsMultiUse = item.IsMultiUse;
                                checkHeader.LimitQtyUsed = item.LimitQtyUsed;
                                checkHeader.CpnVchType = item.CpnVchType;
                                checkHeader.IsCheckAPI = item.IsCheckAPI;
                                checkHeader.StoreGroupCode = item.StoreGroupCode;
                                checkHeader.ArticleType = item.ArticleType;

                                checkHeader.Counter = counterCpnHeader + 1;
                            }
                            else
                            {
                                var insertCpnHeader = new CpnVchBOMHeader
                                {
                                    ItemNo = item.ItemNo,
                                    ItemName = item.ItemName,
                                    UnitOfMeasure = item.UnitOfMeasure,
                                    DiscountType = item.DiscountType,
                                    DiscountValue = item.DiscountValue,
                                    ValueOfVoucher = item.DiscountValue,
                                    StartingDate = item.StartingDate,
                                    EndingDate = item.EndingDate,
                                    MaxAmount = item.MaxAmount,
                                    LastDateModified = dateTime,
                                    Blocked = item.Blocked,
                                    LimitQty = item.LimitQty,
                                    IsCheckAPI = item.IsCheckAPI,
                                    IsMultiUse = item.IsMultiUse,
                                    LimitQtyUsed = item.LimitQtyUsed,
                                    SaleType = item.SaleType,
                                    StoreGroupCode = item.StoreGroupCode,
                                    Pkey = item.ItemNo,
                                    ArticleType = item.ArticleType,
                                    CpnVchType = item.CpnVchType,
                                    CouponCode = item.CouponCode,
                                    IsCheckItem = false,
                                    Counter = counterCpnHeader + 1
                                };
                                db.CpnVchBOMHeaders.Add(insertCpnHeader);
                            }

                            var listItem = JsonConvert.DeserializeObject<List<CpnItemModel>>(model.JsonItem);
                            var checkCpnLine = db.CpnVchBOMLines.Where(d => d.ItemNo == item.ItemNo).ToList();
                            if (checkCpnLine != null && checkCpnLine.Count > 0)
                            {
                                db.CpnVchBOMLines.RemoveRange(checkCpnLine);
                            }
                            if (listItem != null && listItem.Count > 0)
                            {
                                var counterCpnLine = db.CpnVchBOMLines.Max(a => a.Counter) ?? 0;
                                var lineNo = 1;

                                var lineCpnByItem = db.CpnVchBOMLines.Where(d => d.ItemNo == model.ItemNo).ToList();
                                if (lineCpnByItem != null && lineCpnByItem.Count > 0)
                                {
                                    lineNo = lineCpnByItem.Max(a => a.LineNo);
                                }

                                var insertCpnItem = listItem.Select(a => new CpnVchBOMLine
                                {
                                    ItemNo = model.ItemNo,
                                    LineNo = lineNo,
                                    LineItemNo = a.ItemNo,
                                    Description = a.ItemName,
                                    UnitOfMeasure = a.UOM,
                                    Counter = counterCpnLine + 1,
                                    Barcode = a.Barcode,
                                    Pkey = $"{model.ItemNo}-{a.ItemNo}-{a.Barcode}"
                                });

                                db.CpnVchBOMLines.AddRange(insertCpnItem);
                            }
                            var storeGroup = item.StoreGroupCode;
                            var checkBOMStore = db.CpnVchBOMStores.Where(d => d.ItemNo == item.ItemNo);
                            var counterStore = db.CpnVchBOMStores.Max(a => a.Counter) ?? 0;
                            if (checkBOMStore != null && checkBOMStore.Count() > 0)
                            {
                                db.CpnVchBOMStores.RemoveRange(checkBOMStore);
                            }
                            if (storeGroup == "ALL")
                            {
                                var insertStore = new CpnVchBOMStore
                                {
                                    ItemNo = item.ItemNo,
                                    StoreNo = storeGroup,
                                    Enabled = true,
                                    Counter = counterStore + 1,
                                    CreatedDate = dateTime,
                                    Pkey = item.ItemNo
                                };
                                db.CpnVchBOMStores.Add(insertStore);
                            }
                            else
                            {
                                var listStore = db.StoreGroups.Where(d => d.GroupCode == storeGroup).Select(a => a.StoreNo).ToList();
                                if (listStore.Count > 0)
                                {
                                    var insertStore = listStore.Select(a => new CpnVchBOMStore
                                    {
                                        ItemNo = item.ItemNo,
                                        StoreNo = a,
                                        Enabled = true,
                                        Counter = counterStore + 1,
                                        CreatedDate = dateTime,
                                        Pkey = item.ItemNo
                                    });
                                    db.CpnVchBOMStores.AddRange(insertStore);
                                }
                            }

                            db.SaveChanges();
                            transaction.Commit();

                            result = new Tuple<bool, string, CpnVchBOMHeaderRequestModel>(true, $"Cập nhật thành công coupon {item.ItemNo}", model);
                        }

                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, CpnVchBOMHeaderRequestModel>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }

            return result;
        }

        public List<CpnVchBOMHeaderModel> LoadCpnVch(string itemNo, string description, string blocked, out int totalRecord, int skip, int take)
        {
            var result = new List<CpnVchBOMHeaderModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;

                    var data = from b in db.CpnVchBOMHeaders
                               where !(from o in db.CpnVchBOMIssueRules
                                       select o.ItemNo).Contains(b.ItemNo)
                               select new CpnVchBOMHeaderModel
                               {
                                   ItemNo = b.ItemNo,
                                   ItemName = b.ItemName,
                                   UnitOfMeasure = b.UnitOfMeasure,
                                   DiscountType = b.DiscountType,
                                   DiscountValue = b.DiscountValue,
                                   ArticleType = b.ArticleType,
                                   Counter = b.Counter,
                                   Pkey = b.Pkey,
                                   Blocked = b.Blocked,
                                   StartingDate = b.StartingDate,
                                   EndingDate = b.EndingDate,
                                   LastDateModified = b.LastDateModified,
                                   Status = b.Blocked == 1 ? false : DbFunctions.TruncateTime(b.EndingDate) < DbFunctions.TruncateTime(date) ? false : true

                               };

                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(m => m.ItemNo == itemNo);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        data = data.Where(m => m.ItemName.ToLower().Contains(description.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(blocked))
                    {
                        var block = blocked == "0" ? false : true;
                        data = data.Where(m => m.Status == block);
                    }

                    totalRecord = data.Count();
                    result = data.OrderByDescending(d => d.LastDateModified).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
            }
            return result;
        }

        public List<Model.SetupItem.ItemModel> LoadItemByArticleType(string articleType, string itemNo, string itemName, out int totalRecord, int skip, int take)
        {
            var result = new List<Model.SetupItem.ItemModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;

                    var data = db.Items.Where(d => d.ItemFamilyCode == articleType && d.Blocked == 0 && !(from o in db.CpnVchBOMHeaders
                                                                                                          select o.ItemNo).Contains(d.No))
                               .Select(b => new Model.SetupItem.ItemModel
                               {
                                   No = b.No,
                                   No2 = b.No2,
                                   Description = b.Description,
                                   BaseUnitOfMeasure = b.BaseUnitOfMeasure
                               });

                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(m => m.No == itemNo);
                    }
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        data = data.Where(m => m.Description == itemName);
                    }

                    totalRecord = data.Count();
                    result = data.OrderByDescending(d => d.No).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
            }
            return result;
        }
    }
}
