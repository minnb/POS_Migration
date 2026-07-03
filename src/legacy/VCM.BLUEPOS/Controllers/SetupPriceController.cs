using Gma.QrCodeNet.Encoding;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PLG.Controllers;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using TichHopSAP;
using VCM.BLUEPOS.Business.SetupPrice;
using VCM.BLUEPOS.Business.SetupPromotion;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Model.Price;
using VCM.BLUEPOS.Model.SetupPrice;
using VCM.BLUEPOS.Models;


namespace PLG.Controllers
{
    public class SetupPriceController : BaseController
    {
        private ISetupPriceBLO _setupPriceBLO { get; set; }
        private ISetupPromotionBLO _setupBBBLO { get; set; }
        public SetupPriceController(ISetupPriceBLO setupPriceBLO, ISetupPromotionBLO setupBBBLO)
        {
            _setupPriceBLO = setupPriceBLO;
            _setupBBBLO = setupBBBLO;
        }

        [DisplayName("Cài đặt giá")]
        public ActionResult SetupPrice()
        {
            var viewSetupPrice = _setupPriceBLO.ViewSetupPrice();
            return View(viewSetupPrice);
        }

        [HttpPost]
        public JsonResult GetComboSiteByChannel(string ChannelCode)
        {
            var data = _setupPriceBLO.ListStoreByChannel(ChannelCode);
            var result = data.Select(a => new SetupComboboxModel
            {
                PriceGroupCode = a.No,
                DescGroupCode = a.Name,
                Type = 4

            }).ToList();
            return Json(result);
        }

        [HttpPost]
        public JsonResult SearchItem(string keyword, int page = 1)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                int pageSize = 20;
                var data = _setupPriceBLO.SearchItem(keyword, pageSize, page,"item");

                if (data != null)
                {
                    return Json(new
                    {
                        Status = HttpStatusCode.OK,
                        items = data.Items,
                        total = data.TotalRecord,
                        pageSize = pageSize
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tồn tại dữ liệu"
                    };
                }
            }
            catch (Exception ex)
            {

                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = ex.Message
                };
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult _AppendListItem(string typeSetup, string salesType,string SalesTypeName, string saleGroupCode, string saleGroupName, string regionCode, string store,string QtyRow)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK//OK                
            };
            /*
            var ListItem = _setupPriceBLO.ListItemBarcode();

            var html = "<option value=\"\">Chọn sản phẩm</option>";
            
            foreach (var item in ListItem)
            {
                var ht = $"{item.No} : {item.Description}";
                html += $"<option value=\"{item.No}\" data-barcode=\"{item.Barcode}\" data-uom=\"{item.BaseUnitOfMeasure}\">{ht}</option>";

                
            }
            */
            //string html = "";
            var model = new ViewSetupItemPriceModel
            {
                SalesType = salesType,
                SalesTypeName = SalesTypeName,
                TypeSetup = typeSetup,
                //ChannelCode = channelCode,
                saleGroupCode = saleGroupCode,
                saleGroupName = saleGroupName,
                RegionCode = regionCode,
                Store = store,
                QtyRow = QtyRow,
                IsImport = false
                // ListItem = _setupPriceBLO.ListItemBarcode()
            };

            try
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPrice/_AppendListItem.cshtml", model),
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
            var jsonResult = Json(result, JsonRequestBehavior.AllowGet);

            // Tăng giới hạn lên tối đa
            jsonResult.MaxJsonLength = int.MaxValue;

