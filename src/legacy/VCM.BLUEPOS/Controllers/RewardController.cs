using Newtonsoft.Json;
using PLG.Controllers;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Business.Reward;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.Reward;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Models;
//using VMC.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class RewardController : BaseController
    {
        private IRewardBLO _rewardBLO { get; set; }
        public RewardController(IRewardBLO rewardBLO)
        {
            _rewardBLO = rewardBLO;

        }

        [DisplayName("Phát hành mã dự thưởng")]
        public ActionResult RewardIssue()
        {
            return View();
        }
        [HttpPost]
        public ActionResult RewardIssueLoad()
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

                var code = Request?.Form["RewardNo"];
                var title = Request?.Form["Title"];
                var offerNo = Request?.Form["OfferNo"];
                var status = Request?.Form["Enable"];

                var recordsTotal = 0;
                var result = _rewardBLO.LoadReward(code, title, offerNo, status, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<RewardHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<RewardHeaderModel>());
            }
        }

        [HttpGet]
        public ActionResult _RewardIssue(string code)
        {
            var model = new ViewRewardModel();

            try
            {
                model = _rewardBLO.InfoReward(code);
            }
            catch (Exception ex)
            {

            }

            return PartialView(model);
        }

        [HttpPost]
        public ActionResult RewardCodeLoad()
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

                var rewardNo = Request?.Form["RewardNo"];

                var recordsTotal = 0;
                var result = _rewardBLO.RewardCodeLoad(rewardNo, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<RewardCodeModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return Json(new List<RewardCodeModel>());
            }
        }

        [HttpPost]
        public JsonResult SaveReward(RewardHeaderRequest model, HttpPostedFileBase ImportFile)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                model.FromDate = string.IsNullOrEmpty(model.FromDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.FromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.ToDate = string.IsNullOrEmpty(model.ToDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.ToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var dateNow = DateTime.Now;
               
                var checkDate = model.FromDate.Value.Date > model.ToDate.Value.Date;
                if (checkDate)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"TỪ NGÀY không lớn hơn ĐẾN NGÀY"
                    };
                    return Json(result);
                }
                if (model.ToDate.Value.Date < model.FromDate.Value.Date)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"ĐẾN NGÀY không được nhỏ hơn TỪ NGÀY"
                    };
                    return Json(result);
                }


                if (string.IsNullOrEmpty(model.RewardNo))
                {
                    if (model.IsReward == true)
                    {
                        if (ImportFile == null) return Json(new { Status = 0, Message = "No File Selected" });
                        string fileExtension = Path.GetExtension(ImportFile.FileName).ToLower();
                        if (fileExtension != ".xls" && fileExtension != ".xlsx")
                        {
                            return Json(new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = "Không đúng định dạng file excel",
                                Data = null
                            });
                        }

                        Workbook workbook = new Workbook();
                        workbook.LoadFromStream(ImportFile.InputStream);
                        Worksheet sheet = workbook.Worksheets[0];
                        var data = sheet.ExportDataTable();

                        var listCode = ConvertData.ConvertDataTable<RewardCodeExcel>(data);
                        if (listCode.Count == 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = $"Vui lòng kiểm tra file Excel, không có mã dự thưởng",
                                Data = null
                            };
                            return Json(result);
                        }
                        var checkCode = listCode.Any(a => string.IsNullOrEmpty(a.MaDuThuong));
                        if (checkCode)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = $"Vui lòng kiểm tra cột MaDuThuong, có giá trị trống",
                                Data = null
                            };
                            return Json(result);
                        }

                        var checkCodeDuplicate = listCode.GroupBy(x => x.MaDuThuong).Where(g => g.Count() > 1).ToList();
                        if (checkCodeDuplicate.Count > 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"File excel có giá trị trùng ({string.Join(",", checkCodeDuplicate)}), Vui lòng kiểm tra lại",
                                Data = null
                            };
                            return Json(result);
                        }
                        model.listCode = listCode;
                    }
                }

                var save = _rewardBLO.SaveReward(model);
                if (save.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = save.Item2,
                        Data = save.Item3
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = save.Item2,
                        Data = save.Item3
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


        public ActionResult _ViewOfferByType(string offerType)
        {
            var model = new ViewOfferByTypeRequest
            {
                OfferType = offerType
            };
            return PartialView(model);
        }
        [HttpPost]
        public ActionResult LoadOfferReward()
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

                var offerType = Request?.Form["OfferType"];
                var offerNo = Request?.Form["No"];
                var offerName = Request?.Form["Description"];
                var recordsTotal = 0;
                var result = _rewardBLO.LoadOfferReward(offerType, offerNo, offerName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<OfferHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<VCM.BLUEPOS.Model.SetupItem.ItemModel>());
            }
        }

        public ActionResult ExcelExampleReward()
        {
            var sDocument = Server.MapPath("~/Files/") + "ImportMaDuThuong.xlsx";
            byte[] fileBytes = System.IO.File.ReadAllBytes(sDocument);
            string fileName = "ImportMaDuThuong.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


    }
}