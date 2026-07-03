using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.SetupExtraFee;
using VCM.BLUEPOS.Model.SetupItem;

namespace VCM.BLUEPOS.Data.SetupExtraFee
{
    public interface ISetupExtraFeeData
    {
        List<ExtraFeeHeaderModel> LoadExtraFee(string code, string name, string storeGroup, out int totalRecord, int skip, int take);
        ViewSetupExtraFeeModel ViewInfoExtraFee(string code);
        List<ExtraFeeStroreGroupModel> ListGroupSiteExtraFee(string groupCode, string groupName, out int total, int skip, int take);
        List<StoreExtraFeeModel> ListStoreFeeByGroup(string groupCode, string storeNo, string storeName, out int total, int skip, int take);
        Tuple<bool, string> DeleteGroupSite(string groupSite);
        Tuple<bool, string, ExtraFeeStroreGroupModel> SetupGroupSite(ExtraFeeStroreGroupModel model);
        List<POSGroupModel> ListPOSGroup();
        Tuple<bool, string, SaveExtraFeeRequest> SaveExtraFee(SaveExtraFeeRequest model);
        Tuple<bool, string> DeleteExtraFee(string code);
    }
    public class SetupExtraFeeData : ISetupExtraFeeData
    {
        private readonly object lockRequest = new object();
        public List<ExtraFeeHeaderModel> LoadExtraFee(string code, string name, string storeGroup, out int totalRecord, int skip, int take)
        {
            var result = new List<ExtraFeeHeaderModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var date = DateTime.Now.Date;
                    db.Database.CommandTimeout = 2 * 60;

                    var data = from a in db.ExtraFeeHeaders
                                   //join b in db.ExtraFeeStroreGroups on a.StoreGroup equals b.StoreGroup
                               select new ExtraFeeHeaderModel
                               {
                                   Code = a.Code,
                                   Name = a.Name,
                                   StoreGroup = a.StoreGroup,
                                   StoreGroupName = db.ExtraFeeStroreGroups.FirstOrDefault(d => d.StoreGroup == a.StoreGroup || d.ListStore == a.StoreGroup).StroreGroupName,
                                   SalesType = (from b in db.SalesOrderTypes
                                                join c in db.ExtraFeeOrderTypes on b.Code equals c.SalesOrderType
                                                where c.Code == a.Code
                                                select new SalesOrderTypeModel { Code = b.Code, Description = b.Description }).ToList(),
                                   FromDate = a.FromDate,
                                   ToDate = a.ToDate,
                                   CreatedDate = a.CreatedDate,
                                   ExtraFeeValue = db.ExtraFeeLines.FirstOrDefault(d => d.Code == a.Code) == null ? "" : db.ExtraFeeLines.FirstOrDefault(d => d.Code == a.Code).ExtraFeeValue + " %"
                               };
                    if (!string.IsNullOrEmpty(code))
                    {
                        data = data.Where(m => m.Code == code);
                    }
                    if (!string.IsNullOrEmpty(name))
                    {
                        data = data.Where(m => m.Name.ToLower().Contains(name.ToLower()));
                    }
                    if (!string.IsNullOrEmpty(storeGroup))
                    {
                        data = data.Where(m => m.StoreGroup == storeGroup);
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

        public ViewSetupExtraFeeModel ViewInfoExtraFee(string code)
        {
            var result = new ViewSetupExtraFeeModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var feeHeader = new ViewExtraFeeHeaderModel();
                    var feeListLineNganhHang = new List<ViewExtraFeeLineModel>();
                    var feeListLineCombo = new List<ViewExtraFeeLineModel>();
                    var feeListOrderTypeByCode = new List<ViewEtraFeeOrderTypeModel>();
                    var listOrderType = new List<ViewEtraFeeOrderTypeModel>();
                    var feeListStore = new List<ViewEtraFeeStoreModel>();

                    if (!string.IsNullOrEmpty(code))
                    {
                        feeHeader = db.ExtraFeeHeaders.Where(d => d.Code == code)
                            .Select(a => new ViewExtraFeeHeaderModel
                            {
                                Code = a.Code,
                                Name = a.Name,
                                StoreGroup = a.StoreGroup,
                                FromDate = a.FromDate,
                                ToDate = a.ToDate

                            }).FirstOrDefault();

                        feeListLineNganhHang = (from a in db.ExtraFeeLines
                                                join b in db.PosGroups on a.No equals b.GroupCode
                                                where a.Code == code && a.ItemType == "Group"
                                                select new ViewExtraFeeLineModel
                                                {
                                                    Code = a.Code,
                                                    ItemType = a.ItemType,
                                                    No = a.No,
                                                    ParentCode = b.ParentCode,
                                                    ParentName = b.Description,
                                                    GroupName = b.Description,
                                                    ExtraFeeValue = a.ExtraFeeValue ?? 0
                                                }).ToList();

                        feeListLineCombo = (from a in db.ExtraFeeLines
                                            join b in db.OfferHeaders on a.No equals b.No
                                            where a.Code == code && a.ItemType == "Combo"
                                            select new ViewExtraFeeLineModel
                                            {
                                                Code = a.Code,
                                                ItemType = a.ItemType,
                                                No = a.No,
                                                GroupName = b.Description,
                                                ExtraFeeValue = a.ExtraFeeValue ?? 0
                                            }).ToList();

                        feeListOrderTypeByCode = db.ExtraFeeOrderTypes.Where(d => d.Code == code)
                            .Select(a => new ViewEtraFeeOrderTypeModel
                            {
                                Code = a.Code,
                                SalesOrderType = a.SalesOrderType

                            }).ToList();

                        feeListStore = db.ExtraFeeStores.Where(d => d.Code == code)
                            .Select(a => new ViewEtraFeeStoreModel
                            {
                                Code = a.Code,
                                StoreGroup = a.StoreGroup,
                                StoreNo = a.StoreNo
                            }).ToList();
                    }

                    listOrderType = db.SalesOrderTypes.Where(d => d.IsActive == true)
                        .Select(a => new ViewEtraFeeOrderTypeModel
                        {
                            Code = a.Code,
                            SalesOrderType = a.Description

                        }).ToList();

                    var listPosGroup = db.PosGroups.Where(d => d.Status == 1).Select(a => new POSGroupModel
                    {
                        GroupCode = a.GroupCode,
                        Description = a.Description,
                        Level = a.Level,
                        ParentCode = a.ParentCode,
                        Seq = a.Seq

                    }).ToList();

                    result = new ViewSetupExtraFeeModel
                    {
                        InfoHeader = feeHeader,
                        ListExtraFeeLineNganhHang = feeListLineNganhHang,
                        ListExtraFeeLineCombo = feeListLineCombo,
                        ListExtraFeeOrderTypeByCode = feeListOrderTypeByCode,
                        ListOrderType = listOrderType,
                        ListExtraFeeStore = feeListStore,
                        ListPOSGroup = listPosGroup
                    };
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        public List<ExtraFeeStroreGroupModel> ListGroupSiteExtraFee(string groupCode, string groupName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.ExtraFeeStroreGroups.Where(d => d.Status == true).Select(a => new ExtraFeeStroreGroupModel
                    {
                        StoreGroup = a.ListStore == "ALL" ? "ALL" : a.StoreGroup,
                        StroreGroupName = a.StroreGroupName,
                        ListStore = a.ListStore,
                        Status = a.Status,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser

                    });
                    if (!string.IsNullOrEmpty(groupCode))
                    {
                        data = data.Where(m => m.StoreGroup == groupCode);
                    }
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        data = data.Where(m => m.StroreGroupName.ToLower().Contains(groupName.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<ExtraFeeStroreGroupModel>();
            }
        }

        public List<StoreExtraFeeModel> ListStoreFeeByGroup(string groupCode, string storeNo, string storeName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                var result = new List<StoreExtraFeeModel>();
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkData = db.ExtraFeeStroreGroups.Where(d => d.StoreGroup == groupCode || d.ListStore == groupCode).Select(a => new ExtraFeeStroreGroupModel
                    {
                        StoreGroup = a.StoreGroup,
                        StroreGroupName = a.StroreGroupName,
                        ListStore = a.ListStore

                    }).FirstOrDefault();

                    if (checkData != null)
                    {

                        if (checkData.ListStore == "ALL")
                        {
                            var data = db.Stores.Where(d => d.ClosingMethod == 0).Select(a => new StoreExtraFeeModel
                            {
                                SiteNo = a.No,
                                SiteName = a.Name
                            });
                            if (!string.IsNullOrEmpty(storeNo))
                            {
                                data = data.Where(d => d.SiteNo == storeNo);
                            }
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                data = data.Where(d => d.SiteName.Contains(storeName));
                            }
                            total = data.Count();
                            result = data.OrderBy(d => d.SiteNo).Skip(skip).Take(take).ToList();
                            return result;

                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(checkData.ListStore);
                            var data = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0)
                                       .Select(a => new StoreExtraFeeModel
                                       {
                                           SiteNo = a.No,
                                           SiteName = a.Name

                                       });
                            if (!string.IsNullOrEmpty(storeNo))
                            {
                                data = data.Where(d => d.SiteNo == storeNo);
                            }
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                data = data.Where(d => d.SiteName.Contains(storeName));
                            }
                            total = data.Count();
                            result = data.OrderBy(d => d.SiteNo).Skip(skip).Take(take).ToList();
                            return result;
                        }

                    }
                    else
                    {
                        total = 0;
                        return result;
                    }

                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return result;
            }
        }

