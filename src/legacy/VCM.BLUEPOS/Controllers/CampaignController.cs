using OfficeOpenXml;
using OfficeOpenXml.Style;
using PLG.Controllers;
using Spire.Xls;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using VCM.BLUEPOS.Business.Campaign;
using VCM.BLUEPOS.Model.Campaign;


namespace VCM.BLUEPOS.Controllers
{
    public class CampaignController : BaseController
    {
        private ICampaignBLO _campaignBLO;
        private string endPoint = WebConfigurationManager.AppSettings["Campaign_EndPoint"];
        private int timeOut = int.Parse(WebConfigurationManager.AppSettings["Campaign_TimeOut"]);
        private string user = WebConfigurationManager.AppSettings["Campaign_User"];
        private string password = WebConfigurationManager.AppSettings["Campaign_Password"];
        private string nameAPICreate = WebConfigurationManager.AppSettings["Campaign_NameAPICreate"];
        private string nameAPISync = WebConfigurationManager.AppSettings["Campaign_NameAPISync"];
        private string nameAPICopy = WebConfigurationManager.AppSettings["Campaign_NameAPICopy"];
        private string nameAPIList = WebConfigurationManager.AppSettings["Campaign_NameAPIList"];
        private string nameAPIDetail = WebConfigurationManager.AppSettings["Campaign_NameAPIDetail"];

        private string nameAPICreateV2 = WebConfigurationManager.AppSettings["Campaign_NameAPICreate_V2"];
        private string passwordV2 = WebConfigurationManager.AppSettings["Campaign_Password_V2"];

        public CampaignController(ICampaignBLO campaignBLO)
        {
            _campaignBLO = campaignBLO;
        }

        [DisplayName("Danh sách CTKM")]
        public ActionResult ListCampaign()
        {
            ViewBag.ListOfferType = _campaignBLO.GetListOfferType();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetListCampaign(CampaignFilterModel filter)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPIList,
                User = user,
                Password = password,
            };
            var result = await _campaignBLO.GetListCampaign(filter, api);
            return Json(result);
        }

