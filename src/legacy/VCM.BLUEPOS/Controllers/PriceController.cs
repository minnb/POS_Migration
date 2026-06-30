using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model.Price;
using VCM.BLUEPOS.Model.PriceGroup;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.Enums;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Business.Price;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class PriceController : BaseController
    {
        private IPriceBLO _priceBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        public PriceController(IPriceBLO priceBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _priceBLO = priceBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }

        [DisplayName("Danh mục bảng giá")]
        public ActionResult PriceList()
        {
            return View();
        }
        public ActionResult CreatePriceList()
        {
            return View();
        }
        public ActionResult UpdatePriceList()
        {
            return View();
        }
        public JsonResult GetComboSite(string ChanelCode)
        {
            if (ChanelCode == "10")
            {
                ChanelCode = "VMP";
            }
            else if (ChanelCode == "20")
            {
                ChanelCode = "VM";
            }
            var data = _priceBLO.GetComboSite(ChanelCode);
            return Json(data);
        }
        public JsonResult GetComboRegion()
        {
            var data = _priceBLO.GetComboRegion();
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetSearchProduct(string ItemNo)
        {
            if (string.IsNullOrEmpty(ItemNo))
            {
                var result = new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng nhập mã sản phẩm"
                };
            }

            var data = _priceBLO.GetSearchProduct(ItemNo);
            if (data.Item1 == false)
            {
                return Json(data.Item2.Status);
            }
            else
            {
                return Json(data.Item2);
            }
        }
        
        [HttpPost]
        public JsonResult GetUnitOfMeasureCode(string ItemNo)
        {
            if (string.IsNullOrEmpty(ItemNo))
            {
                var result = new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng nhập mã sản phẩm"
                };
            }
            var data = _priceBLO.GetUnitOfMeasureCode(ItemNo);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetPriceList()
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

            var barCode = Request?.Form["Barcode"];
            var itemCode = Request?.Form["ItemCode"];
            var itemName = Request?.Form["ItemName"];
            var siteCode = Request?.Form["SiteCode"];
            var isCheck = int.Parse(Request?.Form["IsCheck"]);  // Lay danh sach gia con hieu luc

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
            var data = _priceBLO.GetPriceList(itemCode, itemName, barCode, siteCode, isCheck, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<PriceListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }   
        
        public ActionResult ExportPriceList(string ItemCode, string ItemName, string BarCode, string SiteCode, int IsCheck)
        {
            if (!string.IsNullOrEmpty(ItemCode))
            {
                ItemCode = ItemCode.Trim();
            }

            if (!string.IsNullOrEmpty(ItemName))
            {
                ItemName = ItemName.Trim();
            }

            if (!string.IsNullOrEmpty(BarCode))
            {
                BarCode = BarCode.Trim();
            }

            if (!string.IsNullOrEmpty(SiteCode))
            {
                SiteCode = SiteCode.Trim();
            }

            var data = _priceBLO.ExportPriceList(ItemCode, ItemName, BarCode, SiteCode, IsCheck);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportPriceListModel
                    {
                        BarcodeNo = x.BarcodeNo,
                        SalesCode = x.SalesCode,
                        SiteNo = x.SiteNo,
                        ItemNo = x.ItemNo,
                        ItemNo_PLG = x.ItemNo_PLG,
                        ItemName = x.ItemName,
                        UnitOfMeasureCode = x.UnitOfMeasureCode,
                        UnitPrice = x.UnitPrice,
                        StartingDateStr = x.StartingDateStr,
                        EndingDateStr = x.EndingDateStr,
                        EndingYearStr = x.EndingYearStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã Barcode";
                    worksheet.Cells[1, 2].Value = "Vùng giá";
                    worksheet.Cells[1, 3].Value = "Mã Site";
                    worksheet.Cells[1, 4].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 5].Value = "Mã sản phẩm PLG";
                    worksheet.Cells[1, 6].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 7].Value = "ĐVT";
                    worksheet.Cells[1, 8].Value = "Giá bán";
                    worksheet.Cells[1, 9].Value = "Ngày có hiệu lực";
                    worksheet.Cells[1, 10].Value = "Ngày hết hiệu lực";
                    worksheet.Cells[1, 11].Value = "Năm hết hiệu lực";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 11])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"PriceList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [HttpPost]
        public ActionResult CreatePriceList(string req)
        {
            try
            {
                var UserName = base.LoginUser.UserName;
                var CreatedDate = DateTime.Now;
                var model = JsonConvert.DeserializeObject<List<CreatePriceModel>>(req);
                var data = _priceBLO.CreatePriceList(model);
                if (data == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                        Message = "Không có dữ liệu"
                    });
                }               
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Có lỗi xảy ra"
                });
            }          
        }

        [HttpPost]
        public JsonResult GetUpdatePriceList()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                string sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var pageIndex = skip / pageSize;
                var recordsTotal = 0;
                
                var check = Request?.Form["Search"];
                var chanelCode = Request?.Form["ChanelCode"];
                var regionCode = Request?.Form["RegionCode"];
                var itemNo = Request?.Form["ItemNo"];
                var salesCode = "";
                if (!string.IsNullOrEmpty(itemNo))
                {
                    itemNo = itemNo.Trim();
                }
                if (check == "1")
                {
                    salesCode = chanelCode;
                }
                else // = 2
                {
                    salesCode = regionCode;
                }
                var result = _priceBLO.GetUpdatePriceList(salesCode, itemNo, out recordsTotal, pageIndex, pageSize);
                return Json(new DataTablesViewModel<UpdatePriceListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<UpdatePriceListResponseModel>());
            }
        }

        [HttpPost]
        public ActionResult UpdatePriceList(UpdatePriceListModel model)
        {
            try
            {
                var UserName = base.LoginUser.UserName;
                var CreatedDate = DateTime.Now;
                var data = _priceBLO.UpdatePriceList(model);
                if (data == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                        Message = "Không có dữ liệu"
                    });
                }
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Có lỗi xảy ra"
                });
            }
        }
        [DisplayName("Tạo Price Group")]
        public ActionResult CreatePriceGroup()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateStorePriceGroup(string req)
        {
            try
            {
                var UserName = base.LoginUser.UserName;
                var CreatedDate = DateTime.Now;
                var model = JsonConvert.DeserializeObject<List<CreateStorePriceGroupModel>>(req);
                var data = _priceBLO.CreateStorePriceGroup(model);
                if (data == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                        Message = "Không có dữ liệu"
                    });
                }
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Có lỗi xảy ra"
                });
            }
        }

        // 16/03/2026,tungnt8
        [DisplayName("Tạo Price Group V2")]
        public ActionResult CreatePriceGroupV2()
        {
            ViewBag.ListPriceGroup = _priceBLO.GetComboxPriceGroup();
            ViewBag.StyleProfile = _priceBLO.GetComboxStyleProfile();
            return View();
        }

        [HttpPost]
        public JsonResult GetPriceGroupList()
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

            var priceGroup = Request?.Form["PriceGroup"];
            var priority = Request?.Form["Priority"];

            if (!string.IsNullOrEmpty(priceGroup))
            {
                priceGroup = priceGroup.Trim();
            }

            if (!string.IsNullOrEmpty(priority))
            {
                priority = priority.Trim();
            }

            var data = _priceBLO.GetPriceGroupList(priceGroup, priority, out recordsTotal, pageIndex, pageSize);

            return Json(new DataTablesViewModel<PriceGroupListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public JsonResult CreatePriceGroup(CreatePriceGroupModel req)
        {
            if (req.PriceGroupCode == null || req.PriceGroupCode == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập mã nhóm giá"
                });
            }

            if (req.PriceGroupName == null || req.PriceGroupName == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập tên nhóm giá"
                });
            }

            if (req.Priority == 0 || req.Priority < 0)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập độ ưu tiên"
                });
            }
            var model = new CreatePriceGroupModel
            {
                PriceGroupCode = req.PriceGroupCode,
                PriceGroupName = req.PriceGroupName,
                Priority = req.Priority,
                EnabledStr = req.EnabledStr,
                CreatedUser = base.LoginUser.UserName
            };
            var data = _priceBLO.CreatePriceGroup(model);
            return Json(data);
        }

        [HttpPost]
        [ParentAuthorize("CreatePriceGroupV2")]
        public JsonResult LoadStoreList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var recordsTotal = 0;

            var storeNo = Request?.Form["StoreNo"];
            var textsearch = Request?.Form["TextSearch"];
            var styleProfile = Request?.Form["StyleProfile"];

            if (!string.IsNullOrEmpty(textsearch))
            {
                storeNo = textsearch.Trim();
                var data = _priceBLO.GetStoreList(storeNo, "",  out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<StoreModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            else
            {
                var storeList = "";
                if (storeNo == "[]")
                {
                    storeNo = "";
                }
                if (!string.IsNullOrEmpty(storeNo) && storeNo != "-1")
                {
                    storeList = string.Join(";", JsonConvert.DeserializeObject<string[]>(storeNo));
                }         
                if (!string.IsNullOrEmpty(storeList))
                {
                    storeNo = storeList;
                }
                var data = _priceBLO.GetStoreList(storeNo, styleProfile, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<StoreModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
        }
        public JsonResult GetStoreAllList(string StoreNo, string TextSearch, string StyleProfile)
        {
            
            if (!string.IsNullOrEmpty(TextSearch))
            {
                var data = _priceBLO.GetStoreAllList("", TextSearch,"");
                return Json(data);
            }
            else
            {
                var storeList = "";
                if (StoreNo == "[]")
                {
                    StoreNo = "";
                }
                if (!string.IsNullOrEmpty(StoreNo) && StoreNo != "-1")
                {
                    storeList = string.Join(";", JsonConvert.DeserializeObject<string[]>(StoreNo));
                }
                if (!string.IsNullOrEmpty(storeList))
                {
                    StoreNo = storeList;
                }
                var data = _priceBLO.GetStoreAllList(StoreNo,"", StyleProfile);
                return Json(data);
            }
        }

        [HttpPost]
        [ParentAuthorize("CreatePriceGroupV2")]
        public JsonResult SaveStorePriceGroup(string StoreList, string PriceGroupCode, string Priority, string StoreSearchList, string TextSearch, string StyteProfile)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest,
                Message = string.Empty
            };

            try
            {
                if (string.IsNullOrEmpty(StoreList))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không có thông tin cửa hàng để cập nhật. Vui lòng kiểm tra lại"
                    };
                    return Json(result);
                }

                if (string.IsNullOrEmpty(Priority))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không có thông tin độ ưu tiên để cập nhật. Vui lòng kiểm tra lại"
                    };
                    return Json(result);
                }

                var modelStore = new AddStoreModel();
                var listSite = new List<AddStoreModel>();
                var model = new List<AddStorePriceGroupModel>();
               
                if (StoreList == "ALL")
                {
                    listSite = _priceBLO.GetStoreAllList(StoreSearchList, TextSearch, StyteProfile);
                }
                else
                {
                    var listStore = StoreList.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    if (listStore.Count > 0)
                    {
                        foreach (var site in listStore)
                        {
                            modelStore = new AddStoreModel
                            {
                                StoreNo = site,
                            };
                            listSite.Add(modelStore);
                        }
                    }
                }

                model = listSite.Select(a => new AddStorePriceGroupModel
                {
                    Store = listSite,
                    PriceGroupCode = PriceGroupCode,
                    Priority = Int32.Parse(Priority)
                }).ToList();

                var data = _priceBLO.SaveStorePriceGroup(model);

                if (data.IsStatus == 1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Cập nhật thành công tổng số {data.Item1} cửa hàng vào bảng StorePriceGroup"
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Message}"
                    };
                }
                return Json(result);
            }
            catch(Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Có lỗi xảy ra"
                });
            }            
        }



    }
}