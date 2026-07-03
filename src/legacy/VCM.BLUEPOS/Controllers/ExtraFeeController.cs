using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Business.SetupExtraFee;
using VCM.BLUEPOS.Business.SetupPromotion;
using VCM.BLUEPOS.Model.SetupExtraFee;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class ExtraFeeController : BaseController
    {
        private ISetupExtraFeeBLO _setupExtraFeeBLO { get; set; }
        private ISetupPromotionBLO _setupBBBLO { get; set; }
        private IStoreBLO _storeBLO;
        public ExtraFeeController(ISetupExtraFeeBLO setupExtraFeeBLO, IStoreBLO storeBLO, ISetupPromotionBLO setupBBBLO)
        {
            _setupExtraFeeBLO = setupExtraFeeBLO;
            _storeBLO = storeBLO;
            _setupBBBLO = setupBBBLO;
        }

        //[DisplayName("Cài đặt phụ thu")]
        public ActionResult SetupExtraFee()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SetupExtraFeeLoad()
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

                var code = Request?.Form["Code"];
                var name = Request?.Form["Name"];
                var storeGroup = Request?.Form["StoreGroup"];

                var recordsTotal = 0;
                var result = _setupExtraFeeBLO.LoadExtraFee(code, name, storeGroup, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<ExtraFeeHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<ExtraFeeHeaderModel>());
            }
        }

        [HttpGet]
        public ActionResult _SetupExtraFee(string Id)
        {
            var model = new ViewSetupExtraFeeModel();
            try
            {
                model = _setupExtraFeeBLO.ViewInfoExtraFee(Id);
            }
            catch (Exception ex)
            {
            }
            return PartialView(model);
        }

        [HttpGet]
        public ActionResult _ViewSetupCHST(string offerNo)
        {
            var listStore = _storeBLO.ListStorePartner();
            return PartialView(listStore);
        }

        [HttpGet]
        public ActionResult _ViewDataGroupSite()
        {
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

                var groupCode = Request?.Form["StoreGroup"];
                var groupName = Request?.Form["StroreGroupName"];

                var recordsTotal = 0;
                var result = _setupExtraFeeBLO.ListGroupSiteExtraFee(groupCode, groupName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<ExtraFeeStroreGroupModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<ExtraFeeStroreGroupModel>());
            }
        }

        [HttpGet]
        public ActionResult _ViewListStore(string groupCode)
        {
            ViewBag.GroupCode = groupCode;
            return PartialView();
        }
        [HttpPost]
        public ActionResult LoadStoreByGroup()
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
                var result = _setupExtraFeeBLO.ListStoreFeeByGroup(groupCode, siteNo, siteName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<StoreExtraFeeModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<StoreExtraFeeModel>());
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
                var response = _setupExtraFeeBLO.DeleteGroupSite(groupSite);
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
                var allStore = IsAllStore == 1 ? "ALL" : ListStore.Replace("\\n", "");


                var model = new ExtraFeeStroreGroupModel
                {
                    StoreGroup = GroupCode,
                    StroreGroupName = GroupName,
                    ListStore = allStore,
                    Status = true,
                    CreatedDate = DateTime.Now,
                    CreatedUser = createUserName,
                };
                var response = _setupExtraFeeBLO.SetupGroupSite(model);
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
                        Data = response.Item3
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
        public ActionResult _ViewNganhHang(string JsonNganhHangSelected)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };
            try
            {
                var listNganhHang = _setupExtraFeeBLO.ListPOSGroup();
                var listNganhHangSelected = string.IsNullOrEmpty(JsonNganhHangSelected) ? new List<JsonExtraFeeSelected>() : JsonConvert.DeserializeObject<List<JsonExtraFeeSelected>>(JsonNganhHangSelected);
                var model = new ViewNganhHangModel
                {
                    ListNganhHang = listNganhHang,
                    ListNganhHangSelected = listNganhHangSelected

                };

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/ExtraFee/_ViewNganhHang.cshtml", model),
                    Data = model
                };
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
            //return PartialView(model);
        }

        [HttpPost]
        public ActionResult _ViewCombo(string JsonComboSelected)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };
            try
            {
                var listComboSelected =string.IsNullOrEmpty(JsonComboSelected) ? new List<JsonExtraFeeSelected>() : JsonConvert.DeserializeObject<List<JsonExtraFeeSelected>>(JsonComboSelected);
                var model = new ViewComboModel
                {
                    ListComboSelected = listComboSelected
                };

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/ExtraFee/_ViewCombo.cshtml", model),
                    Data = model
                };
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
        public ActionResult LoadExtraFeeCombo()
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
                var comboSelected = Request?.Form["ComboSelected"];
                //comboSelected =st comboSelected.Split(new string[] { ";", ",", "\\n", " " }, StringSplitOptions.None);
                var listArrayComboSelected = new List<string>();
                if (!string.IsNullOrEmpty(comboSelected) && comboSelected !="\"\"")
                {
                    listArrayComboSelected = JsonConvert.DeserializeObject<List<string>>(comboSelected).Where(d => !string.IsNullOrEmpty(d) && d.Split(new string[] { ";", ",", "\\n", " " }, StringSplitOptions.None).Length > 0).ToList();

                    //listArrayComboSelected = JsonConvert.DeserializeObject<List<string>>(comboSelected);
                    if (listArrayComboSelected == null)
                    {
                        listArrayComboSelected = new List<string>();
                    }
                }
                var recordsTotal = 0;
                var result = _setupBBBLO.LoadExtraFeeCombo(listArrayComboSelected, offerNo, offerName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<OfferHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<OfferHeaderModel>());
            }
        }

        [HttpPost]
        public JsonResult SaveExtraFee(string jsonExtraFeeHeader, string jsonExtraFeeNganhHang, string jsonExtraFeeCombo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                var modelHeader = JsonConvert.DeserializeObject<List<SaveExtraFeeHeaderRequest>>(jsonExtraFeeHeader);
                var modelNganhHang = JsonConvert.DeserializeObject<List<SaveExtraFeeLineRequest>>(jsonExtraFeeNganhHang);
                var modelCombo = JsonConvert.DeserializeObject<List<SaveExtraFeeLineRequest>>(jsonExtraFeeCombo);
                //var modelCup = JsonConvert.DeserializeObject<List<CupRequestModel>>(jsonCup);
                //var modelOption = JsonConvert.DeserializeObject<List<OptionRequestModel>>(jsonOption);
                //var modelDinhLuong = JsonConvert.DeserializeObject<List<DinhLuongRequestModel>>(jsonDinhLuong);
                //var modelPosGroup = JsonConvert.DeserializeObject<List<PosGroupRequestModel>>(jsonPosGroup);


                if (modelHeader == null || modelHeader.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng nhập thông tin phụ thu"
                    };
                }
                //if (modelBarcode != null && modelBarcode.Count > 0)
                //{
                //    var checkDuplicate = modelBarcode.GroupBy(x => new { x.Barcode, x.UOM }).Where(g => g.Count() > 1).ToList();
                //    if (checkDuplicate.Count > 0)
                //    {
                //        result = new ResultResponse
                //        {
                //            Status = HttpStatusCode.BadRequest,
                //            Message = $"Danh sách barcode ({string.Join(",", checkDuplicate.Select(a => a.Key.Barcode))}) bị trùng khi cài đặt"
                //        };
                //        return Json(result);
                //    }
                //}
                //modelHeader.FirstOrDefault().ImgName = !string.IsNullOrEmpty(modelHeader.FirstOrDefault().ImgBase64) ? "/Uploads/imgs" : "";
                var extraFeeHeader = modelHeader.FirstOrDefault();
                extraFeeHeader.FromDate = DateTime.ParseExact(extraFeeHeader.FromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                extraFeeHeader.ToDate = DateTime.ParseExact(extraFeeHeader.ToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var dateNow = DateTime.Now.Date;
                if (extraFeeHeader.ToDate.Date < extraFeeHeader.FromDate.Date)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"ĐẾN NGÀY không được nhỏ hơn TỪ NGÀY"
                    };
                    return Json(result);
                }


                extraFeeHeader.CreatedUser = base.LoginUser.UserName;
                var model = new SaveExtraFeeRequest
                {
                    ExtraFeeHeader = extraFeeHeader,
                    ExtraFeeNganhHang = modelNganhHang,
                    ExtraFeeCombo = modelCombo

                };
                if (extraFeeHeader.ArraySalesType == null || extraFeeHeader.ArraySalesType.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn hình thức bán hàng"
                    };
                    return Json(result);
                }
                var data = _setupExtraFeeBLO.SaveExtraFee(model);
                if (data.Item1)
                {
                    var itemData = data.Item3;

                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = data.Item2
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = data.Item2
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = JsonConvert.SerializeObject(ex)
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult DeleteExtraFee(string Code)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var response = _setupExtraFeeBLO.DeleteExtraFee(Code);
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
    }
}