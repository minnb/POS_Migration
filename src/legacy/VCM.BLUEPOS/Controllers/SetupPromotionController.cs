using Newtonsoft.Json;
using PLG.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Business.SetupPromotion;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Model.SetupPromotion.SetupSpecialComboModel;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Models;


namespace PLG.Controllers
{
    public class SetupPromotionController : BaseController
    {
        private ISetupPromotionBLO _setupBBBLO { get; set; }
        private IStoreBLO _storeBLO;
        private ICommonBLO _commonBLO;
        public SetupPromotionController(ISetupPromotionBLO setupBBBLO, IStoreBLO storeBLO, ICommonBLO commonBLO)
        {
            _setupBBBLO = setupBBBLO;
            _storeBLO = storeBLO;
            _commonBLO = commonBLO;
        }

        [DisplayName("Cài đặt chương trình khuyến mãi")]
        public ActionResult SetupMain()
        {
            var model = _setupBBBLO.ViewSetupMain();
            return View(model);
        }

        public ActionResult _ViewListOfferHeader()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult ListOfferHeaderLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var offerNo = Request?.Form["OfferNo"];
                var offerName = Request?.Form["OfferName"];
                var approve = Request?.Form["Approve"];
                var recordsTotal = 0;
                var result = _setupBBBLO.LoadSetupPromotionHeader(offerNo, offerName, approve, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<OfferHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<OfferHeaderModel>());
            }
        }

        [HttpPost]
        public ActionResult _SetupAdvance(AdvenceSetupRequest req)
        {
            
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            List<DayModel> daysOfMonth = new List<DayModel>();
            for (int day = 1; day <= 31; day++)
            {
                daysOfMonth.Add(new DayModel { Day = day, DayName = (day < 10 ? "0" + day : "" + day) });
            }
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;


            var startDate = DateTime.ParseExact(req.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(req.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            if (endDate.Subtract(startDate).TotalDays < 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Thời gian từ ngày đến ngày không hợp lệ"
                };
                return Json(result);
            }

            var modelHeader = new OfferHeaderModel
            {
                No = req.BonusBuy,
                SalesType = req.SalesType,
                Description = req.Description,
                Status = int.Parse(req.Status),
                LimitQty = 0,
                VoucherLimitNumber = 0,
                VoucherValidDay = 0,
                PriorityBBY = 1,
                Counter = 0,
                OfferType = req.OfferType,
                LastDateModified = DateTime.Now,
                StartingDate = startDate,
                EndingDate = endDate,
                IsVoucherHeader = req.isVoucherHeader,
                FromTime="",
                ToTime="",
                VoucherFromDate = startDate,
                VoucherToDate=endDate

            };

            var advModel = new AdvanceSetupModel
            {
                ListDayMonth = daysOfMonth,
                isTotalBill = req.isTotalBill,
                isSetupBuy = req.isSetupBuy,
                isSetupGet = req.isSetupGet,
                isVoucher = req.isVoucherHeader,
                GetOfferHeader = modelHeader
            };
            result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "Success",
                ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_SetupAdvance.cshtml", advModel),
                Data = advModel
            };
            return Json(result);
        }

        [HttpPost]
        public ActionResult _SetupAdvance_BK(AdvenceSetupRequest req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;

                List<DayModel> daysOfMonth = new List<DayModel>();
                for (int day = 1; day <= 31; day++)
                {
                    daysOfMonth.Add(new DayModel { Day = day, DayName = (day < 10 ? "0" + day : "" + day) });
                }
                var startDate = DateTime.ParseExact(req.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(req.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                if (endDate.Subtract(startDate).TotalDays < 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Thời gian từ ngày đến ngày không hợp lệ"
                    };
                    return Json(result);
                }

                var modelHeader = new OfferHeaderModel
                {
                    No = req.BonusBuy,
                    SalesType = req.SalesType,
                    Description = req.Description,
                    Status = int.Parse(req.Status),
                    LimitQty = 0,
                    VoucherLimitNumber = 0,
                    VoucherValidDay = 0,
                    PriorityBBY = 1,
                    Counter = 0,
                    OfferType = req.OfferType,
                    LastDateModified = DateTime.Now,
                    StartingDate = startDate,
                    EndingDate = endDate,
                    IsVoucherHeader = req.isVoucherHeader

                };
                //var updateHeader = _setupBBBLO.UpdateOfferHeader(modelHeader);

                var updateHeader = _setupBBBLO.SetupPromotionHeader(modelHeader);
                if (updateHeader.Item1 == false)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tạo được mã chương trình khuyến mãi"
                    };
                }
                else if (updateHeader.Item1 == true)
                {
                    var model = new AdvanceSetupModel
                    {
                        ListDayMonth = daysOfMonth,
                        isTotalBill = req.isTotalBill,
                        isSetupBuy = req.isSetupBuy,
                        isSetupGet = req.isSetupGet,
                        isVoucher = req.isVoucherHeader,
                        GetOfferHeader = updateHeader.Item3
                    };
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_SetupAdvance.cshtml", model),
                        Data = model
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult SaveSetupCTKM(AdvenceSetupRequest req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;

                List<DayModel> daysOfMonth = new List<DayModel>();
                for (int day = 1; day <= 31; day++)
                {
                    daysOfMonth.Add(new DayModel { Day = day, DayName = (day < 10 ? "0" + day : "" + day) });
                }

                var startDate = DateTime.ParseExact(req.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(req.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                if (endDate.Subtract(startDate).TotalDays < 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Thời gian từ ngày đến ngày không hợp lệ"
                    };
                    return Json(result);
                }

                var saveResult = _setupBBBLO.SaveSetupCTKMAll(req);
                if (!saveResult.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = saveResult.Item2
                    };
                }
                else
                {
                    var model = new AdvanceSetupModel
                    {
                        ListDayMonth = daysOfMonth,
                        isTotalBill = req.isTotalBill,
                        isSetupBuy = req.isSetupBuy,
                        isSetupGet = req.isSetupGet,
                        isVoucher = req.isVoucher,
                        GetOfferHeader = saveResult.Item3
                    };
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = saveResult.Item2,
                        Data = model
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult PopupAdvance(string BonusBuy, bool isTotalBill, bool isSetupBuy, bool isSetupGet, bool isVoucher)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                //var offerHeader = _setupBBBLO.GetOfferHeader(BonusBuy);
                var offerHeader = _setupBBBLO.SetupGetOfferHeader(BonusBuy);
                if (offerHeader == null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"BonusBuy này {BonusBuy} không tồn tại trong hệ thống"
                    };
                }
                else
                {
                    //if (offerHeader.Status == 0)
                    //{
                    //    result = new ResultResponse
                    //    {
                    //        Status = HttpStatusCode.BadRequest,
                    //        Message = $"CTKM {BonusBuy} đã active nên không chỉnh sửa"
                    //    };
                    //    return Json(result);
                    //}
                    var year = DateTime.Now.Year;
                    var month = DateTime.Now.Month;
                    //List<DayModel> daysOfMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                    //                 .Select(a => new DayModel { Day = a, DayName = (a < 10 ? "0" + a : "" + a) })
                    //                 .ToList();
                    List<DayModel> daysOfMonth = new List<DayModel>();
                    for (int day = 1; day <= 31; day++)
                    {
                        daysOfMonth.Add(new DayModel { Day = day, DayName = (day < 10 ? "0" + day : "" + day) });
                    }

                    var model = new AdvanceSetupModel
                    {
                        ListDayMonth = daysOfMonth,
                        isTotalBill = isTotalBill,
                        isSetupBuy = isSetupBuy,
                        isSetupGet = isSetupGet,
                        isVoucher = isVoucher,
                        GetOfferHeader = offerHeader
                    };

                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_SetupAdvance.cshtml", model),
                        Data = model
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult InsertUpdateAdvance(OfferHeaderModel modelHeader)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var vcStartDate = string.IsNullOrEmpty(modelHeader.VoucherFromDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(modelHeader.VoucherFromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var vcEndDate = string.IsNullOrEmpty(modelHeader.VoucherToDateStr) ? Convert.ToDateTime("9999-12-31") : DateTime.ParseExact(modelHeader.VoucherToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                modelHeader.VoucherFromDate = vcStartDate;
                modelHeader.VoucherToDate = vcEndDate;
                modelHeader.DayOfWeek = "";
                modelHeader.FromTime = string.IsNullOrEmpty(modelHeader.FromTime) ? "" : modelHeader.FromTime;
                modelHeader.ToTime = string.IsNullOrEmpty(modelHeader.ToTime) ? "" : modelHeader.ToTime;
                modelHeader.LastDateModified = DateTime.Now;

                //var updateHeader = _setupBBBLO.InsertUpdateAdvance(modelHeader);
                var updateHeader = _setupBBBLO.SetupInsertUpdateAdvance(modelHeader);
                if (updateHeader.Item1 == false)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{updateHeader.Item2}"
                    };
                }

                else if (updateHeader.Item1 == true)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        Data = updateHeader.Item3
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult _DetailOfferHeader(string BonusBuy)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                if (string.IsNullOrEmpty(BonusBuy))//khoi tao ban dau
                {
                    OfferHeaderModel offerHeaderModel = new OfferHeaderModel();
                    offerHeaderModel.Status = 2;
                    offerHeaderModel.No = "";
                    offerHeaderModel.Mon = 0;
                    offerHeaderModel.Tue = 0;
                    offerHeaderModel.Wed = 0;
                    offerHeaderModel.Thu = 0;
                    offerHeaderModel.Fri = 0;
                    offerHeaderModel.Sat = 0;
                    offerHeaderModel.Sun = 0;
                    offerHeaderModel.StartingDate = DateTime.Now;

                    var model = new GetBBModel
                    {
                        GetOfferHearder = offerHeaderModel
                    };
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_DetailOfferHeader.cshtml", model),
                        Data = model
                    };
                    return Json(result);
                }
                var offerHeader = _setupBBBLO.SetupGetOfferHeader(BonusBuy);
                if (offerHeader == null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"BonusBuy này {BonusBuy} không tồn tại trong hệ thống"
                    };
                }
                else
                {
                    var model = new GetBBModel
                    {
                        GetOfferHearder = offerHeader
                    };
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_DetailOfferHeader.cshtml", model),
                        Data = model
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult _DetailOfferBuy(string offerNo, bool isSetupBuy, string offerType, bool isTotalBill, bool isSetupGet, string buyLinkCat, string checkMinValue, string totalMinValue)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            if(isSetupBuy == false)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Loại khuyến mãi này không chọn [Sản phẩm mua]"
                };

                return Json(result);
            }
            try
            {
                //var getOfferBuy = _setupBBBLO.GetOfferBuy(offerNo);
                var model = _setupBBBLO.SetupGetOfferBuy(offerNo);
                model.IsSetupGet = isSetupGet;
                model.IsTotalBill = isTotalBill;
                model.OfferType = offerType;
                model.IsSetupBuy = isSetupBuy;

                //var model = new GetBBModel
                //{
                //    OfferNo = offerNo,
                //    IsSetupGet = isSetupGet,
                //    IsTotalBill = isTotalBill,
                //    OfferType = offerType,
                //    IsSetupBuy = isSetupBuy,
                //    BuyLinkCat = buyLinkCat,
                //    CheckMinValue = checkMinValue,
                //    TotalMinValue = totalMinValue,
                //    GetOfferBuy = getOfferBuy
                //};

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_DetailOfferBuy.cshtml", model),
                    Data = model
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult _DetailOfferGet(string offerNo, string offerType, bool isTotalBill, bool isSetupGet)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };

            if (isSetupGet == false)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Loại khuyến mãi này không chọn [Sản phẩm khuyến mãi]"
                };

                return Json(result);
            }
            try
            {
                var getOfferGet = _setupBBBLO.SetupGetOfferGet(offerNo);
                getOfferGet.IsSetupGet = isSetupGet;
                getOfferGet.IsTotalBill = isTotalBill;

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_DetailOfferGet.cshtml", getOfferGet),
                    Data = getOfferGet
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult _DetailOfferSite(string offerNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                
                var getOfferSite = _setupBBBLO.SetupGetOfferSite(offerNo);

                var model = new ViewDetailOfferSiteModel
                {
                    OfferNo = offerNo,
                    GetOfferSite = getOfferSite
                };

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_DetailOfferSite.cshtml", model),
                    Data = model
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult DeleteOfferBuy(string OfferNo, int LineNo, int LineType, string GroupCode)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                // var response = _setupBBBLO.DeleteOfferBuy(OfferNo, LineNo, LineType, GroupCode);
                var response = _setupBBBLO.SetupDeleteOfferBuy(OfferNo, LineNo, LineType, GroupCode);
                if (response.Item1)
                {
                    result.Status = HttpStatusCode.OK;
                    result.Message = response.Item2;
                }
                else
                {
                    result.Status = HttpStatusCode.BadRequest;
                    result.Message = response.Item2;
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult DeleteOfferSite(string OfferNo, string GroupSite)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                // var response = _setupBBBLO.DeleteOfferSite(OfferNo, GroupSite);
                var response = _setupBBBLO.SetupDeleteOfferSite(OfferNo, GroupSite);
                if (response.Item1)
                {
                    result.Status = HttpStatusCode.OK;
                    result.Message = response.Item2;
                }
                else
                {

                    result.Status = HttpStatusCode.BadRequest;
                    result.Message = response.Item2;

                }

                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult DeleteOfferGet(string offerNo, int lineNo, bool isTotalBill, bool isSetupGet, int LineType, string GroupCode)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                // var response = _setupBBBLO.DeleteOfferGet(offerNo, lineNo, isTotalBill, isSetupGet, LineType, GroupCode);
                var response = _setupBBBLO.SetupDeleteOfferGet(offerNo, lineNo, LineType, GroupCode);
                if (response.Item1)
                {
                    result.Status = HttpStatusCode.OK;
                    result.Message = response.Item2;
                }
                else
                {

                    result.Status = HttpStatusCode.BadRequest;
                    result.Message = response.Item2;

                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }


        [HttpPost]

        public ActionResult _AppendOfferBuy( string offerNo,string conditionBuy,string offerType,bool? isTotalBill,bool? isSetupGet,List<int> linenos)
        {
            try
            {
                if (linenos == null || !linenos.Any())
                {
                    return Json(new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Danh sách LineNo rỗng"
                    });
                }

                conditionBuy = conditionBuy == "OR" ? "O" : "A";

                var html = "";

                foreach (var lineno in linenos)
                {
                    var model = new AppendOfferBuyModel
                    {
                        OfferNo = offerNo ?? "",
                        OfferType = offerType ?? "",
                        ConditonBuy = conditionBuy,
                        IsTottalBill = isTotalBill ?? false,
                        IsSetupGet = isSetupGet ?? false,
                        GetOfferBuy = new OfferBuyModel
                        {
                            LineNo = lineno
                        }
                    };

                    html += Ultils.RenderViewToString(
                        ControllerContext,
                        "~/Views/SetupPromotion/_AppendOfferBuy.cshtml",
                        model
                    );
                }

                return Json(new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = "Success",
                    ViewPartial = html
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = ex.Message + " | " + (ex.InnerException?.Message ?? "")
                });
            }
        }

        [HttpPost]
        public ActionResult _AppendOfferGet(string offerNo, string offerType, string conditionGet, bool isTotalBill, bool isSetupGet, float totalBill, List<int> linenos)
        {
            try
            {
                if (linenos == null || !linenos.Any())
                {
                    return Json(new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Danh sách LineNo rỗng"
                    });
                }

                var html = "";

                foreach (var lineno in linenos)
                {
                    var model = new AppendOfferGetModel
                    {
                        OfferNo = offerNo,
                        OfferType = offerType,
                        IsTottalBill = isTotalBill,
                        IsSetupGet = isSetupGet,
                        ConditonGet = conditionGet,
                        TotalBill = totalBill,

                        GetOfferGet = new OfferGetModel
                        {
                            LineNo = lineno
                        }
                    };

                    html += Ultils.RenderViewToString(
                        ControllerContext,
                        "~/Views/SetupPromotion/_AppendOfferGet.cshtml",
                        model
                    );
                }

                return Json(new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = "Success",
                    ViewPartial = html
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = ex.Message + " | " + (ex.InnerException?.Message ?? "")
                });
            }

        }
        [HttpPost]
        public ActionResult _AppendOfferBuy_BK(string offerNo, string conditionBuy, string offerType, bool isTotalBill, bool isSetupGet,int lineno)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };

            try
            {
                var modelUpdate = new OfferBuyModel
                {
                    OfferNo = offerNo,
                    LineType = 0,
                    No = "",
                    Description = "",
                    UnitOfMeasure = "",
                    DiscountType = 0,
                    DiscountValue = 0,
                    Quantity = 0,
                    Step = 0,
                    BonusBuyNo = "",
                    ScaleType = "C",//mặc định
                    Counter = 0

                };
                conditionBuy = conditionBuy == "OR" ? "O" : "A";

                //honglk:22.03.2026
                var model = new AppendOfferBuyModel
                {
                    OfferNo = offerNo,
                    OfferType = offerType,
                    ConditonBuy = conditionBuy,
                    IsTottalBill = isTotalBill,
                    IsSetupGet = isSetupGet,
                    GetOfferBuy = new OfferBuyModel { LineNo = lineno }
                };

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_AppendOfferBuy.cshtml", model),
                    Data = model
                };
                return Json(result);
                //end
                /*
                //var data = _setupBBBLO.InsertUpdateOfferBuy(modelUpdate, isTotalBill, isSetupGet, conditionBuy, offerType, "");
                var data = _setupBBBLO.SetupInsertUpdateOfferBuy(modelUpdate, isTotalBill, isSetupGet, conditionBuy, offerType, "");
                if (data.Item1)
                {
                     model = new AppendOfferBuyModel
                    {
                        OfferNo = offerNo,
                        OfferType = offerType,
                        ConditonBuy = conditionBuy,
                        IsTottalBill = isTotalBill,
                        IsSetupGet = isSetupGet,
                        GetOfferBuy = data.Item3
                    };

                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_AppendOfferBuy.cshtml", model),
                        Data = model
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}"
                    };
                }
                */
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);

        }

        [HttpPost]
        public ActionResult _AppendOfferGet_BK(string offerNo, string offerType, string conditionGet, bool isTotalBill, bool isSetupGet, float totalBill)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };

            try
            {
                var modelUpdate = new OfferGetModel
                {
                    OfferNo = offerNo,
                    LineType = 0,
                    No = "",
                    Description = "",
                    UnitOfMeasure = "",
                    DiscountType = 0,
                    DiscountValue = 0,
                    Quantity = 0,
                    Step = totalBill,
                    BonusBuyNo = "",
                    ScaleType = "C",//mặc định
                    Counter = 0

                };
                //var data = _setupBBBLO.InsertUpdateOfferGetOrBenefit(modelUpdate, isTotalBill, isSetupGet, conditionGet, offerType, "");
                var data = _setupBBBLO.SetupInsertUpdateOfferGet(modelUpdate, conditionGet, "");
                if (data.Item1)
                {
                    var model = new AppendOfferGetModel
                    {
                        OfferNo = offerNo,
                        OfferType = offerType,
                        IsTottalBill = isTotalBill,
                        IsSetupGet = isSetupGet,
                        ConditonGet = conditionGet,
                        TotalBill = totalBill,
                        GetOfferGet = data.Item3
                    };

                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPromotion/_AppendOfferGet.cshtml", model),
                        Data = model
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}"
                    };
                }


            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);

        }

        [HttpPost]
        public ActionResult InsertUpdateOfferGet(OfferGetModel modelUpdate, bool isTotalBill, bool isSetupGet, string conditionGet, string offerType, string itemHidden)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            modelUpdate.Description = (modelUpdate.Description == "undefined" || modelUpdate.Description == null) ? "" : modelUpdate.Description;
            modelUpdate.No = modelUpdate.No == null ? "" : modelUpdate.No;
            modelUpdate.UnitOfMeasure = modelUpdate.UnitOfMeasure == null ? "" : modelUpdate.UnitOfMeasure;
            conditionGet = conditionGet == "OR" ? "O" : "A";
            try
            {
                //var data = _setupBBBLO.InsertUpdateOfferGetOrBenefit(modelUpdate, isTotalBill, isSetupGet, conditionGet, offerType, itemHidden);
                var data = _setupBBBLO.SetupInsertUpdateOfferGet(modelUpdate, conditionGet, itemHidden);
                if (data.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Cập nhật thành công",
                        Data = data.Item3
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}",
                        Data = data.Item3
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"

                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult InsertUpdateOfferBuy(OfferBuyModel modelUpdate, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            modelUpdate.Description = modelUpdate.Description == "undefined" ? "" : modelUpdate.Description;
            modelUpdate.No = modelUpdate.No == null ? "" : modelUpdate.No;
            modelUpdate.UnitOfMeasure = modelUpdate.UnitOfMeasure == null ? "" : modelUpdate.UnitOfMeasure;
            conditionBuy = conditionBuy == "OR" ? "O" : "A";
            try
            {
                var data = _setupBBBLO.SetupInsertUpdateOfferBuy(modelUpdate, isTotalBill, isSetupGet, conditionBuy, offerType, itemHidden);
                if (data.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Cập nhật thành công",
                        Data = data.Item3
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}",
                        Data = data.Item3
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"

                };
            }
            return Json(result);
        }

        public ActionResult _ViewListItemSetup(string offerNo, string offerType, string conditionBuy, string lineNo, string tabOffer, bool isTotalBill, bool isSetupGet)
        {
            var request = new ViewItemRequestModel { OfferNo = offerNo, LineNo = lineNo, TabOffer = tabOffer, Condition = conditionBuy, OfferType = offerType, IsTotalBill = isTotalBill, IsSetupGet = isSetupGet };
            return PartialView(request);
        }

        public ActionResult _ViewListItemGetSetup(string offerNo, string offerType, string conditionGet, string lineNo, string tabOffer, bool isTotalBill, bool isSetupGet, decimal totalBill)
        {
            var request = new ViewItemRequestModel { OfferNo = offerNo, OfferType = offerType, Condition = conditionGet, LineNo = lineNo, TabOffer = tabOffer, IsTotalBill = isTotalBill, IsSetupGet = isSetupGet, TotalBill = totalBill };
            return PartialView(request);
        }

        [HttpPost]
        public ActionResult ListItemSetup()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var itemNo = Request?.Form["No"];
                var mota = Request?.Form["Description"];

                var recordsTotal = 0;
                var result = _setupBBBLO.ListItemSetup(itemNo, mota, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<ItemSetupMode> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<ItemSetupMode>());
            }
        }

        [HttpPost]
        public ActionResult GetItem(string itemNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };

            var recordsTotal = 0;
            var data = _setupBBBLO.ListItemSetup(itemNo, "", out recordsTotal, 0, 1).FirstOrDefault();
            if (data == null)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy thông tin {itemNo}"
                };

            }
            else
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"OK",
                    Data = data
                };
            }
            return Json(result);
        }

        [HttpGet]
        public ActionResult _ViewSetupGroupItemBuy_BK(string groupCode, string offerNo, string offerType, string condition, string lineNo, string tabOffer, bool isTotalBill, bool isSetupGet, string scaleType, string quantiy)
        {
            var data = new ViewSetupGroupItemBuyModel
            {
                GroupCode = groupCode,
                OfferNo = offerNo,
                LineNo = lineNo,
                TabOffer = tabOffer,
                Condition = condition,
                OfferType = offerType,
                IsTotalBill = isTotalBill,
                IsSetupGet = isSetupGet,
                ScaleType = scaleType,
                Quantity = string.IsNullOrEmpty(quantiy) ? "0" : quantiy,
                ListItem = _setupBBBLO.ListItem()
            };

            return PartialView(data);
        }
        [HttpGet]
        public ActionResult _ViewSetupGroupItemBuy(string LineNo, string groupBuyCode)
        {
            var data = new ViewSetupGroupItemBuyModel
            {
                LineNo = LineNo,
                GroupCode = groupBuyCode
                //ListItem = _setupBBBLO.ListItem()
            };

            return PartialView(data);
        }
        [HttpGet]
        public ActionResult _ViewSetupGroupItemGet(string LineNo, string groupGetCode)
        {
            var data = new ViewSetupGroupItemBuyModel
            {
                LineNo = LineNo,
                GroupCode = groupGetCode
                //ListItem = _setupBBBLO.ListItem()
            };

            return PartialView(data);
          
        }

        [HttpGet]
        public ActionResult _ViewSetupGroupItemGet_BK(string groupCode, string offerNo, string offerType, string condition, string lineNo, string tabOffer, bool isTotalBill, bool isSetupGet, decimal totalBill, string scaleType, string quantiy)
        {
            var data = new ViewSetupGroupItemBuyModel
            {
                GroupCode = groupCode,
                OfferNo = offerNo,
                LineNo = lineNo,
                TabOffer = tabOffer,
                Condition = condition,
                OfferType = offerType,
                IsTotalBill = isTotalBill,
                IsSetupGet = isSetupGet,
                TotalBill = totalBill,
                ScaleType = scaleType,
                Quantity = string.IsNullOrEmpty(quantiy) ? "0" : quantiy,
                ListItem = _setupBBBLO.ListItem()
            };

            return PartialView(data);
        }

        public ActionResult SetupBuyGroupItem(string GroupCode, string GroupName, string ListItem)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var createUserName = base.LoginUser.UserName;
                var model = new BuyGroupItemModel
                {
                    GroupCode = GroupCode,
                    GroupName = GroupName,
                    ListItemNo = ListItem,
                    Status = true,
                    CreatedDate = DateTime.Now,
                    CreatedUser = createUserName,
                    LastUpdateDate = DateTime.Now,
                    LastUpdateUser = createUserName
                };
                var response = _setupBBBLO.SetupGroupBuyItem(model);
                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        [HttpGet]
        public ActionResult _ViewDataBuyGroupItem(string groupCode, string offerNo, string offerType, string conditionBuy, string lineNo, string tabOffer, bool isTotalBill, bool isSetupGet, string scaleType)
        {
            var data = new ViewSetupGroupItemBuyModel
            {
                GroupCode = groupCode,
                OfferNo = offerNo,
                LineNo = lineNo,
                TabOffer = tabOffer,
                Condition = conditionBuy,
                OfferType = offerType,
                IsTotalBill = isTotalBill,
                IsSetupGet = isSetupGet,
                ScaleType = scaleType
            };
            return PartialView(data);
        }

        [HttpGet]
        public ActionResult _ViewDataGetGroupItem(string groupCode, string offerNo, string offerType, string conditionGet, string lineNo, string tabOffer, bool isTotalBill, bool isSetupGet, decimal totalBill, string scaleType)
        {
            var data = new ViewSetupGroupItemBuyModel
            {
                GroupCode = groupCode,
                OfferNo = offerNo,
                LineNo = lineNo,
                TabOffer = tabOffer,
                Condition = conditionGet,
                OfferType = offerType,
                IsTotalBill = isTotalBill,
                IsSetupGet = isSetupGet,
                TotalBill = totalBill,
                ScaleType = scaleType
            };
            return PartialView(data);
        }

        [HttpPost]
        public ActionResult LoadBuyGroupItem()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var groupCode = Request?.Form["GroupCode"];
                var groupName = Request?.Form["GroupName"];

                var recordsTotal = 0;
                var result = _setupBBBLO.LoadBuyGroup(groupCode, groupName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<BuyGroupItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<BuyGroupItemModel>());
            }
        }

        [HttpGet]
        public ActionResult _ViewListIem(string groupCode)
        {
            ViewBag.GroupCode = groupCode;
            return PartialView();
        }

        [HttpGet]
        public ActionResult _ViewListIemGet(string groupCode)
        {
            ViewBag.GroupCode = groupCode;
            return PartialView();
        }

        [HttpPost]
        public ActionResult LoadListItemBuySetup()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var itemNo = Request?.Form["No"];
                var itemName = Request?.Form["Description"];
                var groupCode = Request?.Form["GroupCode"];

                var recordsTotal = 0;
                var result = _setupBBBLO.LoadListItemBuySetup(groupCode, itemNo, itemName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<ItemSetupMode> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<ItemSetupMode>());
            }
        }


        [HttpGet]
        public ActionResult _ViewSetupCHST(string offerNo)
        {
            var listStore = _storeBLO.ListStorePartner();
            var data = new ViewSetupCHSTModel
            {
                OfferNo = offerNo,
                ListStore = listStore
            };

            return PartialView(data);
        }

        [HttpPost]
        public ActionResult SetupGroupCode(string GroupCode, string GroupName, string ListStore, int IsAllStore)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var createUserName = base.LoginUser.UserName;
                var allStore = IsAllStore == 1 ? "ALL" : ListStore;
                var model = new GroupSiteModel
                {
                    GroupCode = GroupCode,
                    GroupName = GroupName,
                    ListStore = allStore,
                    Status = true,
                    CreatedDate = DateTime.Now,
                    CreatedUser = createUserName,
                    LastUpdateDate = DateTime.Now,
                    LastUpdateUser = createUserName
                };
                var response = _setupBBBLO.SetupGroupSite(model);
                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        [HttpGet]
        public ActionResult _ViewSelectGroupSite(string offerNo)
        {
            ViewBag.OfferNo = offerNo;
            return PartialView();
        }

        [HttpGet]
        public ActionResult _ViewDataGroupSite(string offerNo)
        {
            ViewBag.OfferNo = offerNo;
            return PartialView();
        }

        [HttpGet]
        public ActionResult _AddStoreGroup_ViewDataGroupSite(string offerNo)
        {
            ViewBag.OfferNo = offerNo;
            return PartialView();
        }

        [HttpGet]
        public ActionResult _ViewListStore(string groupCode)
        {
            ViewBag.GroupCode = groupCode;
            return PartialView();
        }

        [HttpGet]
        public ActionResult _ViewListStoreSetup(string groupCode)
        {
            ViewBag.GroupCode = groupCode;
            return PartialView();
        }

        [HttpPost]
        public ActionResult ListGroupSite()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var groupCode = Request?.Form["GroupCode"];
                var groupName = Request?.Form["GroupName"];

                var recordsTotal = 0;
                var result = _setupBBBLO.ListGroupSiteSetup(groupCode, groupName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<GroupSiteModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<ItemSetupMode>());
            }
        }
        [HttpPost]
        public ActionResult LoadStoreSetup()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var siteNo = Request?.Form["SiteNo"];
                var siteName = Request?.Form["SiteName"];
                var groupCode = Request?.Form["GroupCode"];

                var recordsTotal = 0;
                var result = _setupBBBLO.ListStoreSetup(groupCode, siteNo, siteName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<StoreSetupModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<StoreSetupModel>());
            }
        }

        [HttpPost]
        public ActionResult InsertUpdateOfferSite(string OfferNo, string SiteGroupCode)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            if (string.IsNullOrEmpty(OfferNo))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $""
                };
                return Json(result);
            }
            try
            {
                // var listGroupCode = JsonConvert.DeserializeObject<List<string>>(JsonGroupCode);

                if (string.IsNullOrEmpty(SiteGroupCode))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn nhóm cửa hàng"
                    };
                }
                else
                {
                    var data = _setupBBBLO.SetupInsertUpdateOfferSite(OfferNo, SiteGroupCode);
                    if (data.Item1)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = $"{data.Item2}"
                        };
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"{data.Item2}",
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}"

                };
            }
            return Json(result);
        }


        [HttpPost]
        public ActionResult SetupHeaderBuy(string OfferNo, string BuyLinkCat, string CheckMinValue, string MinValue)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                var linkCat = BuyLinkCat == "OR" ? "O" : "A";
                var data = _setupBBBLO.SetupInsertUpdateHeaderBuy(OfferNo, linkCat, CheckMinValue, MinValue);
                if (data.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success"
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}",
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult SetupHeaderGet(string OfferNo, string GetLinkCat, string CheckDiscount, string TypeDiscount, string DiscountDiscount)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                TypeDiscount = TypeDiscount == "0" ? "%" : TypeDiscount == "1" ? "R" : TypeDiscount == "2" ? "P" : "";
                var linkCat = GetLinkCat == "OR" ? "O" : "A";
                var data = _setupBBBLO.SetupInsertUpdateHeaderGet(OfferNo, linkCat, CheckDiscount, TypeDiscount, DiscountDiscount);
                if (data.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success"
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}",
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult DeleteOfferGetByOfferNo(string offerNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
                var response = _setupBBBLO.SetupDeleteOfferGetByOfferNo(offerNo);
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult DeleteGroupSite(string groupSite)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
                var response = _setupBBBLO.SetupDeleteGroupSite(groupSite);
                if (response.Item1)
                {
                    result.Status = HttpStatusCode.OK;
                    result.Message = response.Item2;
                }
                else
                {
                    result.Status = HttpStatusCode.BadRequest;
                    result.Message = response.Item2;
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }
        [HttpPost]
        public ActionResult DeleteGroupItem(string groupCode)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
                var response = _setupBBBLO.SetupDeleteGroupItem(groupCode);
                if (response.Item1)
                {
                    result.Status = HttpStatusCode.OK;
                    result.Message = response.Item2;
                }
                else
                {
                    result.Status = HttpStatusCode.BadRequest;
                    result.Message = response.Item2;
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult UpdateStatusPromotion(string offerNo, string status)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                var finish = _setupBBBLO.UpdateStatusPromotion(offerNo, status);
                if (finish.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = finish.Item2
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadGateway,
                        Message = finish.Item2
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult FinishSetupPromotion(string offerNo, string status, string salesType, string offerName, string tuNgay, string denNgay, bool isVoucher, bool isApprove)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            try
            {
                var startDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                //var getOfferSite = _setupBBBLO.GetOfferSite(offerNo);
                var finish = _setupBBBLO.FinishSetupPromotion(offerNo, status, salesType, offerName, startDate.ToString("yyyyMMdd"), endDate.ToString("yyyyMMdd"), isVoucher, isApprove);
                if (finish.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = finish.Item2
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadGateway,
                        Message = finish.Item2
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }


        //********* 12/06/2024 ***********
      
        [DisplayName("Khai báo Special Combo")]
        public ActionResult SetupSpecialComboList()
        {
            var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            var listSetDB = listStoreByUser
                .GroupBy(d => new { d.ServerIP, d.ServerIPRead, d.ServerStoreDesc })
                .Select(a => new SQLServerModel
                {
                    IPServer = a.FirstOrDefault().ServerIP,
                    IPServerRead = a.FirstOrDefault().ServerIPRead,
                    Description = a.FirstOrDefault().ServerStoreDesc
                }).Distinct().ToList();

            var captionSaleType = "SALESTYPEREPORT";
            ViewBag.SetDB = listSetDB;  
            ViewBag.ListStore = _storeBLO.ListStorePartner();
            ViewBag.ListSalesType = _commonBLO.GetComboxOptionData(captionSaleType);
            ViewBag.ListCode = _setupBBBLO.LoadCodeBySpecialComboHeader();
            ViewBag.ListItem = _setupBBBLO.LoadComboxItemByComboLine();
            ViewBag.ListMemberCode = _commonBLO.LoadComboxMemberCodeType(); // Loại hạng thẻ
            ViewBag.ListUOM = _setupBBBLO.GetListComboxUOM();
            return View();
        }
        public ActionResult LoadComboxItemByComboLine()
        {
            var data = _setupBBBLO.LoadComboxItemByComboLine();
            return Json(data);
        }

        public ActionResult GenerateRandomCode()
        {
            var dateTime = DateTime.UtcNow.ToUniversalTime();
            var dto = VCM.BLUEPOS.Helpers.DateTimeExtension.ToUnixTimeMilliSeconds(dateTime);
            var data = _setupBBBLO.GenerateRandomCode(dto);
            return Json(data);
        }

        public ActionResult LoadComboxUOM(string ItemNo)
        {
            var data = _setupBBBLO.LoadComboxUOM(ItemNo);
            return Json(data);
        }

        public ActionResult LoadComboxItem()
        {
            var data = _setupBBBLO.LoadComboxItem();
            return Json(data);
        }
        public ActionResult LoadComboxPrice(string ItemNo,string UOM)
        {
            var data = _setupBBBLO.LoadComboxPrice(ItemNo,UOM);
            return Json(data);
        }

        public ActionResult LoadComboxPriceV2(string ItemNo)
        {
            var data = _setupBBBLO.LoadComboxPriceV2(ItemNo);
            return Json(data);
        }

        [HttpPost]
        public ActionResult LoadComboxItemV2(string searchText, int? page, int? pageSize, string itemNo)
        {
            int countTotal = 0;
            searchText = searchText ?? string.Empty;
            var data = _setupBBBLO.LoadComboxItemV2(searchText, page ?? 1, pageSize ?? 20, out countTotal, itemNo);
            return Json(new { data = data, count = countTotal });
        }

        [HttpPost]
        public ActionResult GetSpecialComboHeaderList()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var skip = start != null ? Convert.ToInt32(start) : 1;
                var pageNumber = skip / pageSize;
                var recordsTotal = 0;

                var storeNo = Request?.Form["StoreNo"];
                var salesType = Request?.Form["SalesType"];
                var memberType = Request?.Form["MemberType"];
                var status = Request?.Form["Status"];
                var textSearch = Request?.Form["TextSearch"];

                var result = _setupBBBLO.GetSpecialComboHeaderList("","",storeNo,textSearch,salesType, memberType, status, out recordsTotal, pageNumber, pageSize);
                return Json(new DataTablesViewModel<GetSpecialComboHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch(Exception ex)
            {
                return View(new List<GetSpecialComboHeaderModel>());
            }
        }

        public ActionResult GetDetailSpecialComboList()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var skip = start != null ? Convert.ToInt32(start) : 1;
                var pageNumber = skip / pageSize;
                var recordsTotal = 0;

                var code = Request?.Form["Code"];
                var storeNo = Request?.Form["StoreNo"];
                var textSearch = Request?.Form["TextSearch"];
                var status = Request?.Form["Status"];
                var checkSearch = Request?.Form["CheckSearch"];
                var result = new List<DetailSpecialComboModel>();

                if (checkSearch == "true")
                {
                    result = _setupBBBLO.GetDetailSpecialComboList("", storeNo, status, textSearch, out recordsTotal, pageNumber, pageSize);
                }
                else
                {
                    result = _setupBBBLO.GetDetailSpecialComboList(code, storeNo, status, textSearch, out recordsTotal, pageNumber, pageSize);
                }

                //var result = _setupBBBLO.GetDetailSpecialComboList(code,status,textSearch, out recordsTotal, skip, pageSize);

                return Json(new DataTablesViewModel<DetailSpecialComboModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return View(new List<DetailSpecialComboModel>());
            }
        }

        public ActionResult GetDetailItemNoByCombo()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var skip = start != null ? Convert.ToInt32(start) : 1;
                var pageNumber = skip / pageSize;
                var recordsTotal = 0;

                var comboCode = Request?.Form["ComboCode"]; 
                var result = new List<DetailSpecialComboModel>();

                result = _setupBBBLO.GetDetailItemNoByCombo(comboCode, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<DetailSpecialComboModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return View(new List<DetailSpecialComboModel>());
            }
        }

        public ActionResult CreateSpecialCombo(CreateSetupSpecialComboModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var createUserName = base.LoginUser.UserName;
                var model = new CreateSetupSpecialComboModel
                {
                    Code = req.Code,
                    SalesType = req.SalesType,
                    StoreNo = req.StoreNo,
                    StatusStore = req.StatusStore,
                    Name = req.Name,
                    ComboQuantity = req.ComboQuantity,
                    Amount = req.Amount, // gia tri combo
                    IsMember = req.IsMember,
                    MemberCode = req.MemberCode,
                    FromDate = Convert.ToDateTime(req.FromDate),
                    ToDate = Convert.ToDateTime(req.ToDate),
                    IsEnable = req.IsEnable,
                    CreatedDate = DateTime.Now,
                    CreatedBy = createUserName,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = createUserName,

                    GroupCode = req.GroupCode,
                    GroupName = req.GroupName,
                    ItemNo = req.ItemNo,
                    ItemName = req.ItemName,
                    UOM = req.UOM,
                    MinQuantity = req.MinQuantity,
                    MaxQuantity = req.MaxQuantity,
                    Order = req.Order,
                    IsDefault = req.IsDefault,
                    IsSendSAP = req.IsSendSAP,
                    IsDynamicPrice = req.IsDynamicPrice,
                    IsEnableLine = req.IsEnableLine
                };

                var response = _setupBBBLO.CreateSpecialCombo(model);

                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        public ActionResult CreateSpecialComboV2(CreateSetupSpecialComboModel req, string ListComboLine)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var createUserName = base.LoginUser.UserName;
                var listComboLine = JsonConvert.DeserializeObject<List<CreateSetupSpecialComboLineModel>>(ListComboLine);

                foreach (var item in listComboLine)
                {
                    if (string.IsNullOrEmpty(item.GroupCode))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng nhập mã nhóm"
                        };
                        return Json(result);
                    }

                    if (string.IsNullOrEmpty(item.GroupName))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng nhập tên nhóm"
                        };
                        return Json(result);
                    }

                    if (string.IsNullOrEmpty(item.MinimumQuantity))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng nhập số lượng (Min)"
                        };
                        return Json(result);
                    }

                    if (string.IsNullOrEmpty(item.MaximumQuantity))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng nhập số lượng (Max)"
                        };
                        return Json(result);
                    }

                    if (string.IsNullOrEmpty(item.ItemNo))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng chọn sản phẩm"
                        };
                        return Json(result);
                    }

                    if (string.IsNullOrEmpty(item.UOM))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng chọn đơn vị tính"
                        };
                        return Json(result);
                    }

                    if (string.IsNullOrEmpty(item.IsDynamicPrice))
                    {
                        result = new ResultResponse
                        {
                            Status = 0,
                            Message = $"Vui lòng chọn loại sản phẩm"
                        };
                        return Json(result);
                    }
                }

                // Check trong 1 group code không được trùng sản phẩm, và chỉ duy nhất 1 sản phẩm (nếu có) có trạng thái isDynamic = true
                //var listItem = listComboLine.GroupBy(x => new {x.GroupCode, x.ItemNo}, (key, g) => new
                //{
                //    GroupCode = key.GroupCode,
                //    ItemNo = key.ItemNo,
                //    ListData= g.ToList()
                //}).Where(x => x.ListData.Count > 1)
                //.Select(x => $"{x.GroupCode}-{ x.ListData.FirstOrDefault()?.ItemNo}"
                //).ToList();


                // Check trong 01 groupcode không được trùng sản phẩm

                var listItem = listComboLine.GroupBy(x => new {x.GroupCode, x.ItemNo}, (key, g) => new
                {
                    GroupCode = key.GroupCode,
                    ItemNo = key.ItemNo,
                    ListData= g.ToList()
                }).Where(x => x.ListData.Count > 1)
                .Select(x => $"{x.GroupCode}-{ x.ListData.FirstOrDefault()?.ItemNo}"
                ).ToList();

                if (listItem.Count > 0)
                {
                    var errorData = string.Join("|", listItem).TrimEnd();
                    result = new ResultResponse
                    {
                        Status = 0,
                        Message = $"Danh sách sản phẩm {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại"
                    };
                    return Json(result);
                }

                // Check trong 01 Combo chỉ có duy nhât 1 san pham mac dinh co gia ban dong (isDynamic = true)

                var checkIsDynamicPrice = listComboLine.GroupBy(x => new {x.Code, x.IsDynamicPrice}, (key, g) => new
                {
                    GroupCode = key.Code,
                    IsDynamicPrice = key.IsDynamicPrice,
                    ListData= g.ToList()
                }).Where(x => x.ListData.Count > 1 && x.IsDynamicPrice == "2") // san pham mac dinh co gia ban dong
                      .Select(x => $"{x.GroupCode}-{ x.ListData.FirstOrDefault()?.ItemNo}"
                ).ToList();
                
                if (checkIsDynamicPrice.Count > 0)
                {
                    var errorData = string.Join("|", checkIsDynamicPrice).TrimEnd();
                    result = new ResultResponse
                    {
                        Status = 0,
                        Message = $"Sản phẩm {errorData} này bị trùng thông tin sản phẩm mặc định có giá bán động. Vui lòng kiểm tra lại"
                    };
                    return Json(result);
                }

                var model = new CreateSetupSpecialComboModel
                {
                    Code = req.Code,
                    SalesType = req.SalesType,
                    StoreNo = req.StoreNo,
                    StatusStore = req.StatusStore,
                    Name = req.Name,
                    ComboQuantity = req.ComboQuantity,
                    Amount = req.Amount,
                    IsMember = req.IsMember,
                    MemberCode = req.MemberCode,
                    FromDate = Convert.ToDateTime(req.FromDate),
                    ToDate = Convert.ToDateTime(req.ToDate),
                    FromDateStr = req.FromDate.ToString(),
                    ToDateStr = req.ToDate.ToString(),
                    IsEnable = req.IsEnable,
                    CreatedDate = DateTime.Now,
                    CreatedBy = createUserName,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = createUserName,
                    ListComboLine = listComboLine
                };

                var response = _setupBBBLO.CreateSpecialComboV2(model);

                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        public ActionResult UpdateSpecialComboV2(UpdateSetupSpecialComboModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var userName = base.LoginUser.UserName;

                var model = new UpdateSetupSpecialComboModel
                {
                    Code = req.Code,
                    SalesType = req.SalesType,
                    Name = req.Name,
                    ComboQuantity = req.ComboQuantity,
                    Amount = req.Amount,
                    IsMember = req.IsMember,
                    MemberCode = req.MemberCode,
                    FromDate = Convert.ToDateTime(req.FromDate),
                    ToDate = Convert.ToDateTime(req.ToDate),
                    FromDateStr = req.FromDate.ToString(),
                    ToDateStr = req.ToDate.ToString(),
                    IsEnable = req.IsEnable,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = userName
                };

                var response = _setupBBBLO.UpdateSpecialComboV2(model);

                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        // New : 17/07/2024
        public ActionResult UpdateSpecialComboV3(UpdateSetupSpecialComboModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                if (req.StoreNo == null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Vui lòng chọn cửa hàng"
                    };
                    return Json(result);
                }

                if (req.StatusStore == null || req.StatusStore == "")
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Vui lòng chọn trạng thái"
                    };
                    return Json(result);
                }

                if (req.CheckOptionStore == "1")
                {
                    var listArrayStore = req.StoreNoStr.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    //req.StoreNo = JsonConvert.DeserializeObject<List<string>>(listArrayStore);
                    req.StoreNo = listArrayStore;
                }

                var userName = base.LoginUser.UserName;
                var model = new UpdateSetupSpecialComboModel
                {
                    Code = req.Code,
                    SalesType = req.SalesType,
                    Name = req.Name,
                    StoreNo = req.StoreNo,
                    ComboQuantity = req.ComboQuantity,
                    Amount = req.Amount,
                    IsMember = req.IsMember,
                    MemberCode = req.MemberCode,
                    FromDate = Convert.ToDateTime(req.FromDate),
                    ToDate = Convert.ToDateTime(req.ToDate),
                    FromDateStr = req.FromDate.ToString(),
                    ToDateStr = req.ToDate.ToString(),
                    IsEnable = req.IsEnable,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = userName,
                    CheckOptionStore = req.CheckOptionStore,
                    StatusStore = req.StatusStore
                };

                var response = _setupBBBLO.UpdateSpecialComboV3(model);

                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {

                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }
        public ActionResult UpdateItemNoByGroup(UpdateItemNoByGroupModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var userName = base.LoginUser.UserName;
                var model = new UpdateItemNoByGroupModel
                {
                    Code = req.Code,
                    GroupCode = req.GroupCode,
                    GroupName = req.GroupName,
                    ItemNo = req.ItemNo,
                    ItemName = req.ItemName,
                    UOM = req.UOM,
                    IsDynamicPrice = req.IsDynamicPrice,
                    MinQuantity = req.MinQuantity,
                    MaxQuantity = req.MaxQuantity,
                    Order = req.Order,              
                    IsEnable = req.IsEnable,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = userName
                };

                var response = _setupBBBLO.UpdateItemNoByGroup(model);

                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        public ActionResult AddItemNoByGroupCode(UpdateItemNoByGroupModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var userName = base.LoginUser.UserName;
                var model = new UpdateItemNoByGroupModel
                {
                    Code = req.Code,
                    GroupCode = req.GroupCode,
                    GroupName = req.GroupName,
                    ItemNo = req.ItemNo,
                    ItemName = req.ItemName,
                    UOM = req.UOM,
                    IsDynamicPrice = req.IsDynamicPrice,
                    MinQuantity = req.MinQuantity,
                    MaxQuantity = req.MaxQuantity,
                    Order = req.Order,
                    IsEnable = req.IsEnable,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = userName
                };

                var response = _setupBBBLO.AddItemNoByGroupCode(model);

                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }
        public ActionResult DeleteItemNoByGroupCode(DeleteItemNoByGroupModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK, // OK
                Message = "",
                Data = null
            };

            try
            {
                // var listItem = ListItem.Split(new char[] { ';', ',', '\n', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                // var listArray = JsonConvert.DeserializeObject<List<string>>(listItem);

                var userName = base.LoginUser.UserName;
                var model = new DeleteItemNoByGroupModel
                {
                    Code = req.Code,
                    GroupCode = req.GroupCode,
                    ItemNo = req.ItemNo,
                    UOM = req.UOM
                };

                var response = _setupBBBLO.DeleteItemNoByGroupCode(model);
                if (!response.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{response.Item2}",
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {JsonConvert.SerializeObject(ex)}",
                };
                return Json(result);
            }
        }

        [HttpPost]
        public JsonResult UpdateStatusComboHeader(string code, string status)
        {
            try
            {
                var data = _setupBBBLO.UpdateStatusComboHeader(code, status);
                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!"
                });
            }
        }

        public ActionResult GetStoreSpecialCombo()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var skip = start != null ? Convert.ToInt32(start) : 1;
                var pageNumber = skip / pageSize;
                var recordsTotal = 0;

                var code = Request?.Form["Code"];
                var textSearch = Request?.Form["TextSearch"];
                if (!string.IsNullOrEmpty(textSearch))
                {
                    if (textSearch != code)
                    {
                        code = "";
                    }
                    textSearch = textSearch.Trim();
                }

                var status = Request?.Form["Status"];
                var checkSearch = Request?.Form["CheckSearch"];
                var result = new List<SpecialComboStoreModel>();
                if (checkSearch == "true")
                {
                    result = _setupBBBLO.GetStoreSpecialCombo("", status, textSearch, out recordsTotal, pageNumber, pageSize);
                }
                else
                {
                    result = _setupBBBLO.GetStoreSpecialCombo(code, status, textSearch, out recordsTotal, pageNumber, pageSize);
                }  
                return Json(new DataTablesViewModel<SpecialComboStoreModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return View(new List<SpecialComboStoreModel>());
            }
        }

        [HttpPost]
        public JsonResult UpdateStatusStore(string code, string storeno, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "Vui lòng chọn mã combo"
                    });
                }

                if (string.IsNullOrEmpty(storeno))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "Vui lòng chọn cửa hàng"
                    });
                }

                var data = _setupBBBLO.UpdateStatusStore(code, storeno, status,base.LoginUser.UserName);

                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!"
                });
            }
        }






    }
}