        public Tuple<bool, string> DeleteGroupSite(string groupSite)
        {
            var result = new Tuple<bool, string>(false, $"Group site {groupSite} fail");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkGroupSite = db.ExtraFeeHeaders.Any(d => d.StoreGroup == groupSite);
                    if (!checkGroupSite)
                    {
                        var checkDelete = db.ExtraFeeStroreGroups.Where(d => d.StoreGroup.ToLower() == groupSite.ToLower());
                        if (checkDelete != null)
                        {
                            db.ExtraFeeStroreGroups.RemoveRange(checkDelete);
                            db.SaveChanges();
                            result = new Tuple<bool, string>(true, $"Xóa thành công group site {groupSite}");
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại group site {groupSite} trong hệ thống");
                        }
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Đã cài đặt group site {groupSite} này trong phụ thu");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
            }
            return result;
        }

        public Tuple<bool, string, ExtraFeeStroreGroupModel> SetupGroupSite(ExtraFeeStroreGroupModel model)
        {
            var result = new Tuple<bool, string, ExtraFeeStroreGroupModel>(true, "", model);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkGroup = db.ExtraFeeStroreGroups.FirstOrDefault(d => d.StoreGroup == model.StoreGroup);
                    var storeList = db.Stores.ToList();
                    var maxCode = "G0001";
                    var listStoreGroup = db.ExtraFeeStroreGroups.Select(d => d.StoreGroup.Substring(1, d.StoreGroup.Length - 1)).ToList();
                    if (listStoreGroup != null && listStoreGroup.Count > 0)
                    {
                        var sttMax = listStoreGroup.Select(int.Parse).ToList().Max() + 1;
                        if (sttMax < 10)
                        {
                            maxCode = "G000" + sttMax;
                        }
                        else if (sttMax >= 10 && sttMax < 100)
                        {
                            maxCode = "G00" + sttMax;
                        }
                        else if (sttMax >= 100 && sttMax < 1000)
                        {
                            maxCode = "G0" + sttMax;
                        }
                        else
                        {
                            maxCode = "G" + sttMax;
                        }
                    }

                    if (model.ListStore == "ALL")
                    {
                        var insert = new ExtraFeeStroreGroup
                        {
                            StoreGroup = maxCode,
                            StroreGroupName = model.StroreGroupName,
                            CreatedUser = model.CreatedUser,
                            CreatedDate = model.CreatedDate,
                            ListStore = model.ListStore,
                            Status = true
                        };
                        db.ExtraFeeStroreGroups.Add(insert);
                        db.SaveChanges();
                        result = new Tuple<bool, string, ExtraFeeStroreGroupModel>(true, $"Setup thành công group {model.StoreGroup} với {storeList.Count} cửa hàng", model);
                    }
                    else
                    {
                        // model.ListStore = model.ListStore.Split(new char[] { ';', ',', '\n', ' ' });
                        // var listItem = model.ListStore.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                        var listStoreSetup = JsonConvert.DeserializeObject<List<string>>(model.ListStore).Where(d => !string.IsNullOrEmpty(d) &&  d.Split(new string[] { ";",",","\\n", " " }, StringSplitOptions.None).Length > 0).ToList();

                        var listStoreExists = listStoreSetup.Where(d => storeList.Any(a => a.No == d.ToString())).ToList();
                        var listStoretExists = listStoreSetup.Where(d => !storeList.Any(a => a.No == d.ToString())).Where(d => !string.IsNullOrEmpty(d)).ToList();

                        // var messageStoreError = listStoretExists.Count > 0 ? $" và thất bại với cửa hàng ({string.Join(",", listStoretExists) }) do không tồn tại trong hệ thống" : "";

                        if (listStoretExists.Count > 0)
                        {
                            return new Tuple<bool, string, ExtraFeeStroreGroupModel>(false, $"Cửa hàng ({string.Join(",", listStoretExists) }) không tồn tại trong hệ thống, vui lòng kiểm tra lại", model);
                        }

                        var insert = new ExtraFeeStroreGroup
                        {
                            StoreGroup = maxCode,
                            StroreGroupName = model.StroreGroupName,
                            CreatedUser = model.CreatedUser,
                            CreatedDate = model.CreatedDate,
                            ListStore = JsonConvert.SerializeObject(listStoreExists),// model.ListStore,
                            Status = true
                        };
                        db.ExtraFeeStroreGroups.Add(insert);
                        db.SaveChanges();
                        result = new Tuple<bool, string, ExtraFeeStroreGroupModel>(true, $"Setup thành công group {model.StoreGroup} với {listStoreExists.Count} cửa hàng", model);
                    }
                    model.StoreGroup = maxCode;

                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, ExtraFeeStroreGroupModel>(false, JsonConvert.SerializeObject(ex), model);
                }
            }

