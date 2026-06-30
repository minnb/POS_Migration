using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Business.Barcode;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using VCM.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class BarcodeController : BaseController
    {
        private IBarcodeBLO _barcodeBLO;
        private IAuthenBLO _authenBLO;

        public BarcodeController(IBarcodeBLO barcodeBLO, IAuthenBLO authenBLO)
        {
            _barcodeBLO = barcodeBLO;
            _authenBLO = authenBLO;
        }

        [DisplayName("Danh mục Barcode")]
        public ActionResult BarcodeList()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetBarcodeList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var pageIndex = skip / pageSize;
            var recordsTotal = 0;

            var barcodeNo = Request?.Form["BarcodeNo"];
            var itemNo = Request?.Form["ItemNo"];
            if (!string.IsNullOrEmpty(barcodeNo))
            {
                barcodeNo = barcodeNo.Trim();
            }

            if (!string.IsNullOrEmpty(barcodeNo))
            {
                itemNo = itemNo.Trim();
            }

            var data = _barcodeBLO.GetBarcodeList(barcodeNo, itemNo, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<BarcodeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportBarcodeList(string BarcodeNo, string ItemNo)
        {
            var data = _barcodeBLO.ExportBarcodeList(BarcodeNo, ItemNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportBarcodeResponseModel
                    {
                        BarcodeNo = x.BarcodeNo,
                        ItemNoSAP = x.ItemNoSAP,
                        ItemNo = x.ItemNo,
                        ItemName = x.ItemName,
                        UnitBarcode = x.UnitBarcode,
                        //VariantCode = x.VariantCode,
                        DiscountPercent = x.DiscountPercent,
                        LastDateModified = x.LastDateModified
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Barcode";
                    worksheet.Cells[1, 2].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 3].Value = "Mã sản phẩm PLG";
                    worksheet.Cells[1, 4].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 5].Value = "ĐVT Barcode";
                    //worksheet.Cells[1, 6].Value = "VariantCode";
                    worksheet.Cells[1, 6].Value = "Giảm giá (%)";
                    worksheet.Cells[1, 7].Value = "Ngày cập nhật";
                   
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 7])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"BarcodeList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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







    }
}