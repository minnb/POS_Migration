using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.Mvc;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.MonitorPOS;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Product;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Business.Product;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Business.MonitorPOS;


namespace PLG.Controllers
{
    public class ProductController : BaseController
    {
        private IProductBLO _productBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private IMonitorPOSBLO _monitorPOSBLO;
        private string linkAPI { get; set; }

        private string POSUser = @"" + ConfigurationManager.AppSettings["POSUser"];
        private string POSPass = @"" + ConfigurationManager.AppSettings["POSPass"];

        private string agentUser = @"" + ConfigurationManager.AppSettings["AgentUser"];
        private string agentPass = @"" + ConfigurationManager.AppSettings["AgentPass"];

        string GrabFood_Active_Item_Async = WebConfigurationManager.AppSettings["GrabFood_Active_Item_Async"];
        string GrabFood_Item_Menu_Block = WebConfigurationManager.AppSettings["GrabFood_Item_Menu_Block"];  // khóa món GrabFood : 16/06/2025
        string GrabFood_Item_Block = WebConfigurationManager.AppSettings["GrabFood_Item_Block"];  // khóa món GrabFood : 23/06/2025

        string GrabFood_Authorization = WebConfigurationManager.AppSettings["GrabFood_Authorization"];
        string GrabFood_Basic_Pass = WebConfigurationManager.AppSettings["GrabFood_Basic_Pass"];
        public ProductController(IProductBLO productBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO, IMonitorPOSBLO monitorPOSBLO)
        {
            _productBLO = productBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            _monitorPOSBLO = monitorPOSBLO;
        }