            return result;
        }

        public List<POSGroupModel> ListPOSGroup()
        {
            var result = new List<POSGroupModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    result = db.PosGroups.Where(d => d.Status == 1).Select(a => new POSGroupModel
                    {
                        GroupCode = a.GroupCode,
                        Description = a.Description,
                        Level = a.Level,
                        ParentCode = a.ParentCode,
                        Seq = a.Seq

                    }).ToList();

                }
                catch (Exception ex)
                {
                }
            }
            return result;
        }

        public Tuple<bool, string, SaveExtraFeeRequest> SaveExtraFee(SaveExtraFeeRequest model)
        {
            var result = new Tuple<bool, string, SaveExtraFeeRequest>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model.ExtraFeeHeader;
                            var modelHealder = db.ExtraFeeHeaders;
                            var checkHeader = modelHealder.FirstOrDefault(d => d.Code == item.ExtraFeeNo);

                            var dateTime = DateTime.Now;
                            var counterHeader = modelHealder.Max(a => a.Counter) ?? 0;
                             
                            if (checkHeader != null)
                            {
                                checkHeader.Name = item.ExtraFeeName;
                                checkHeader.StoreGroup = item.StoreGroup;
                                checkHeader.FromDate = item.FromDate;
                                checkHeader.ToDate = item.ToDate;
                                checkHeader.CreatedUser = item.CreatedUser;
                                checkHeader.ItemNo = item.ItemNo;
                                checkHeader.Counter = counterHeader + 1;
                            }
                            else
                            {
                                var maxCode = "F0001";
                                var listExtraFeeHeader = modelHealder.AsNoTracking().ToList();
                                if (listExtraFeeHeader != null && listExtraFeeHeader.Count > 0)
                                {
                                    var checkCode = listExtraFeeHeader.Select(d => d.Code.Substring(1, d.Code.Length - 1)).ToList();
                                    var maxInc = checkCode.Select(int.Parse).ToList().Max() + 1;
                                    if (maxInc < 10)
                                    {
                                        maxCode = $"F000{maxInc}";
                                    }
                                    else if (maxInc >= 10 && maxInc < 100)
                                    {
                                        maxCode = $"F00{maxInc}";
                                    }
                                    else if (maxInc >= 100 && maxInc < 1000)
                                    {
                                        maxCode = $"F0{maxInc}";
                                    }
                                    else
                                    {
                                        maxCode = $"F{maxInc}";
                                    }
                                }
                                var insertHeader = new ExtraFeeHeader
                                {
                                    Code = maxCode,
                                    Name = item.ExtraFeeName,
                                    StoreGroup = item.StoreGroup,
                                    FromDate = item.FromDate,
                                    ToDate = item.ToDate,
                                    CreatedDate = dateTime,
                                    CreatedUser = item.CreatedUser,
                                    PKey = maxCode,
                                    Counter = counterHeader + 1,
                                    ItemNo = item.ItemNo
                                };
                                db.ExtraFeeHeaders.Add(insertHeader);
                                model.ExtraFeeHeader.ExtraFeeNo = string.IsNullOrEmpty(model.ExtraFeeHeader.ExtraFeeNo) ? maxCode : model.ExtraFeeHeader.ExtraFeeNo;
                            }

                            var listSaleType = item.ArraySalesType;
                            if (listSaleType.Count > 0)
                            {
                                var counterSaleType = db.ExtraFeeOrderTypes.AsNoTracking().Max(a => a.Counter) ?? 0;
                                var checkSaleType = db.ExtraFeeOrderTypes.Where(d => d.Code == model.ExtraFeeHeader.ExtraFeeNo).ToList();
                                if (checkSaleType != null && checkSaleType.Count > 0)
                                {
                                    db.ExtraFeeOrderTypes.RemoveRange(checkSaleType);
                                }

                                var insertSaleType = listSaleType.Select(a => new ExtraFeeOrderType
                                {
                                    Code = model.ExtraFeeHeader.ExtraFeeNo,
                                    SalesOrderType = a,
                                    CreatedDate = dateTime,
                                    CreatedUser = item.CreatedUser,
                                    PKey = model.ExtraFeeHeader.ExtraFeeNo,
                                    Counter = counterSaleType + 1
                                });
                                db.ExtraFeeOrderTypes.AddRange(insertSaleType);
                            }

                            var extraFeeNganhHang = model.ExtraFeeNganhHang;
                            var listAllExtraFeeLineInDB = db.ExtraFeeLines;

                            var listCodeNganhHang = extraFeeNganhHang != null ? extraFeeNganhHang.Select(a => a.No).ToList() : new List<string>();
                            var checkCodeNganhHangExsist = listAllExtraFeeLineInDB.Where(d => d.Code != model.ExtraFeeHeader.ExtraFeeNo && d.ItemType == "Group" && listCodeNganhHang.Any(a => a == d.No));

                            if (checkCodeNganhHangExsist != null && checkCodeNganhHangExsist.Count() > 0)
                            {
                                var showCodeExitst = string.Join(",", checkCodeNganhHangExsist.Select(a => a.No).ToList());
                                return new Tuple<bool, string, SaveExtraFeeRequest>(false, $"Đã tồn tại mã ngành hàng này [{showCodeExitst}] trong hệ thống", model);
                            }

                            var checkLineNganhHang = listAllExtraFeeLineInDB.Where(d => d.Code == model.ExtraFeeHeader.ExtraFeeNo && d.ItemType == "Group").ToList();

                            if (checkLineNganhHang != null && checkLineNganhHang.Count > 0)
                            {
                                db.ExtraFeeLines.RemoveRange(checkLineNganhHang);
                            }

                            var counterExtraLine = db.ExtraFeeLines.Max(a => a.Counter) ?? 0;

                            if (extraFeeNganhHang != null && extraFeeNganhHang.Count > 0)
                            {
                                var insertLineNganhHang = extraFeeNganhHang.Select(a => new ExtraFeeLine
                                {
                                    Code = model.ExtraFeeHeader.ExtraFeeNo,
                                    No = a.No,
                                    ExtraFeeValue = a.ExtraFeeValue,
                                    ItemType = a.ItemType,
                                    CreatedDate = dateTime,
                                    CreatedUser = item.CreatedUser,
                                    PKey = model.ExtraFeeHeader.ExtraFeeNo,
                                    Counter = counterExtraLine + 1
                                });
                                db.ExtraFeeLines.AddRange(insertLineNganhHang);
                            }

                            var extraFeeCombo = model.ExtraFeeCombo;
                            var listCodeCombo = extraFeeCombo != null ? extraFeeCombo.Select(a => a.No).ToList() : new List<string>();
                            var checkCodeComboExsist = listAllExtraFeeLineInDB.Where(d => d.Code != model.ExtraFeeHeader.ExtraFeeNo && d.ItemType == "Combo" && listCodeCombo.Any(a => a == d.No));

                            if (checkCodeComboExsist != null && checkCodeComboExsist.Count() > 0)
                            {
                                var showCodeExitst = string.Join(",", checkCodeComboExsist.Select(a => a.No).ToList());
                                return new Tuple<bool, string, SaveExtraFeeRequest>(false, $"Đã tồn tại mã combo này [{showCodeExitst}] trong hệ thống", model);
                            }
                            var checkLineCombo = listAllExtraFeeLineInDB.Where(d => d.Code == model.ExtraFeeHeader.ExtraFeeNo && d.ItemType == "Combo").ToList();

                            if (checkLineCombo != null && checkLineCombo.Count > 0)
                            {
                                db.ExtraFeeLines.RemoveRange(checkLineCombo);
                            }
                            //var checkLineCombo = db.ExtraFeeLines.Where(d => d.Code == model.ExtraFeeHeader.ExtraFeeNo && d.ItemType == "Combo").ToList();
                            //if (checkLineCombo != null && checkLineCombo.Count > 0)
                            //{
                            //    db.ExtraFeeLines.RemoveRange(checkLineCombo);
                            //}

                            if (extraFeeCombo != null && extraFeeCombo.Count > 0)
                            {
                                var insertLineCombo = extraFeeCombo.Select(a => new ExtraFeeLine
                                {
                                    Code = model.ExtraFeeHeader.ExtraFeeNo,
                                    No = a.No,
                                    ExtraFeeValue = a.ExtraFeeValue,
                                    ItemType = a.ItemType,
                                    CreatedDate = dateTime,
                                    CreatedUser = item.CreatedUser,
                                    PKey = model.ExtraFeeHeader.ExtraFeeNo,
                                    Counter = counterExtraLine + 1
                                });
                                db.ExtraFeeLines.AddRange(insertLineCombo);
                            }
                            var storeGroup = item.StoreGroup;

                            var checkExtraFeeStore = db.ExtraFeeStores.Where(d => d.Code == model.ExtraFeeHeader.ExtraFeeNo).ToList();
                            if (checkExtraFeeStore != null && checkExtraFeeStore.Count > 0)
                            {
                                db.ExtraFeeStores.RemoveRange(checkExtraFeeStore);
                            }

                            if (storeGroup != "ALL")
                            {
                                var counterExtraStore = db.ExtraFeeStores.AsNoTracking().Max(a => a.Counter) ?? 0;
                                var arrayStore = db.ExtraFeeStroreGroups.AsNoTracking().FirstOrDefault(d => d.StoreGroup == storeGroup).ListStore;
                                var listStore = JsonConvert.DeserializeObject<List<string>>(arrayStore);
                                var insertExtraFeeStore = listStore.Select(a => new ExtraFeeStore
                                {
                                    Code = model.ExtraFeeHeader.ExtraFeeNo,
                                    StoreGroup = storeGroup,
                                    StoreNo = a,
                                    CreatedDate = dateTime,
                                    CreatedUser = item.CreatedUser,
                                    PKey = model.ExtraFeeHeader.ExtraFeeNo,
                                    Counter = counterExtraStore + 1
                                });
                                db.ExtraFeeStores.AddRange(insertExtraFeeStore);
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SaveExtraFeeRequest>(true, $"Cập nhật thành phụ thu {item.ExtraFeeName}", model);
                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SaveExtraFeeRequest>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }

        public Tuple<bool, string> DeleteExtraFee(string code)
        {
            var result = new Tuple<bool, string>(false, $"Code={code}");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var checkExtraLine = db.ExtraFeeLines.Any(d => d.Code == code);
                            if (checkExtraLine)
                            {
                                result = new Tuple<bool, string>(false, $"Mã phụ thu này này {code} đã cài đặt phụ thu");
                                return result;
                            }
                            else
                            {
                                var checkExtraFeeSaleType = db.ExtraFeeOrderTypes.Where(d => d.Code == code);
                                if (checkExtraFeeSaleType != null && checkExtraFeeSaleType.Count() > 0)
                                {
                                    db.ExtraFeeOrderTypes.RemoveRange(checkExtraFeeSaleType);
                                }
                                var checkExtraFeeStore = db.ExtraFeeStores.Where(d => d.Code == code);
                                if (checkExtraFeeStore != null && checkExtraFeeStore.Count() > 0)
                                {
                                    db.ExtraFeeStores.RemoveRange(checkExtraFeeStore);
                                }

                                var checkHeader = db.ExtraFeeHeaders.FirstOrDefault(d => d.Code == code);
                                if (checkHeader != null)
                                {
                                    db.ExtraFeeHeaders.Remove(checkHeader);
                                }
                                db.SaveChanges();
                                transaction.Commit();
                                return new Tuple<bool, string>(true, $"Success");
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
    }
}