        // 21/08/2025,tungnt8
        [HttpPost]
        public async Task<JsonResult> GetListCampaignV2(CampaignFilterModel filter)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPIList,
                User = user,
                Password = password,
            };
            var result = await _campaignBLO.GetListCampaignV2(filter, api);
            return Json(result);
        }

        [HttpPost]
        public async Task<JsonResult> Detail(string objectId)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPIDetail,
                User = user,
                Password = password,
            };
            var result = await _campaignBLO.Detail(objectId, api);
            return Json(result);
        }

        [HttpPost]
        public async Task<JsonResult> Create(CampaignCreateModel req)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPICreate,
                User = user,
                Password = password,
            };
            req.CrtUser = base.LoginUser.UserName;
            var result = await _campaignBLO.Create(req, api);
            return Json(result);
        }

        [HttpPost]
        public async Task<ActionResult> Import(HttpPostedFileBase importFile)
        {
            if (importFile == null)
            {
                return Json(new
                {
                    Status = 0,
                    Message = "Không có file nào được chọn"
                });
            }

            string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return Json(new
                {
                    Status = 0,
                    Message = "Định dạng file excel không đúng"
                });
            }

            try
            {
                var createUserName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;

                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];

                    var data = sheet.ExportDataTable();
                    var result = await _campaignBLO.Import(data);

                    if (!string.IsNullOrEmpty(result.Item2))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"{result.Item2}"
                        });
                    }

                    return Json(new
                    {
                        Status = 1,
                        Data = result.Item1,
                        Message = $"Import danh sách thành công"
                    });

                }
                catch (Exception e)
                {
                    return Json(new
                    {
                        Status = 0,
                        Message = e.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Status = 0,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SyncCampaign(string objectId)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPISync,
                User = user,
                Password = password,
            };
            var result = await _campaignBLO.SyncCampaign(objectId, api);
            return Json(result);
        }

        // 13/08/2025,tungnt8
        [HttpPost]
        public async Task<JsonResult> SyncCampaignV2(string objectId,string date_campaign)
        {
            var _date = DateTime.ParseExact(date_campaign, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            DateTime startTime = _date.AddMinutes(20);
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPICreateV2,
                User = user,
                Password = passwordV2,
                StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
            var result = await _campaignBLO.SyncCampaignV2(objectId, api);
            return Json(result);
        }

        [HttpPost]
        public async Task<JsonResult> CopyCampaign(CampaignCopyModel req)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPICopy,
                User = user,
                Password = password,
            };
            req.CrtUser = base.LoginUser.UserName;
            var result = await _campaignBLO.CopyCampaign(req, api);
            return Json(result);
        }

        public async Task<ActionResult> ExportExcelGetListCampaign(CampaignFilterModel filter)
        {
            var api = new CampaignAPIModel
            {
                endPoint = endPoint,
                timeOut = timeOut,
                nameAPI = nameAPIList,
                User = user,
                Password = password,
            };
            var result = await _campaignBLO.GetListCampaign(filter, api);

            if (result.Meta.Code == 200)
            {
                var data = result.Data;
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.MerchantID,
                        x.ObjectIDs,
                        x.OfferNo,
                        x.OfferType,
                        x.StartTime,
                        x.EndTime,
                        x.Name,
                        x.Description,
                        x.Price,
                        x.Value,
                        x.IsAllStoreStr,
                        x.CrtUser,
                        x.CrtDate,
                        Status = x.IsSyncToGrab == "true" ? "Đã đồng bộ lên GRAP": x.EnableSyncToGrap == false ? "Chưa đồng bộ (Chờ ít phút)": "Chờ đồng bộ lên GRAP"
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Loại dịch vụ";
                    worksheet.Cells[1, 2].Value = "Mã campaign";
                    worksheet.Cells[1, 3].Value = "Mã offerNo SAP (nếu có)";
                    worksheet.Cells[1, 4].Value = "Loại CTKM";
                    worksheet.Cells[1, 5].Value = "Ngày giờ áp dụng";
                    worksheet.Cells[1, 6].Value = "Ngày giờ hết hạn";
                    worksheet.Cells[1, 7].Value = "Tên món";
                    worksheet.Cells[1, 8].Value = "Diễn giải";
                    worksheet.Cells[1, 9].Value = "Giá trị của combo";
                    worksheet.Cells[1, 10].Value = "Số tiền giảm giá của combo";
                    worksheet.Cells[1, 11].Value = "Tất cả Cửa hàng";
                    worksheet.Cells[1, 12].Value = "Người tạo";
                    worksheet.Cells[1, 13].Value = "Ngày giờ tạo";
                    worksheet.Cells[1, 14].Value = "Trạng thái";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 14])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"DanhsachCampaign{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.Close();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }


        #region middle
        [HttpPost]
        public JsonResult GetListItem(CampaignItemFilterModel filter)
        {
            var result = _campaignBLO.GetListItem(filter);
            return Json(result);
        }

        [HttpPost]
        public async Task<JsonResult> GetListStore(CampaignStoreFilterModel filter)
        {
            var data = new PagingComboxOptionModel();

            var newOptions = await _campaignBLO.GetStoreList(filter.SearchText, filter.PageIndex, filter.PageSize);
            var total = newOptions.Item2;
            var optionAll = new ComboxOptionModel { Value = "ALL", Text = "All - Tất cả cửa hàng" };
            data.SelectedOptions = await _campaignBLO.GetStoreByCodeList(filter.SeletedList);

            if (filter.IsLoadMore)
            {
                data.ComboxOptions = newOptions.Item1;
            }
            else
            {
                if (newOptions.Item1.Count > 5)
                {
                    data.ComboxOptions = data.SelectedOptions.Concat(newOptions.Item1).Distinct().ToList();
                }
                else
                {
                    data.ComboxOptions = newOptions.Item1.Concat(data.SelectedOptions).Distinct().ToList();
                }
            }

            if (filter.IsCheckAll)
            {
                data.SelectedOptions = data.ComboxOptions;
            }

            if (!filter.IsLoadMore && filter.SeletedList != null && filter.SeletedList.Any(x => x == "ALL"))
            {
                data.SelectedOptions.Add(optionAll);
            }
            data.UnSelectedOptions = filter.UnSeletedList;
            data.Total = total;
            data.PageIndex = filter.PageIndex;
            data.SearchText = filter.SearchText;
            data.Limit = filter.PageIndex * filter.PageSize;
            data.IsNextPage = (data.Limit < data.Total) ? true : false;

            return Json(data);
        }

        [HttpPost]
        public JsonResult GetListOfferNo(CampaignOfferNoFilterModel filter)
        {
            var result = _campaignBLO.GetListOfferNo(filter);
            return Json(result);
        }
        #endregion
    }
}