        [HttpPost]
        public bool PingIP(string IPAddress)
        {
            try
            {
                Ping ping = new Ping();
                if (!string.IsNullOrEmpty(IPAddress))
                {
                    PingReply pingReply = ping.Send(IPAddress,1000);
                    if (pingReply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                }                
            }
            catch (Exception ex)
            { 
            }
            return false;
        }

        public ActionResult LoadStoreByUserName(string userName)
        {
            var listStoreNo = new List<ArraySiteNoModel>();
            try
            {
                listStoreNo = _commonBLO.LoadComboxStoreByUserName(userName).ToList();
                if (listStoreNo.Count > 1)
                {
                    listStoreNo.Add(new ArraySiteNoModel { SiteNo = "", SiteName = "Tất cả" });
                }
            }
            catch (Exception ex){}
            return Json(listStoreNo.OrderBy(d => d.SiteNo));
        }

        [DisplayName("Danh mục Barcode")]
        public ActionResult ProductList()
        {
            return View();
        }
        public ActionResult CreateArticle()
        {
            ViewBag.ListArticleType = _commonBLO.GetComboxArticleType();
            ViewBag.ListUnitOfMeasure = _commonBLO.GetComboxUnitOfMeasure();
            return View();
        }
        public ActionResult UpdateArticle()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetProductList()
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

            var itemName = Request?.Form["ItemName"];
            if (!string.IsNullOrEmpty(itemName))
            {
                itemName = itemName.Trim();
            }
            var itemNo = Request?.Form["ItemNo"];
            if (!string.IsNullOrEmpty(itemNo))
            {
                itemNo = itemNo.Trim();
            }
            var barcodeNo = Request?.Form["BarcodeNo"];
            if (!string.IsNullOrEmpty(barcodeNo))
            {
                barcodeNo = barcodeNo.Trim();
            }
            var taxCode = Request?.Form["TaxCode"];
            if (!string.IsNullOrEmpty(taxCode))
            {
                taxCode = taxCode.Trim();
            }
            var data = _productBLO.GetProductList(itemNo, itemName, barcodeNo, taxCode, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<ProductListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }      
        public ActionResult ExportProductList( string ItemNo, string ItemName, string BarcodeNo, string TaxCode)
        {
            var data = _productBLO.ExportProductList( ItemNo, ItemName, BarcodeNo, TaxCode);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportProductListModel
                    {
                        ItemNo = x.ItemNo,
                        ItemNo_PLG = x.ItemNo_PLG,
                        ItemName1 = x.ItemName1,
                        ItemName2 = x.ItemName2,
                        SalesUnit = x.SalesUnit,
                        BarcodeNo = x.BarcodeNo,
                        BarcodeUnit = x.BarcodeUnit,
                        TaxGroupCode = x.TaxGroupCode,
                        ParentCode = x.ParentCode,
                        Size = x.Size,
                        //CommonItemNo = x.CommonItemNo,
                        //ItemFamilyCode = x.ItemFamilyCode,
                        LastDateModifiedStr = x.LastDateModifiedStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 2].Value = "Mã sản phẩm PLG";
                    worksheet.Cells[1, 3].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 4].Value = "Tên sản phẩm (tiếng việt)";
                    worksheet.Cells[1, 5].Value = "Đơn vị tính";
                    worksheet.Cells[1, 6].Value = "Mã Barcode";
                    worksheet.Cells[1, 7].Value = "ĐVT Barcode";
                    worksheet.Cells[1, 8].Value = "Thuế suất";
                    worksheet.Cells[1, 9].Value = "Mã cha";
                    worksheet.Cells[1, 10].Value = "Size";
                    //worksheet.Cells[1, 11].Value = "CommonItemNo";
                    //worksheet.Cells[1, 12].Value = "ItemFamilyCode";
                    worksheet.Cells[1, 11].Value = "Ngày cập nhật";
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 11])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }
                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"ProductList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public ActionResult SaveArticleList(CreateProductModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            req.CreatedDate = DateTime.Now;
            var barCodeModel = JsonConvert.DeserializeObject<List<CreateBarcodeListModel>>(req.CreateBarcodeStr);
            var model = new CreateProductModel
            {
                ItemName = req.ItemName,
                ItemNameFull = req.ItemNameFull,
                BaseUnitOfMeasure = req.BaseUnitOfMeasure,
                SalesUnitOfMeasure = req.SalesUnitOfMeasure,
                TaxGroupCode = req.TaxGroupCode,
                ItemFamilyCode = req.ItemFamilyCode,
                Blocked = req.Blocked,
                BlockedVINID = req.BlockedVINID,
                CreateBarcodeList = barCodeModel
            };
            var data = _productBLO.SaveArticleList(model);
            return Json(data);
        }

        [HttpPost]
        public ActionResult SaveBarcodeList(string req)
        {
            try
            {
                var CreatedUser = base.LoginUser.UserName;
                var CreatedDate = DateTime.Now;
                var model = JsonConvert.DeserializeObject<List<CreateBarcodeListModel>>(req);
                var data = _productBLO.SaveBarcodeList(model);
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
       
        [HttpPost]
        public JsonResult GetUpdateProductList()
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

            var itemNo = Request?.Form["ItemNo"];
            if (!string.IsNullOrEmpty(itemNo))
            {
                itemNo = itemNo.Trim();
            }
            var itemName = Request?.Form["ItemName"];
            if (!string.IsNullOrEmpty(itemName))
            {
                itemName = itemName.Trim();
            }

            var barcodeNo = Request?.Form["BarcodeNo"];
            if (!string.IsNullOrEmpty(barcodeNo))
            {
                barcodeNo = barcodeNo.Trim();
            }

            var taxCode = Request?.Form["TaxCode"];
            if (!string.IsNullOrEmpty(taxCode))
            {
                taxCode = taxCode.Trim();
            }
            var data = _productBLO.GetUpdateProductList(itemNo, itemName, barcodeNo, taxCode, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<UpdateProductListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult UpdateProductList(UpdateProductListModel req)
        {
                var UserName = base.LoginUser.UserName;
                var UpdatedDate = DateTime.Now;
                var model = new UpdateProductListModel
                {
                    ItemNo = req.ItemNo,
                    ItemName = string.IsNullOrEmpty(req.ItemName) ? string.Empty : req.ItemName,
                    ItemNameFull = string.IsNullOrEmpty(req.ItemNameFull) ? string.Empty : req.ItemNameFull,
                    SalesUnit = req.SalesUnit,
                    Blocked = req.Blocked,
                    BarcodeNo = req.BarcodeNo,
                    BarcodeUnit = req.BarcodeUnit,
                    BlockedBarcode = req.BlockedBarcode,
                    TaxGroupCode = string.IsNullOrEmpty(req.TaxGroupCode) ? string.Empty : req.TaxGroupCode,
                    ItemCategoryCode = string.IsNullOrEmpty(req.ItemCategoryCode) ? string.Empty : req.ItemCategoryCode,
                    Brand = string.IsNullOrEmpty(req.Brand) ? string.Empty : req.Brand,
                    Model = string.IsNullOrEmpty(req.Model) ? string.Empty : req.Model,
                    ItemFamilyCode = req.ItemFamilyCode,
                    BlockedVINID = req.BlockedVINID,
                    LastDateModifiedStr = UpdatedDate
                };

                var data = _productBLO.UpdateProductList(model);
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

        [DisplayName("Khóa món")]
        public ActionResult ProductLock()
        {
            var siteNo = base.LoginUser.SiteCode;
            var setStoreUser = _commonBLO.GetInforStoreSet(siteNo);
            var listSetDB = new List<SQLServerModel>();
            if (setStoreUser != null && setStoreUser.Count > 0)
            {
                var first = setStoreUser.FirstOrDefault();
                listSetDB.Add(new SQLServerModel { IPServer = first.IPServer, Description = first.Description });
            }
            else
            {
                listSetDB = _commonBLO.ListSQLServer();
            }

            ViewBag.SetDB = listSetDB; 
            ViewBag.ListRole = _authenBLO.GetRoleByUser(LoginUser.UserName);
            ViewBag.UserLogin = base.LoginUser.UserName;
            return View();
        }
        public JsonResult GetStoreProductLock()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var recordsTotal = 0;

            var serverType = Request?.Form["SourceType"];
            if (!string.IsNullOrEmpty(serverType))
            {
                serverType = serverType.Trim();
            }

            var setServer = Request?.Form["SetServer"];
            if (!string.IsNullOrEmpty(setServer))
            {
                setServer = setServer.Trim();
            }

            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var IPAddress = Request?.Form["IpAddress"];
            if (!string.IsNullOrEmpty(IPAddress))
            {
                IPAddress = IPAddress.Trim();
            }

            var userName = base.LoginUser.UserName;
            var data = new List<StoreProductLockResponseModel>();
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];

            if (serverType == "SERVER")
            {
                data = _productBLO.GetStoreProductLock(setServer, storeNo, userName, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            }
            else if (serverType == "POS")
            {
                if(PingIP(IPAddress) == false)
                {
                    data = new List<StoreProductLockResponseModel>();
                }
                else
                {
                    data = _productBLO.POS_GetStoreProductLock(IPAddress, POSUser, POSPass, storeNo, userName, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
                }
            }

            return Json(new DataTablesViewModel<StoreProductLockResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public JsonResult GetItemListForBlock()
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
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }
            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }
            var IPAddress = Request?.Form["IpAddress"];
            if (!string.IsNullOrEmpty(IPAddress))
            {
                IPAddress = IPAddress.Trim();
            }
            var userName = base.LoginUser.UserName;
            var serverType = Request?.Form["SourceType"];
            if (!string.IsNullOrEmpty(serverType))
            {
                serverType = serverType.Trim();
            }

            var data = new List<ItemListResponseModel>();

            if (serverType == "SERVER")
            {
                data = _productBLO.GetItemListForBlock(storeNo, status, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            }
            else if (serverType == "POS")
            {
                if (PingIP(IPAddress) == false)
                {
                    data = new List<ItemListResponseModel>();
                }
                else
                {
                    data = _productBLO.POS_GetItemListForBlock(IPAddress, POSUser, POSPass, storeNo, status, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
                }            
            }

            return Json(new DataTablesViewModel<ItemListResponseModel> {
                draw = draw.ToString(),
                recordsFiltered = recordsTotal,
                recordsTotal = recordsTotal,
                data = data
            });
        }

        // API: khóa món sản phẩm bán trên kênh Grabfood

        [HttpPost]
        public ActionResult SetupLockItemByGrabFoodAPI(List_Active_Item_Async req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            if (string.IsNullOrEmpty(req.StoreNo))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Vui lòng chọn cửa hàng",
                    Data = null
                };
                return Json(result);
            }

            try
            {
                var model = new GrabFood_Active_Item_Async();
                var listItem = new List<GrabFood_Active_Item_Async>();
                var listItemGrabfood = JsonConvert.DeserializeObject<List<GrabFoodActiveItemAsyncModel>>(req.ItemListStr);
                foreach (var a in listItemGrabfood)
                {
                    if (string.IsNullOrEmpty(a.ItemNo))
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Không thấy mã sản phẩm. Vui lòng kiểm tra lại",
                            Data = null
                        };
                        return Json(result);
                    }

                    model = new GrabFood_Active_Item_Async
                    {
                        ItemId = a.ItemNo,
                        Store = req.StoreNo,
                        AvailableStatus = "UNAVAILABLE",
                        User = base.LoginUser.UserName
                    };
                    listItem.Add(model);
                }

                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

                if (string.IsNullOrEmpty(linkAPI))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(result);
                }

                var endPoint = $"{GrabFood_Active_Item_Async}";

                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(listItem);
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add("Authorization", "Basic UE9TOjk4NzY1NDMyMTA=");
                        var responseOption = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;

                        if (responseOption.StatusCode == HttpStatusCode.OK)  // Khóa món thành công
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.OK,
                                Message = "Khóa món thành công",
                                Data = ""
                            };
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = responseOption.StatusCode,
                                Message = responseOption.ReasonPhrase,
                                Data = ""
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Data = JsonConvert.SerializeObject(ex);
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = JsonConvert.SerializeObject(ex)
                };
                return Json(result);
            }
        }

        // Khóa món Grab : 16/06/2025,tungnt8: New   
        public async Task<ActionResult> SetupLockItemGrabFoodAPIV2(List_Active_Item_Async req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(result);
            }

            if (string.IsNullOrEmpty(req.StoreNo))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Vui lòng chọn cửa hàng",
                    Data = null
                };
                return Json(result);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var path = $"{GrabFood_Item_Menu_Block}?Store={req.StoreNo}";
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var response = await client.GetAsync(path);
                        var resultStr = response.Content.ReadAsStringAsync().Result;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.OK,
                                Message = "Khóa món thành công",
                                Data = ""
                            };
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = response.StatusCode,
                                Message = response.ReasonPhrase,
                                Data = ""
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Data = JsonConvert.SerializeObject(ex);
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = JsonConvert.SerializeObject(ex)
                };
                return Json(result);
            }
        }

        // Khóa món kênh GrabFood : 23/06/2025,tungnt8: New
        
        [HttpPost]
        public ActionResult SetupLockItemByGrabFoodAPIV3(Item_Block_Model req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(result);
            }

            if (string.IsNullOrEmpty(req.Store))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Vui lòng chọn cửa hàng",
                    Data = null
                };
                return Json(result);
            }

            try
            {
                var model = new Item_Block();
                if (req.ItemListStr.ToString() == null || req.ItemListStr.ToString() == "")
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn sản phẩm để khóa / mở món",
                        Data = null
                    };
                    return Json(result);
                }

                var listItemGrabfood = JsonConvert.DeserializeObject<List<Item_Block_Active>>(req.ItemListStr);

                foreach (var a in listItemGrabfood)
                {
                    if (string.IsNullOrEmpty(a.ItemId))
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng chọn sản phẩm để khóa / mở món",
                            Data = null
                        };
                        return Json(result);
                    }
                }

                model = new Item_Block
                {
                    Store = req.Store,
                    Item = listItemGrabfood
                };
                
                using (var client = new HttpClient())
                {
                    try
                    {
                        var path = $"{GrabFood_Item_Block}?Store={req.Store}";
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);

                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var response = client.PostAsync(path, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var dataResponse = JsonConvert.DeserializeObject<ApiResponse>(resultStr);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            result = new ResultResponse
                            {
                                Status = dataResponse.StatusCode,
                                Message = dataResponse.Message,
                                Data = dataResponse.Msg
                            };
                        }
                        else
                        {
                            // Call api lần 2
                            var responseV2 = client.PostAsync(path, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                            var resultStrV2 = response.Content.ReadAsStringAsync().Result;
                            var dataResponseV2 = JsonConvert.DeserializeObject<ApiResponse>(resultStr);

                            result = new ResultResponse
                            {
                                Status = dataResponseV2.StatusCode,
                                Message = dataResponseV2.Message,
                                Data = dataResponseV2.Msg
                            };

                        }
                    }
                    catch (Exception ex)
                    {
                        result.Data = JsonConvert.SerializeObject(ex);
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = JsonConvert.SerializeObject(ex)
                };
                return Json(result);
            }
        }

        // Active món kênh GrabFood : 23/06/2025,tungnt8: New
        [HttpPost]
        public ActionResult SetupActiveItemByGrabFoodAPIV3(Item_Block_Model req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(result);
            }

            if (string.IsNullOrEmpty(req.Store))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Vui lòng chọn cửa hàng",
                    Data = null
                };
                return Json(result);
            }

            try
            {
                var model = new Item_Block();
                if (req.ItemListStr.ToString() == null || req.ItemListStr.ToString() == "")
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn sản phẩm để khóa / mở món",
                        Data = null
                    };
                    return Json(result);
                }

                var listItemGrabfood = JsonConvert.DeserializeObject<List<Item_Block_Active>>(req.ItemListStr);
                foreach (var a in listItemGrabfood)
                {
                    if (string.IsNullOrEmpty(a.ItemId))
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng chọn sản phẩm để khóa / mở món",
                            Data = null
                        };
                        return Json(result);
                    }
                }

                model = new Item_Block
                {
                    Store = req.Store,
                    Item = listItemGrabfood
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var path = $"{GrabFood_Item_Block}?Store={req.Store}";
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);

                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var response = client.PostAsync(path, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var dataResponse = JsonConvert.DeserializeObject<ApiResponse>(resultStr);
                        
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            result = new ResultResponse
                            {
                                Status = dataResponse.StatusCode,
                                Message = dataResponse.Message,
                                Data = dataResponse.Msg
                            };
                        }
                        else
                        {
                            // Call api lần 2
                            var responseV2 = client.PostAsync(path, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                            var resultStrV2 = response.Content.ReadAsStringAsync().Result;
                            var dataResponseV2 = JsonConvert.DeserializeObject<ApiResponse>(resultStr);

                            result = new ResultResponse
                            {
                                Status = dataResponseV2.StatusCode,
                                Message = dataResponseV2.Message,
                                Data = dataResponseV2.Msg
                            };

                        }
                    }
                    catch (Exception ex)
                    {
                        result.Data = JsonConvert.SerializeObject(ex);
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = JsonConvert.SerializeObject(ex)
                };
                return Json(result);
            }
        }

        // API : mở món sản phẩm bán trên kênh Grabfood

        [HttpPost]
        public ActionResult SetupActiveItemByGrabFoodAPI(List_Active_Item_Async req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            if (string.IsNullOrEmpty(req.StoreNo))
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Vui lòng chọn cửa hàng",
                    Data = null
                };
                return Json(result);
            }

            try
            {
                var model = new GrabFood_Active_Item_Async();
                var listItem = new List<GrabFood_Active_Item_Async>();
                var listItemGrabfood = JsonConvert.DeserializeObject<List<GrabFoodActiveItemAsyncModel>>(req.ItemListStr);
                foreach (var a in listItemGrabfood)
                {
                    if (string.IsNullOrEmpty(a.ItemNo))
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Không thấy mã sản phẩm. Vui lòng kiểm tra lại",
                            Data = null
                        };
                        return Json(result);
                    }

                    model = new GrabFood_Active_Item_Async
                    {
                        ItemId = a.ItemNo,
                        Store = req.StoreNo,
                        AvailableStatus = "AVAILABLE",
                        User = base.LoginUser.UserName
                    };
                    listItem.Add(model);
                }

                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

                if (string.IsNullOrEmpty(linkAPI))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(result);
                }

                var endPoint = $"{GrabFood_Active_Item_Async}";

                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(listItem);
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add("Authorization", "Basic UE9TOjk4NzY1NDMyMTA=");
                        var responseOption = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;

                        if (responseOption.StatusCode == HttpStatusCode.OK)  // Mở món thành công
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.OK,
                                Data = ""
                            };
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = responseOption.StatusCode,
                                Message = responseOption.ReasonPhrase,
                                Data = ""
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Data = JsonConvert.SerializeObject(ex);
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = JsonConvert.SerializeObject(ex)
                };
                return Json(result);
            }
        }

        // Khoa mon tren Central
        public ActionResult CreateProductLockOrUnlockByCentral(CreateProductLockModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            req.CreatedDate = DateTime.Now;
            var itemModel = JsonConvert.DeserializeObject<List<CreateProductLockListModel>>(req.ItemListStr);
            var model = new CreateProductLockModel
            {
                StoreNo = req.StoreNo,
                Status = req.Status,
                CreatedDate = req.CreatedDate,
                CreatedUser = req.CreatedUser,
                UpdatedDate = req.CreatedDate,
                ItemList = itemModel
            };
            var data = _productBLO.CreateProductLockOrUnlock(model);
            return Json(data);
        }

        // Khoa mon tại POS
        public ActionResult CreateProductLockOrUnlockByPOS(CreateProductLockModel req)
        {
            var flag = PingIP(req.POSTerminal);
            if (flag == false)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng kiểm tra lại kết nối mạng tại CH"
                });
            }

            req.CreatedUser = LoginUser.UserName;
            req.CreatedDate = DateTime.Now;
            var itemModel = JsonConvert.DeserializeObject<List<CreateProductLockListModel>>(req.ItemListStr);
            var model = new CreateProductLockModel
            {
                StoreNo = req.StoreNo,
                POSTerminal = req.POSTerminal,
                Status = req.Status,
                CreatedDate = req.CreatedDate,
                CreatedUser = req.CreatedUser,
                UpdatedDate = req.CreatedDate,
                ItemList = itemModel
            };
            var data = _productBLO.POS_CreateProductLockOrUnlock(model, POSUser, POSPass);
            return Json(data);
        }

        public ActionResult UnlockProductList()
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
            var itemNo = Request?.Form["Itemno"];
            var itemName = Request?.Form["ItemName"];
            var status = Request?.Form["Status"];
            var IPAddress = Request?.Form["IpAddress"];
            var serverType = Request?.Form["SourceType"];

            if (!string.IsNullOrEmpty(serverType))
            {
                serverType = serverType.Trim();
            }
            if (!string.IsNullOrEmpty(IPAddress))
            {
                IPAddress = IPAddress.Trim();
            }
            var userName = base.LoginUser.UserName;
            if (!string.IsNullOrEmpty(itemNo))
            {
                itemNo = itemNo.Trim();
            }
            if (!string.IsNullOrEmpty(itemName))
            {
                itemName = itemName.Trim();
            }
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            var data = new List<ProductLockListResponseModel>();

            if (serverType == "SERVER")
            {
                data = _productBLO.GetProductLockList(storeNo, itemNo, itemName, status, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            }
            else if (serverType == "POS")
            {
                data = _productBLO.POS_GetItemLockList(IPAddress, POSUser, POSPass, storeNo, userName, itemNo, itemName, status, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            }
            return Json(new DataTablesViewModel<ProductLockListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult CreateProductUnlockByModal(CreateProductUnlockModel req)
        {
            var data = new ResultResponseModel();
            req.CreatedUser = LoginUser.UserName;
            req.CreatedDate = DateTime.Now;
            var storeUnlockModel = JsonConvert.DeserializeObject<List<CreateStoreUnlockListModel>>(req.StoreNo);
            var productUnlockModel = JsonConvert.DeserializeObject<List<CreateProductUnlockListModel>>(req.ProductUnlockListStr);
            var model = new CreateProductUnlockModel
            {
                StoreNo = storeUnlockModel.FirstOrDefault().StoreNo,
                StoreName = storeUnlockModel.FirstOrDefault().StoreName,
                Status = req.Status,
                CreatedDate = req.CreatedDate,
                CreatedUser = req.CreatedUser,
                UpdatedDate = req.CreatedDate,
                ProductUnlockList = productUnlockModel,
                POSTerminal = req.POSTerminal,
                SourceType = req.SourceType
            };

            if (req.SourceType == "SERVER")
            {
                data = _productBLO.CreateProductUnlockByModal(model, POSUser, POSPass);
            }
            else if (req.SourceType == "POS")
            {
                var flag = PingIP(req.POSTerminal);
                if (flag == false)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                        Message = "Vui lòng kiểm tra lại mạng ST/CH"
                    });
                }
                data = _productBLO.POS_CreateProductUnlockByModal(model, POSUser, POSPass);
            }
            return Json(data);
        }

        // Khi chọn loại POS: thì sẽ mở khóa món trên DB local và DB Central
        [HttpPost]
        public ActionResult CreateProductUnlockByModalByCentral(CreateProductUnlockModel req)
        {
            var data = new ResultResponseModel();
            req.CreatedUser = LoginUser.UserName;
            req.CreatedDate = DateTime.Now;
            var storeUnlockModel = JsonConvert.DeserializeObject<List<CreateStoreUnlockListModel>>(req.StoreNo);
            var productUnlockModel = JsonConvert.DeserializeObject<List<CreateProductUnlockListModel>>(req.ProductUnlockListStr);

            var model = new CreateProductUnlockModel
            {
                StoreNo = storeUnlockModel.FirstOrDefault().StoreNo,
                StoreName = storeUnlockModel.FirstOrDefault().StoreName,
                Status = req.Status,
                CreatedDate = req.CreatedDate,
                CreatedUser = req.CreatedUser,
                UpdatedDate = req.CreatedDate,
                ProductUnlockList = productUnlockModel,
                POSTerminal = req.POSTerminal,
                SourceType = req.SourceType
            };           
            data = _productBLO.CreateProductUnlockByModal(model, POSUser, POSPass);
            return Json(data);
        }

        











    }
}