            return jsonResult;
            //return Json(result);

        }

        [HttpPost]
        public JsonResult GetInfoBarcode(string barcode)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };

            int pageSize = 20;
            int page = 1;
           
            try
            {
                var data = _setupPriceBLO.SearchItem(barcode, pageSize, page, "barcode");
                if (data != null)
                {
                    return Json(new
                    {
                        Status = 200,
                        items = data.Items,
                        total = data.TotalRecord,
                        pageSize = pageSize
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tồn tại thông tin barcode {barcode}"
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = ex.Message
                };
            }

            return Json(result);
        }
        public JsonResult GetInfoBarcode_BK(string barcode)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };

            var data = new BarcodeResponseModel();
            try
            {
                data = _setupPriceBLO.GetInfoBarcode(barcode);
                if (data != null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Data = data
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tồn tại thông tin barcode {barcode}"
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = ex.Message
                };
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult SaveItemPrice(string jsonRequest)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            var modelRequest = JsonConvert.DeserializeObject<List<SaveItemPriceRequest>>(jsonRequest);

            var checkItemNo = modelRequest.Where(d => string.IsNullOrWhiteSpace(d.ItemNo)).ToList();
            if (checkItemNo.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Có {checkItemNo.Count} dòng chưa chọn sản phẩm"
                };
                return Json(result);
            }
            var checkUom = modelRequest.Where(d => string.IsNullOrWhiteSpace(d.UOM)).ToList();
            if (checkUom.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkUom.Select(a => a.ItemNo))}) chưa chọn Đơn vị tính"
                };
                return Json(result);
            }

            //01/12/2021
            var checkStartDate = modelRequest.Where(d => d.StartDate.Length < 10).ToList();
            if (checkStartDate.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkStartDate.Select(a => a.ItemNo))}) định dạng TỪ NGÀY không đúng"
                };
                return Json(result);
            }
            var checkEndDate = modelRequest.Where(d => d.EndDate.Length < 10).ToList();
            if (checkEndDate.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkEndDate.Select(a => a.ItemNo))}) định dạng ĐẾN NGÀY không đúng"
                };
                return Json(result);
            }
           
            var checkStartMonth = modelRequest.Where(d => int.Parse(d.StartDate.Substring(3, 2)) > 12).ToList();
            if (checkStartMonth.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkStartMonth.Select(a => a.ItemNo))}) định dạng THÁNG TỪ NGÀY không đúng"
                };
                return Json(result);
            }

            var checkEndtMonth = modelRequest.Where(d => int.Parse(d.EndDate.Substring(3, 2)) > 12).ToList();
            if (checkEndtMonth.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkEndtMonth.Select(a => a.ItemNo))}) định dạng THÁNG ĐẾN NGÀY không đúng"
                };
                return Json(result);
            }

            var model = modelRequest.Select(a => new SaveItemPriceModel
            {
                ID = int.Parse(a.ID),
                SalesType = a.SalesType,
                SalesCode = a.SalesCode,
                ChannelCode = a.ChannelCode,
                RegionCode = a.RegionCode,
                StoreNo = a.StoreNo,
                ItemNo = a.ItemNo,
                Barcode = a.Barcode,
                UOM = a.UOM,
                UnitPrice = string.IsNullOrEmpty(a.UnitPrice) ? 0 : double.Parse(a.UnitPrice),
                StartDate = DateTime.ParseExact(a.StartDate, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                EndDate = DateTime.ParseExact(a.EndDate, "dd/MM/yyyy", CultureInfo.InvariantCulture)

            }).ToList();


            var checkDuplicate = model.GroupBy(x => new { x.ItemNo, x.UOM }).Where(g => g.Count() > 1).ToList();
            if (checkDuplicate.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkDuplicate.Select(a => a.Key))}) bị trùng khi cài đặt"
                };
                return Json(result);
            }

            var checkPrice = model.Where(d => d.UnitPrice <= 0).ToList();
            if (checkPrice.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkPrice.Select(a => a.ItemNo))}) chưa cài đặt giá"
                };
                return Json(result);
            }

            var checkDate = model.Where(d => d.StartDate.Value.Date > d.EndDate.Value.Date).ToList();
            if (checkDate.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkDate.Select(a => a.ItemNo))}) cài đặt TỪ NGÀY KHÔNG NHỎ HƠN ĐẾN NGÀY"
                };
                return Json(result);
            }
            var dateNow = DateTime.Now;
            var checkDateStartNow = model.Where(d => d.StartDate.Value.Date < dateNow.Date).ToList();
            if (checkDateStartNow.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkDateStartNow.Select(a => a.ItemNo))}) cài đặt TỪ NGÀY không được nhỏ hơn ngày hiện tại"
                };
                return Json(result);
            }

            var checkDateEndNow = model.Where(d => d.EndDate.Value.Date < dateNow.Date).ToList();
            if (checkDateEndNow.Count > 0)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Danh sách sản phẩm ({string.Join(",", checkDateEndNow.Select(a => a.ItemNo))}) cài đặt ĐẾN NGÀY không được nhỏ hơn ngày hiện tại"
                };
                return Json(result);
            }


            //      CASE WHEN ISNULL(A.[Site], '') <> '' THEN A.[Site]
            //      WHEN ISNULL(A.[Site],'') = '' AND ISNULL(A.Region,'') <> '' THEN A.Region
            //      WHEN ISNULL(A.[Site],'') = '' AND ISNULL(A.Region,'') = '' AND ISNULL(A.Channel,'') <> '' THEN A.Channel
            //      WHEN ISNULL(A.[Site],'') = '' AND ISNULL(A.Region,'') = '' AND ISNULL(A.Channel,'') = '' THEN 'ALL'
            //      END[SalesCode]

            //SalesType+'-'+ItemNo +'-'+ UOMCode +'-' + SalesCode + '-' + StartingDate  AS Pkey

            var listModel = new List<SalesPriceModel>();
            foreach (var item in model)
            {
                /*
                var salesCode = "ALL";
                if (item.StoreNo != "ALL")
                {
                    salesCode = item.StoreNo;
                }
                if (item.StoreNo == "ALL" && item.RegionCode != "ALL")
                {
                    salesCode = item.RegionCode;
                }
                if (item.StoreNo == "ALL" && item.RegionCode == "ALL" && item.ChannelCode != "ALL")
                {
                    salesCode = item.ChannelCode;
                }
                */
                var salesCode = item.SalesCode;
                var pkey = $"{item.SalesType}-{item.ItemNo}-{item.UOM}-{salesCode}";
                var itemList = new SalesPriceModel
                {
                    SalesCode = salesCode,
                    ItemNo = item.ItemNo,
                    CurrencyCode = "VND",
                    UnitOfMeasureCode = item.UOM,
                    UnitPrice = (double)item.UnitPrice,
                    PriceIncludesVAT = 1,
                    AllowInvoiceDisc = 1,
                    SalesType = item.SalesType,
                    MinimumQuantity = 1,
                    VariantCode = "",
                    AllowLineDisc = 1,
                    StartingDate = (DateTime)item.StartDate,
                    EndingDate = (DateTime)item.EndDate,
                    Pkey = pkey
                };
                listModel.Add(itemList);
            }

            try
            {
                var modelCall = listModel.Select(a => new ParamCallStoreProcedureRequest
                {
                    Pkey = a.Pkey,
                    FromDate = a.StartingDate.Date.ToString("yyyy-MM-dd"),
                    ToDate = a.EndingDate.Date.ToString("yyyy-MM-dd"),
                    UnitPrice = (float)a.UnitPrice

                }).ToList();

                //var jsonPrice = JsonConvert.SerializeObject(modelCall);
                var exec = _setupPriceBLO.SaveSalesPrice(listModel);
                if (exec.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Cập nhật thành công bảng giá"
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{exec.Item2}"
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = ex.Message
                };
            }

            return Json(result);
        }

        public ActionResult ExcelExamplePrice()
        {

            var data = _setupPriceBLO.ExcelExamplePrice();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var countR = data.Count;

                    //add header
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 5])
                    {
                        r.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        r.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        // r.AutoFilter = true;
                    }
                    //add value
                    using (ExcelRange r = worksheet.Cells[1, 1, 1 + countR, 5])
                    {
                        r.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                        r.Style.Border.Top.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Border.Left.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Border.Right.Color.SetColor(System.Drawing.Color.Black);
                        // r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = false;

                       
                    }
                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "dd/MM/yyyy";
                    }
                    worksheet.Cells["A1"].LoadFromCollection(data, true);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"FileMauBangGia_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult LoadDataExcelToTable(HttpPostedFileBase importFile, string typeSetup, string salesType, string SalesTypeName, string saleGroupCode, string saleGroupName, string regionCode, string store)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest,
                Message = ""
            };
            try
            {
                if (importFile == null) return Json(new { Status = 0, Message = "No File Selected" });
                string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
                if (fileExtension != ".xls" && fileExtension != ".xlsx")
                {
                    return Json(new ResultResponse
                    {
                        Status = HttpStatusCode.NotFound,
                        Message = "Không đúng định dạng file excel",
                        Data = null
                    });
                }
                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(importFile.InputStream);
                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();

                    var listPrice = ConvertData.ConvertDataTable<LoadExcelExamplePriceModel>(data);
                    var checkStartingDate = listPrice.Any(a => string.IsNullOrEmpty(a.StartingDate));
                    if (checkStartingDate)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.NotFound,
                            Message = $"Vui lòng kiểm tra cột StartingDate, có giá trị trống",
                            Data = null
                        };
                        return Json(result);
                    }
                    var checkEndingDate = listPrice.Any(a => string.IsNullOrEmpty(a.EndingDate));
                    if (checkEndingDate)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.NotFound,
                            Message = $"Vui lòng kiểm tra cột EndingDate, có giá trị trống",
                            Data = null
                        };
                        return Json(result);
                    }
                    var checkMaHang = listPrice.Any(a => string.IsNullOrEmpty(a.UnitPrice) || (!string.IsNullOrEmpty(a.UnitPrice) && double.Parse(a.UnitPrice) < 0));
                    if (checkMaHang)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.NotFound,
                            Message = $"Vui lòng kiểm tra cột UnitPrice, có giá trị trống hoặc bằng 0",
                            Data = null
                        };
                        return Json(result);
                    }
                    var checkItem = listPrice.Any(a => string.IsNullOrEmpty(a.ItemNo) && string.IsNullOrEmpty(a.Barcode));
                    if (checkItem)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.NotFound,
                            Message = $"Vui lòng kiểm tra cột ItemNo/Barcode, có giá trị trống",
                            Data = null
                        };
                        return Json(result);
                    }

                     checkItem = listPrice.Any(a => !string.IsNullOrEmpty(a.ItemNo) && string.IsNullOrEmpty(a.Uom));
                    if (checkItem)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.NotFound,
                            Message = $"Vui lòng kiểm tra cột Uom, có giá trị trống",
                            Data = null
                        };
                        return Json(result);
                    }

                     checkItem = listPrice.Any(a => string.IsNullOrEmpty(a.ItemNo) && !string.IsNullOrEmpty(a.Uom));
                    if (checkItem)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.NotFound,
                            Message = $"Vui lòng kiểm tra cột ItemNo, có giá trị trống",
                            Data = null
                        };
                        return Json(result);
                    }

                    var convertItem = _setupPriceBLO.GetListInfoItem(listPrice);
                    var errorList = convertItem.Where(e => e.ErrorMessage != "");

                    string QtyRow = convertItem.Count.ToString();
                    /*
                    var model = new ViewSetupItemPriceModel
                    {
                        SalesType = salesType,
                        TypeSetup = typeSetup,
                        ChannelCode = channelCode,
                        RegionCode = regionCode,
                        Store = store,
                        ListItem = _setupPriceBLO.ListItemBarcode()//,
                       // ListItemFromExcel = convertItem
                    };
                    */
                    var model = new ViewSetupItemPriceModel
                    {
                        SalesType = salesType,
                        SalesTypeName = SalesTypeName,
                        TypeSetup = typeSetup,
                        saleGroupCode = saleGroupCode,
                        saleGroupName = saleGroupName,
                        RegionCode = regionCode,
                        Store = store,
                        QtyRow = QtyRow,
                        IsImport = false,
                        ListItemResultFromExcel = convertItem
                    };

                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        //ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPrice/LoadDataExcelToTable.cshtml", model),
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupPrice/LoadDataExcelToTable.cshtml", model),
                        Data = model
                    };

                    //result.Status = HttpStatusCode.OK;
                    //result.Data = listPrice;
                    //result.Message = $"Import thành công  dòng, với số lượng";

                }
                catch (Exception ex)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.InternalServerError,
                        Message = $"Vui lòng xóa hết tất cả các dòng trống trong file excel. {ex.Message}",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = ex.Message,
                    Data = null
                };
            }
            return Json(result);
        }


        [HttpGet]
        public ActionResult _ListPrice()
        {
            return PartialView();
        }

        [HttpPost]
        public JsonResult _ListPriceLoad()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;
            var recordsTotal = 0;

            var barCode = Request?.Form["Barcode"];
            var itemCode = Request?.Form["ItemNo"];
            var itemName = Request?.Form["ItemName"];
            var siteCode = Request?.Form["SiteCode"];
            var status = Request?.Form["Status"];

            if (!string.IsNullOrEmpty(barCode))
            {
                barCode = barCode.Trim();
            }

            if (!string.IsNullOrEmpty(itemCode))
            {
                itemCode = itemCode.Trim();
            }

            if (!string.IsNullOrEmpty(itemName))
            {
                itemName = itemName.Trim();
            }

            if (!string.IsNullOrEmpty(siteCode))
            {
                siteCode = siteCode.Trim();
            }
            var data = _setupPriceBLO.LoadSalesPrice(itemCode, itemName, barCode, siteCode, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ListPriceModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult DeletePrice(int Id)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var response = _setupPriceBLO.DeletePrice(Id);
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
        public ActionResult UpdatePrice(int Id, float UnitPrice)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            if (UnitPrice <= 0)
            {
                result.Status = HttpStatusCode.BadRequest;
                result.Message = "Giá bán phải lớn hơn 0";
                return Json(result);
            }
            try
            {
                var response = _setupPriceBLO.UpdatePrice(Id, UnitPrice);
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