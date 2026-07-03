using Newtonsoft.Json;
using PLG.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Spire.Xls;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Business.Partner;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model.Partner;
using VCM.BLUEPOS.Model.Partner.GrabFoodModel;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Enums;
using VCM.BLUEPOS.Model.Common;


namespace PLG.Controllers
{
    public class PartnerController : BaseController
    {
        //------- NowFood (ShoppeFood)-------

        string NowFood_Get = WebConfigurationManager.AppSettings["NowFood_Get"];
        string NowFood_Create = WebConfigurationManager.AppSettings["NowFood_Create"];
        string NowFood_Update = WebConfigurationManager.AppSettings["NowFood_Update"];
        string NowFood_LockItem = WebConfigurationManager.AppSettings["NowFood_Status"];

        //------- GrabFood -------

        string GrabFood_Get_Item_List = WebConfigurationManager.AppSettings["GrabFood_Get_Item_List"];
        string GrabFood_Get_Menu = WebConfigurationManager.AppSettings["GrabFood_Get_Menu"];
        string GrabFood_Get_Category = WebConfigurationManager.AppSettings["GrabFood_Get_Category"];
        string GrabFood_Get_Item = WebConfigurationManager.AppSettings["GrabFood_Get_Item"];
        string GrabFood_Get_Group_By_Item = WebConfigurationManager.AppSettings["GrabFood_Get_Group_By_Item"];
        string GrabFood_Create_Options = WebConfigurationManager.AppSettings["GrabFood_Create_Options"];
        string GrabFood_Add_Options_Item = WebConfigurationManager.AppSettings["GrabFood_Add_Options_Item"];
        string GrabFood_Item_Update = WebConfigurationManager.AppSettings["GrabFood_Item_Update"];
        string GrabFood_GroupOption_Update = WebConfigurationManager.AppSettings["GrabFood_GroupOption_Update"];
        string GrabFood_Option_Update = WebConfigurationManager.AppSettings["GrabFood_Option_Update"];
        string GrabFood_LockItem = WebConfigurationManager.AppSettings["GrabFood_Status"];
        string GrabFood_Item_Option_Create = WebConfigurationManager.AppSettings["GrabFood_Item_Option_Create"];
        string GrabFood_SyncMenu_Notification = WebConfigurationManager.AppSettings["GrabFood_SyncMenu_Notification"];
        string GrabFood_Item_Menu_Block = WebConfigurationManager.AppSettings["GrabFood_Item_Menu_Block"];

        string GrabFood_Authorization = WebConfigurationManager.AppSettings["GrabFood_Authorization"];
        string GrabFood_Basic_Pass = WebConfigurationManager.AppSettings["GrabFood_Basic_Pass"];

        //------- BeFood -------

        string BeFood_Get_Item = WebConfigurationManager.AppSettings["BeFood_Get_Item"];
        string BeFood_Create = WebConfigurationManager.AppSettings["BeFood_Create"];
        string BeFood_Update = WebConfigurationManager.AppSettings["BeFood_Update"];
        string BeFood_LockItem = WebConfigurationManager.AppSettings["BeFood_Status"];

        string BeFood_Basic_Pass = WebConfigurationManager.AppSettings["BeFood_Basic_Pass"];


        string PLGUser = WebConfigurationManager.AppSettings["PLGUser"];
        string PLGPassword = WebConfigurationManager.AppSettings["PLGPassword"];
        string urlImgServer = WebConfigurationManager.AppSettings["urlImg"];
        string cdn_Image = WebConfigurationManager.AppSettings["CDN_Image"];

        private IPartnerBLO _partnerBLO { get; set; }
        private IAuthenBLO _authenBLO { get; set; }
        private ICommonBLO _commonBLO;
        private string linkAPI { get; set; }
        private readonly object lockRequest = new object();
        public PartnerController(IPartnerBLO partnerBLO, IAuthenBLO authen, ICommonBLO commonBLO)
        {
            _partnerBLO = partnerBLO;
            _authenBLO = authen;
            _commonBLO = commonBLO;
        }

        [HttpPost]
        public ActionResult GetMCH5(string MCH2)
        {
            var mch5 = new List<MCHComboboxModel>();
            try
            {
                if (!string.IsNullOrEmpty(MCH2))
                {
                    mch5 = _commonBLO.LoadComboxMCH5(MCH2);
                }
            }
            catch (Exception ex)
            {
            }
            return Json(mch5);
        }

        [DisplayName("Danh sách sản phẩm")]
        public ActionResult ItemListNowFood(int page = 1, string appcode = "", string storeno = "", string mch2 = "", string keyword = "", string status = "")
        {
            var model = new ViewItemListNowFoodModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;

                var data = _partnerBLO.GetItemListNowFood(appcode, storeno, mch2, keyword, status, out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

                ViewBag.ComboxCategory = _partnerBLO.LoadComboxCategory();
                ViewBag.SaleOrderType = _commonBLO.LoadComboxSalesOrderType();
                ViewBag.ListStore = _commonBLO.GetComboxStoreList();

                data = data.Select(a => new ItemListNowFoodResponseModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    Size = a.Size,
                    UnitPrice = a.UnitPrice,
                    CupType = a.CupType,
                    MCH2 = a.MCH2,
                    MCH2_Name = a.MCH2_Name,
                    StoreNo = a.StoreNo,
                    AppCode = a.AppCode,
                    Blocked = a.Blocked,
                    IsTopping = a.IsTopping,
                    Total = a.Total
                }).ToList();

                model = new ViewItemListNowFoodModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    AppCode = appcode,
                    StoreNo = storeno,
                    Status = status,
                    Keyword = keyword,
                    MCH2 = mch2        // Ma nganh hang,
                    //IsButtonAdd = role.RoleCode == "R0009" ? false : true
                };
            }
            catch (Exception ex)
            {
                model = new ViewItemListNowFoodModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0
                };
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult UpdateItemListNowFood(UpdateItemListNowFoodModel req)
        {
            var data = _partnerBLO.UpdateItemListNowFood(req);
            return Json(data);
        }

        [DisplayName("Khai báo sản phẩm")]
        public ActionResult ItemListByMappingPartnerCreate(int page = 1, string keyword = "")
        {
            var model = new CreateViewItemListNowFoodModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;

                //var data = _nowFoodBLO.Get_Item_List_PLH_By_Mapping(textSearch, out totalRow, page, pageSize);

                var data = _partnerBLO.GetItemListByMappingPartner(keyword, out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

                ViewBag.ListStoreHead = _partnerBLO.GetStoreHeadByNowFood();
                //ViewBag.ListCategory = _partnerBLO.LoadComboxCategory();
                ViewBag.ListPicture = _partnerBLO.LoadComboxPictureByNowFood();

                data = data.Select(a => new CreateItemListPLHResponseModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                    UnitPrice = a.UnitPrice,
                    Blocked = a.Blocked,
                    Total = a.Total
                }).ToList();

                model = new CreateViewItemListNowFoodModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    Keyword = keyword
                };
            }
            catch (Exception ex)
            {
                model = new CreateViewItemListNowFoodModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0
                };
            }
            return View(model);
        }

        //[DisplayName("Tạo sản phẩm")]
        //public ActionResult ItemListNowFoodByCreate(int page = 1, string textSearch = "")
        //{
        //    var model = new CreateViewItemListNowFoodModel { };
        //    try
        //    {
        //        var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
        //        var totalRow = 0;
        //        var pageSize = 120;
        //        var data = _nowFoodBLO.Get_Item_List_PLH_By_Mapping(textSearch, out totalRow, page, pageSize);
        //        var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

        //        ViewBag.ListStoreHead = _nowFoodBLO.GetStoreHeadByNowFood();
        //        ViewBag.ListCategory = _nowFoodBLO.LoadComboxCategory();
        //        ViewBag.ListPicture = _nowFoodBLO.LoadComboxPictureByNowFood();

        //        data = data.Select(a => new CreateItemListPLHResponseModel
        //        {
        //            ItemNo = a.ItemNo,
        //            ItemName = a.ItemName,
        //            ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
        //            BaseUnitOfMeasure = a.BaseUnitOfMeasure,
        //            SalesUnitOfMeasure = a.SalesUnitOfMeasure,
        //            UnitPrice = a.UnitPrice,
        //            Blocked = a.Blocked,
        //            Total = a.Total
        //        }).ToList();

        //        model = new CreateViewItemListNowFoodModel
        //        {
        //            ListItem = data,
        //            TotalRow = totalRow,
        //            PageSize = pageSize,
        //            TotalPageNumber = totasPage,
        //            Keyword = textSearch
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        model = new CreateViewItemListNowFoodModel
        //        {
        //            TotalRow = 0,
        //            TotalPageNumber = 0,
        //            PageSize = 0
        //        };
        //    }
        //    return View(model);
        //}

        //[HttpPost]
        //public ActionResult CreateItemNowFood(CreateItemNowFoodModel req)
        //{
        //    var result = new ResultResponse
        //    {
        //        Status = HttpStatusCode.OK,
        //        Message = "",
        //        Data = null
        //    };

        //    try
        //    {
        //        if (string.IsNullOrEmpty(linkAPI))
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadRequest,
        //                Message = $"Không tìm thấy link API trên hệ thống",
        //                Data = null
        //            };
        //            return Json(result);
        //        }

        //        // Check ItemNo đâ có bán trên NOWFOOD hay chưa ? trước khi tạo Sản phẩm bán trên NOWFOOD

        //        var checkItem = _nowFoodBLO.CheckCreateItemNowFood(req);

        //        if (checkItem.Status == VCM.BLUEPOS.Model.Enums.ResultEnum.Fail)
        //        {
        //            result = new ResultResponse
        //            {
        //                Status = HttpStatusCode.BadRequest,
        //                Message = $"Sản phẩm {req.partner_dish_id} này đâ có bán trên NOWFOOD. Vui lòng kiểm tra lại",
        //                Data = null
        //            };
        //            return Json(result);
        //        }

        //        var model = new APICreateItemNowFoodModel
        //        {
        //            restaurant_id = req.restaurant_id,
        //            partner_restaurant_id = req.partner_restaurant_id.Trim(),
        //            partner_dish_id = req.partner_dish_id.Trim(),
        //            name = string.IsNullOrEmpty(req.name) ? string.Empty : req.name.Trim(),
        //            partner_dish_group_id = req.partner_dish_group_id.Trim(),
        //            price = req.price,
        //            name_en = string.Empty,
        //            display_order = 0,
        //            description = string.IsNullOrEmpty(req.description) ? string.Empty : req.description.Trim(),
        //            picture_id = int.Parse(req.picture_id.Trim())              
        //        };

        //        var endPoint = $"{NowFood_Create}";

        //        using (var client = new HttpClient())
        //        {
        //            try
        //            {
        //                var jsonRequestAPI = JsonConvert.SerializeObject(model);
        //                client.Timeout = TimeSpan.FromMinutes(2);
        //                client.BaseAddress = new Uri(linkAPI);
        //                var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
        //                var readData = response.Content.ReadAsStringAsync();
        //                var jsonAPI = readData.Result;
        //                result.Data = JsonConvert.DeserializeObject<APINowFoodResponseModel>(jsonAPI);

        //                //result.Data = jsonAPI;               
        //                //result.Data = JsonConvert.DeserializeObject(jsonAPI);
        //                //result.Data = JsonConvert.DeserializeObject(jsonAPI);
        //                //result.Data = jsonAPI;
        //            }
        //            catch (Exception ex)
        //            {
        //                result.Data = JsonConvert.SerializeObject(ex);
        //            }
        //        }
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        result = new ResultResponse
        //        {
        //            Status = HttpStatusCode.InternalServerError,
        //            Message = $"Lỗi {ex.Message}",
        //            Data = JsonConvert.SerializeObject(ex)
        //        };
        //        return Json(result);
        //    }
        //}

        [HttpPost]
        public JsonResult ConvertImagesToBase64(string ImgName)
        {
            var model = new ConvertImagesToBase64Model
            {
                ImageName = ImgName
            };

            try
            {
                if (!string.IsNullOrEmpty(ImgName))
                {
                    byte[] imageArray = Encoding.ASCII.GetBytes(ImgName);
                    model.ImageBase64 = Convert.ToBase64String(imageArray);
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Lỗi xảy ra",
                });
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [DisplayName("Danh sách đơn hàng NowFood")]
        public ActionResult OrderSalesByNowFood()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.PermissionRole = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        public JsonResult GetOrderSalesListByNowFood()
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

            var fromDate = DateTime.Now;
            var toDate = DateTime.Now;
            if (!string.IsNullOrEmpty(Request?.Form["FromDate"]))
            {
                fromDate = Convert.ToDateTime(Request?.Form["FromDate"]);
            }
            if (!string.IsNullOrEmpty(Request?.Form["ToDate"]))
            {
                toDate = Convert.ToDateTime(Request?.Form["ToDate"]);
            }

            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var transactionType = Request?.Form["transactionType"]; // Trang thai don hang NowFood
            var _transactionType = 0;
            if (!string.IsNullOrEmpty(transactionType))
            {
                _transactionType = int.Parse(transactionType.ToString());
            }
            else
            {
                _transactionType = 0; // Tat ca
            }

            var statusPayment = Request?.Form["statusPayment"];  // Trang thai don hang trên POS
            if (!string.IsNullOrEmpty(statusPayment))
            {
                statusPayment = statusPayment.Trim();
            }

            var textSearchOrder = Request?.Form["textSearchOrder"];
            if (!string.IsNullOrEmpty(textSearchOrder))
            {
                textSearchOrder = textSearchOrder.Trim();
            }

            var shopeeOrder = Request?.Form["shopeeOrder"];
            var setServer = Request?.Form["SetServer"];
            var data = _partnerBLO.GetOrderSalesListByNowFood(fromDate, toDate, setServer, storeNo, _transactionType, statusPayment, textSearchOrder, shopeeOrder, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<OrderSalesListByNowFoodModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }
        public ActionResult ExportExcel_GetOrderSalesListByNowFood(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string TransactionType, string StatusPayment, string TextSearchOrder, string ShopeeOrder)
        {
            int _transactionType = 0;
            if (TransactionType == "")
            {
                _transactionType = 0; // tat ca
            }
            else
            {
                _transactionType = int.Parse(TransactionType.ToString());
            }

            var data = _partnerBLO.ExportExcel_GetOrderSalesListByNowFood(FromDate, ToDate, SetServer, StoreNo, _transactionType, StatusPayment, TextSearchOrder, ShopeeOrder);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.crt_date_Str,
                        x.pick_time_Str,
                        x.order_code,
                        x.restaurant_id,
                        x.partner_restaurant_id,
                        x.StatusStr,
                        x.update_flg,
                        x.note_for_shipper
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày tạo đơn hàng";
                    worksheet.Cells[1, 2].Value = "Thời gian pick hàng";
                    worksheet.Cells[1, 3].Value = "Số đơn hàng NowFood";
                    worksheet.Cells[1, 4].Value = "Cửa hàng (NowFood)";
                    worksheet.Cells[1, 5].Value = "Cửa hàng (PLH)";
                    worksheet.Cells[1, 6].Value = "Trạng thái đơn hàng trên NowFood";
                    worksheet.Cells[1, 7].Value = "Trạng thái đơn hàng trên POS";
                    worksheet.Cells[1, 8].Value = "Ghi chú";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 8])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"CheckSalesOrderListByNowFood_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public ActionResult LoadComboxStorePLHByMappingNowFood()
        {
            var listData = new List<Shopee_Restaurant_Model>();
            try
            {
                listData = _partnerBLO.LoadStorePLHByMappingNowFood();
            }
            catch (Exception ex)
            {
            }
            return Json(listData.OrderBy(d => d.partner_restaurant_id));
        }
        public ActionResult LoadComboxStoreByPartner(string partner)
        {
            var listData = new List<PartnerRestaurantModel>();
            try
            {
                listData = _partnerBLO.LoadComboxStoreByPartner(partner);
            }
            catch (Exception ex)
            {
            }
            return Json(listData.OrderBy(d => d.partner_restaurant_id));
        }

        public ActionResult LoadComboxStoreByGrabFood()
        {
            var data = _partnerBLO.LoadComboxStoreByGrabFood();
            return Json(data);
        }

        public ActionResult LoadComboxCategoryByPartner(string partner)
        {
            var listData = new List<VCM.BLUEPOS.Model.Partner.OptionModel>();
            try
            {
                listData = _partnerBLO.LoadComboxCategoryByPartner(partner);
            }
            catch (Exception ex)
            {
            }
            return Json(listData);
        }

        [DisplayName("Khai báo Cửa hàng Head")]
        public ActionResult StoreHeadNowFoodByCreate()
        {
            ViewBag.ListStorePLH = _partnerBLO.LoadComboxStorePLH();
            return View();
        }

        [HttpPost]
        public JsonResult GetStoreHeadNowFoodByCreate()
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
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Split('-')[0]; // Ma CH Phúc Long
            }
            var isHeader = Request?.Form["IsHeader"];
            var data = _partnerBLO.GetStoreHeadNowFoodByCreate(storeNo, isHeader, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ShopeeRestaurantResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        [HttpPost]
        public JsonResult CreateStoreHeadByNowFood(CreateShopeeRestaurantModel req)
        {
            if (string.IsNullOrEmpty(req.partner_restaurant_id))
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Vui lòng chọn Cửa hàng",
                });
            }
            var data = _partnerBLO.CreateStoreHeadByNowFood(req);
            return Json(data);
        }
        [HttpPost]
        public JsonResult UpdateStoreHeadByNowFood(UpdateShopeeRestaurantModel req)
        {
            if (string.IsNullOrEmpty(req.partner_restaurant_id))
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Vui lòng chọn Cửa hàng",
                });
            }
            var data = _partnerBLO.UpdateStoreHeadByNowFood(req);
            return Json(data);
        }
        [DisplayName("Kiểm tra loại ly")]
        public ActionResult ItemListNowFoodByCheckCup()
        {
            var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            ViewBag.ListCupType = _commonBLO.LoadComboxCupType();
            ViewBag.ListStore = listStoreByUser;
            return View();
        }
        public JsonResult GetCupTypeByItem()
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
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var cupType = Request?.Form["CupType"];
            if (!string.IsNullOrEmpty(cupType))
            {
                cupType = cupType.Trim();
            }

            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }
            var data = _partnerBLO.GetCupTypeByItemNowFood(storeNo, cupType, status, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ItemSalesOnAppModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }
        public ActionResult ExportExcelGetCupTypeByItem(string StoreNo, string CupType, string Status, string TextSearch)
        {
            var data = _partnerBLO.ExportExcelGetCupTypeByItem(StoreNo, CupType, Status, TextSearch);
            if (data != null || data.Count > 0)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StoreNo,
                        x.ItemNo,
                        x.ItemName,
                        x.CupType,
                        x.Size,
                        x.Uom,
                        x.UnitPrice,
                        x.Blocked,
                        x.IsSync,
                        x.IsApplyAll,
                        x.SyncResults,
                        x.IsTopping,
                        x.IsSales,
                        x.CrtDate,
                        x.ChgeDate,
                        x.AppCode
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Cửa hàng";
                    worksheet.Cells[1, 2].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 3].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 4].Value = "Loại ly";
                    worksheet.Cells[1, 5].Value = "Size";
                    worksheet.Cells[1, 6].Value = "ĐVT";
                    worksheet.Cells[1, 7].Value = "Đơn giá";
                    worksheet.Cells[1, 8].Value = "Trạng thái";
                    worksheet.Cells[1, 9].Value = "IsSync";
                    worksheet.Cells[1, 10].Value = "IsApplyAll";
                    worksheet.Cells[1, 11].Value = "SyncResults";
                    worksheet.Cells[1, 12].Value = "IsTopping";
                    worksheet.Cells[1, 13].Value = "IsSales";
                    worksheet.Cells[1, 14].Value = "Ngày tạo";
                    worksheet.Cells[1, 15].Value = "Ngày cập nhật";
                    worksheet.Cells[1, 16].Value = "Kênh";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 16])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"CupTypeByItemList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public ActionResult UpdateStatusByOrder(UpdateStatusModel req)
        {
            req.UrserName = base.LoginUser.UserName;
            req.UpdateCreated = DateTime.Now;
            var data = _partnerBLO.UpdateStatusByOrder(req);
            return Json(data);
        }

        [HttpPost]
        public ActionResult CreateItemByMappingPartner(CreateItemPartnerModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            try
            {
                if (string.IsNullOrEmpty(req.partner_id))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn đối tác trước khi lưu sản phẩm",
                        Data = null
                    };
                    return Json(result);
                }

                if (req.partner_id == "NOF")
                {
                    linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
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

                    //var checkItem = _nowFoodBLO.CheckCreateItemNowFood(req);
                    var checkItem = _partnerBLO.CheckCreateItemByMappingPartner(req); // 25/12/2024, tungnt8

                    if (checkItem.Status == VCM.BLUEPOS.Model.Enums.ResultEnum.Fail)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Sản phẩm {req.partner_dish_id} này đâ có bán trên kênh đối tác. Vui lòng kiểm tra lại",
                            Data = null
                        };
                        return Json(result);
                    }

                    var model = new APICreateItemNowFoodModel
                    {
                        restaurant_id = req.restaurant_id,
                        partner_restaurant_id = req.partner_restaurant_id.Trim(),
                        partner_dish_id = req.partner_dish_id.Trim(),
                        name = string.IsNullOrEmpty(req.name) ? string.Empty : req.name.Trim(),
                        partner_dish_group_id = req.partner_dish_group_id.Trim(),
                        price = req.price,
                        name_en = string.Empty,
                        display_order = 0,
                        description = string.IsNullOrEmpty(req.description) ? string.Empty : req.description.Trim(),
                        picture_id = int.Parse(req.picture_id.Trim())
                    };

                    var endPoint = $"{NowFood_Create}";
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            var jsonRequestAPI = JsonConvert.SerializeObject(model);
                            client.Timeout = TimeSpan.FromMinutes(2);
                            client.BaseAddress = new Uri(linkAPI);
                            var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                            var readData = response.Content.ReadAsStringAsync();
                            var jsonAPI = readData.Result;
                            result.Data = JsonConvert.DeserializeObject<APINowFoodResponseModel>(jsonAPI);
                        }
                        catch (Exception ex)
                        {
                            result.Data = JsonConvert.SerializeObject(ex);
                        }
                    }
                }
                else if (req.partner_id == "BEF")
                {
                    linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_BEFOOD").Value;
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

                    //var checkItem = _nowFoodBLO.CheckCreateItemNowFood(req);
                    var checkItem = _partnerBLO.CheckCreateItemByMappingPartner(req); // 25/12/2024, tungnt8

                    if (checkItem.Status == VCM.BLUEPOS.Model.Enums.ResultEnum.Fail)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Sản phẩm {req.partner_dish_id} này đâ có bán trên kênh đối tác. Vui lòng kiểm tra lại",
                            Data = null
                        };
                        return Json(result);
                    }

                    var model = new ApiCreateItemByBeFoodModel
                    {
                        restaurant_id = req.restaurant_id,
                        merchant_item_name = string.IsNullOrEmpty(req.name) ? string.Empty : req.name.Trim(),
                        merchant_item_description = string.IsNullOrEmpty(req.name) ? string.Empty : req.name.Trim(),

                        merchant_item_image = "https://media-dev.be.com.vn/bizops/image/f74a6f0f-9b57-11ef-a75d-76c293f39403/original",
                        //merchant_item_image = req.merchant_item_image,

                        merchant_item_price = req.price,
                        merchant_category_id = req.partner_dish_group_id.Trim(),
                        //partner_category_id = req.partner_dish_id.Trim(),
                        partner_category_id = "112233",
                        reference_id = req.partner_dish_id.Trim()
                    };

                    var endPoint = $"{BeFood_Create}";
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            var jsonRequestAPI = JsonConvert.SerializeObject(model);
                            client.Timeout = TimeSpan.FromMinutes(2);
                            client.BaseAddress = new Uri(linkAPI);
                            client.DefaultRequestHeaders.Clear();
                            //client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeHeaderValue);
                            client.DefaultRequestHeaders.Add("Authorization", "Basic Qmx1ZXBvczpCbHVlcG9z");

                            var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                            if (response.StatusCode.ToString() == "OK")  // Tạo thành công
                            {
                                result = new ResultResponse
                                {
                                    Status = HttpStatusCode.OK,
                                    Message = $"Khai báo sản phẩm thành công",
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

        // Call API : Load danh sach san pham theo Store Head
        [HttpPost]
        public async Task<ActionResult> APIItemListNowFoodByStore(string StoreNo, string ItemNo, string TextSearch)
        {
            try
            {
                var data = new List<NowFoodItemListModel>();
                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = "Có lỗi xảy ra",
                    Data = null
                };

                ViewBag.ListStoreHead = _partnerBLO.GetStoreHeadByNowFood();
                ViewBag.ListCategory = _partnerBLO.LoadComboxCategory();
                ViewBag.ListPicture = _partnerBLO.LoadComboxPictureByNowFood();
                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    var result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(result);
                }

                using (var client = new HttpClient())
                {
                    try
                    {
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        if (!string.IsNullOrEmpty(StoreNo))
                        {
                            StoreNo = StoreNo.Trim();
                        }
                        var path = $"{NowFood_Get}?StoreNo={StoreNo}";
                        var response = await client.GetAsync(path);
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<APIListItemNowFoodResponseModel>(resultStr);
                        if (result.meta.code == 200)
                        {
                            if (ItemNo != "" || TextSearch != "")
                            {
                                var listItem = new List<NowFoodItemListModel>();
                                var model = new NowFoodItemListModel();
                                foreach (var item in result.data)
                                {
                                    model = new NowFoodItemListModel()
                                    {
                                        dish_id = item.dish_id,
                                        dish_group_id = item.dish_group_id,
                                        partner_dish_id = item.partner_dish_id,
                                        name = item.name,
                                        display_order = item.display_order,
                                        price = item.price,
                                        description = item.description,
                                        is_active = item.is_active,
                                        partner_dish_group_id = item.partner_dish_group_id,
                                        created_time = item.created_time,
                                        updated_time = item.updated_time,
                                        picture = item.picture
                                    };
                                    listItem.Add(model);
                                }
                                var listData = listItem.Where(c => c.partner_dish_id == ItemNo.TrimEnd() || c.name.ToLower().Contains(TextSearch.ToLower().TrimEnd()));
                                data = listData.ToList();
                            }
                            else if (ItemNo == "" && TextSearch == "")
                            {
                                var listItem = new List<NowFoodItemListModel>();
                                var model = new NowFoodItemListModel();
                                foreach (var item in result.data)
                                {
                                    model = new NowFoodItemListModel()
                                    {
                                        dish_id = item.dish_id,
                                        dish_group_id = item.dish_group_id,
                                        partner_dish_id = item.partner_dish_id,
                                        name = item.name,
                                        display_order = item.display_order,
                                        price = item.price,
                                        description = item.description,
                                        is_active = item.is_active,
                                        partner_dish_group_id = item.partner_dish_group_id,
                                        created_time = item.created_time,
                                        updated_time = item.updated_time,
                                        picture = item.picture
                                    };
                                    listItem.Add(model);
                                }
                                data = listItem.ToList();
                            }
                        }
                        else
                        {
                            kq = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"Có lỗi xảy ra",
                                Data = null
                            };
                            return Json(kq);
                        }
                    }
                    catch (Exception ex)
                    {
                        kq = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Có lỗi xảy ra",
                            Data = null
                        };
                        return Json(kq);
                    }
                }
                return Json(data);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [DisplayName("Khóa món sản phẩm")]
        public ActionResult ItemListNowFoodByLock(int page = 1, string textsearch = "")
        {
            var model = new CreateViewItemListNowFoodModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;
                //var data = _partnerBLO.Get_Item_List_PLH_By_Mapping(textsearch, out totalRow, page, pageSize);
                var data = _partnerBLO.GetItemListByMappingPartner(textsearch, out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;
                ViewBag.ListStoreHead = _partnerBLO.GetStoreHeadByNowFood();
                ViewBag.ListCategory = _partnerBLO.LoadComboxCategory();
                ViewBag.ListPicture = _partnerBLO.LoadComboxPictureByNowFood();

                data = data.Select(a => new CreateItemListPLHResponseModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                    UnitPrice = a.UnitPrice,
                    Blocked = a.Blocked,
                    Total = a.Total
                }).ToList();

                model = new CreateViewItemListNowFoodModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    Keyword = textsearch
                };
            }
            catch (Exception ex)
            {
                model = new CreateViewItemListNowFoodModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0
                };
            }
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> ApiItemListNowFoodByLock(string StoreNo, string ItemNo, string TextSearch)
        {
            var data = new List<NowFoodItemListModel>();
            var kq = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest,
                Message = "Có lỗi xảy ra",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
            if (string.IsNullOrEmpty(linkAPI))
            {
                var result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
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
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        if (!string.IsNullOrEmpty(StoreNo))
                        {
                            StoreNo = StoreNo.Trim();
                        }
                        var path = $"{NowFood_Get}?StoreNo={StoreNo}";
                        var response = await client.GetAsync(path);
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<APIListItemNowFoodResponseModel>(resultStr);
                        if (result.meta.code == 200)
                        {
                            if (ItemNo != "" || TextSearch != "")
                            {
                                var listItem = new List<NowFoodItemListModel>();
                                var model = new NowFoodItemListModel();
                                foreach (var item in result.data)
                                {
                                    model = new NowFoodItemListModel()
                                    {
                                        dish_id = item.dish_id,
                                        dish_group_id = item.dish_group_id,
                                        partner_dish_id = item.partner_dish_id,
                                        name = item.name,
                                        display_order = item.display_order,
                                        price = item.price,
                                        description = item.description,
                                        is_active = item.is_active,
                                        partner_dish_group_id = item.partner_dish_group_id,
                                        created_time = item.created_time,
                                        updated_time = item.updated_time,
                                        picture = item.picture
                                    };
                                    listItem.Add(model);
                                }
                                var listData = listItem.Where(c => c.partner_dish_id == ItemNo.Trim() || c.name.ToLower().Contains(TextSearch.ToLower().Trim()));
                                data = listData.ToList();
                            }
                            else if (ItemNo == "" && TextSearch == "")
                            {
                                var listItem = new List<NowFoodItemListModel>();
                                var model = new NowFoodItemListModel();
                                foreach (var item in result.data)
                                {
                                    model = new NowFoodItemListModel()
                                    {
                                        dish_id = item.dish_id,
                                        dish_group_id = item.dish_group_id,
                                        partner_dish_id = item.partner_dish_id,
                                        name = item.name,
                                        display_order = item.display_order,
                                        price = item.price,
                                        description = item.description,
                                        is_active = item.is_active,
                                        partner_dish_group_id = item.partner_dish_group_id,
                                        created_time = item.created_time,
                                        updated_time = item.updated_time,
                                        picture = item.picture
                                    };
                                    listItem.Add(model);
                                }
                                data = listItem.ToList();
                            }
                        }
                        else
                        {
                            kq = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"Có lỗi xảy ra",
                                Data = null
                            };
                            return Json(kq);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    return Json(data);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public ActionResult _SetupDetailItemNowFoodByLock(SetupLockItemByNowFoodModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            try
            {
                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    var kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(kq);
                }

                var model_dishes = new DishesResponseModel
                {
                    partner_dish_id = req.partner_dish_id,
                    status = req.isActive
                };

                var model_dishes_list = new List<DishesResponseModel>();
                if (model_dishes != null)
                {
                    model_dishes_list.Add(new DishesResponseModel()
                    {
                        partner_dish_id = model_dishes.partner_dish_id,
                        status = model_dishes.status
                    });
                }

                var model = new CreateLockItemByNowFoodModel
                {
                    restaurant_id = int.Parse(req.restaurant_id.ToString()),    // Mã Store
                    partner_restaurant_id = req.partner_restaurant_id.Trim(),   // Mã Store Header
                    is_apply_all = false,                                       // Default = false
                    dishes = model_dishes_list
                };

                var endPoint = $"{NowFood_LockItem}";
                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI);
                        var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var jsonAPI = readData.Result;
                        result.Data = JsonConvert.DeserializeObject<Api_CreateLockItemByNowFoodResponseModel>(jsonAPI);
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

        [HttpPost]
        public JsonResult GetItemNameByNowFood(string partner_dish_id, string partner_restaurant_id)
        {
            var data = _partnerBLO.GetItemNameByNowFood(partner_dish_id, partner_restaurant_id);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetGroupName(string partner_dish_group_id)
        {
            var data = _partnerBLO.GetGroupNameByNowFood(partner_dish_group_id);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetCategoryByGrabFood(string id)
        {
            var data = _partnerBLO.GetCategoryByGrabFood(id);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetItemNameByGrabFood(string id)
        {
            var data = _partnerBLO.GetItemNameByGrabFood(id);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetImagesBase64ByGrabFood(string id)
        {
            var data = _partnerBLO.GetImagesBase64ByGrabFood(id);
            return Json(data);
        }

        //[DisplayName("Cập nhật sản phẩm")]
        //public ActionResult ItemListNowFoodByUpdate(int page = 1, string textsearch = "")
        //{
        //    var model = new UpdateViewItemListNowFoodModel { };
        //    try
        //    {
        //        var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
        //        var totalRow = 0;
        //        var pageSize = 120;
        //        var data = _partnerBLO.Get_Item_List_PLH_By_Mapping_By_Update(textsearch, out totalRow, page, pageSize);
        //        var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

        //        ViewBag.ListStoreHead = _partnerBLO.GetStoreHeadByNowFood();
        //        ViewBag.ListCategory = _partnerBLO.LoadComboxCategory();
        //        ViewBag.ListPicture = _partnerBLO.LoadComboxPictureByNowFood();

        //        data = data.Select(a => new UpdateItemListPLHResponseModel
        //        {
        //            ItemNo = a.ItemNo,
        //            ItemName = a.ItemName,
        //            ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
        //            BaseUnitOfMeasure = a.BaseUnitOfMeasure,
        //            SalesUnitOfMeasure = a.SalesUnitOfMeasure,
        //            UnitPrice = a.UnitPrice,
        //            Blocked = a.Blocked,
        //            Total = a.Total
        //        }).ToList();

        //        model = new UpdateViewItemListNowFoodModel
        //        {
        //            ListItem = data,
        //            TotalRow = totalRow,
        //            PageSize = pageSize,
        //            TotalPageNumber = totasPage,
        //            Keyword = textsearch
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        model = new UpdateViewItemListNowFoodModel
        //        {
        //            TotalRow = 0,
        //            TotalPageNumber = 0,
        //            PageSize = 0
        //        };
        //    }
        //    return View(model);
        //}
        public ActionResult GetComboxStoreByPartner(string partnerCode)
        {
            var data = _partnerBLO.GetStoreHeadByPartner(partnerCode);
            return Json(data);
        }

        public ActionResult GetComboxStoreNowFood()
        {
            var data = _partnerBLO.GetComboxStoreNowFood();
            return Json(data);
        }
        public ActionResult GetComboxStoreGrabFood()
        {
            var data = _partnerBLO.GetComboxStoreByGrabFood();
            return Json(data);
        }
        public ActionResult LoadComboxCategoryGrabFood()
        {
            var data = _partnerBLO.LoadComboxCategoryGrabFood();
            return Json(data);
        }
        public async Task<ActionResult> GetComboxGroupOptionGrabFood(string ItemNo)
        {
            try
            {
                if (string.IsNullOrEmpty(ItemNo))
                {
                    var result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không có thông tin mã sản phẩm. Vui lòng kiểm tra lại",
                        Data = null
                    };
                    return Json(result);
                }

                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    var result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(result);
                }

                using (var client = new HttpClient())
                {
                    var result = new ResultResponse();
                    var groupOption = new List<GrabFood_ModifierGroup>();

                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);
                    //client.DefaultRequestHeaders.Add("Authorization", "Basic UE9TOjk4NzY1NDMyMTA=");

                    var endPoint = $"{GrabFood_Get_Group_By_Item}?ItemID={ItemNo}&IsGroup=true";
                    var response = await client.GetAsync(endPoint);
                    var data = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode.ToString() == "OK")
                    {
                        var groupList = JsonConvert.DeserializeObject<List<GrabFood_ModifierGroup>>(data);
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = response.ReasonPhrase,
                            Data = groupList.ToList()
                        };
                        return Json(result);
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Có lỗi xảy ra",
                            Data = null
                        };
                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [DisplayName("Cập nhật sản phẩm")]
        public async Task<ActionResult> UpdateItemByPartner(int page = 1, string textsearch = "", string partnerCode = "", string storeNo = "")
        {
            var model = new UpdateViewItemListByPartnerModel { };
            try
            {
                var data = new List<UpdateItemListByPartnerResponseModel>();
                var dataGrabfood = new List<GrabFood_Category>();
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;

                ViewBag.ListCategory = _partnerBLO.LoadComboxCategory();
                ViewBag.ListCategoryByGrabFood = _partnerBLO.LoadComboxCategoryGrabFood();
                ViewBag.ListPicture = _partnerBLO.LoadComboxPictureByPartner(partnerCode);

                if (partnerCode == "NOF")
                {
                    data = _partnerBLO.GetItemMappingNowFoodByUpdate(partnerCode, textsearch, out totalRow, page, pageSize);
                }
                else if (partnerCode == "GRF")  // grabfood, call api danh sach san pham
                {
                    linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;
                    if (string.IsNullOrEmpty(linkAPI))
                    {
                        var result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Không tìm thấy link API trên hệ thống",
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
                                var result = new ResultResponse();
                                var grabfoodCategory = new List<GrabFood_Category>();

                                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                                client.BaseAddress = new Uri(linkAPI);
                                client.Timeout = TimeSpan.FromMinutes(2);

                                if (!string.IsNullOrEmpty(storeNo))
                                {
                                    storeNo = storeNo.Trim();
                                }

                                var path = $"{GrabFood_Get_Menu}?partnerMerchantID={storeNo}";

                                var response = await client.GetAsync(path);
                                var resultStr = response.Content.ReadAsStringAsync().Result;
                                var categoryList = JsonConvert.DeserializeObject<GrabFood_Category>(resultStr);

                                if (categoryList != null)
                                {
                                    var listCategory = new List<GrabFood_Category>();
                                    var modelCategory = new GrabFood_Category();
                                    foreach (var a in listCategory)
                                    {
                                        modelCategory = new GrabFood_Category()
                                        {
                                            Id = a.Id,
                                            Name = a.Name,
                                            Sequence = a.Sequence,
                                            AvailableStatus = a.AvailableStatus,
                                            Items = a.Items.ToList()
                                        };
                                        listCategory.Add(modelCategory);
                                    }
                                    grabfoodCategory = listCategory.ToList();
                                    dataGrabfood = grabfoodCategory.ToList();
                                }
                                else
                                {
                                    result = new ResultResponse
                                    {
                                        Status = HttpStatusCode.BadRequest,
                                        Message = $"Có lỗi xảy ra",
                                        Data = null
                                    };
                                    return Json(result);
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                            return Json(dataGrabfood);
                        }
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }

                }
                else if (partnerCode == "BEF")
                {
                    data = _partnerBLO.GetItemMappingBeFoodByUpdate(partnerCode, textsearch, out totalRow, page, pageSize);
                }
                var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

                data = data.Select(a => new UpdateItemListByPartnerResponseModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                    UnitPrice = a.UnitPrice,
                    Blocked = a.Blocked,
                    Total = a.Total
                }).ToList();

                model = new UpdateViewItemListByPartnerModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    Keyword = textsearch
                };
            }
            catch (Exception ex)
            {
                model = new UpdateViewItemListByPartnerModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0
                };
            }
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> APIItemUpdateByPartner(string PartnerCode, string StoreNo, string ItemNo, string TextSearch)
        {
            var data = new List<NowFoodItemListModel>();
            var dataGrabfood = new List<GrabFood_Category>();
            var kq = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            if (PartnerCode == "NOF")
            {
                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(kq);
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                            client.BaseAddress = new Uri(linkAPI);
                            client.Timeout = TimeSpan.FromMinutes(2);

                            if (!string.IsNullOrEmpty(StoreNo))
                            {
                                StoreNo = StoreNo.Trim();
                            }

                            var path = $"{NowFood_Get}?StoreNo={StoreNo}";
                            var response = await client.GetAsync(path);
                            var resultStr = response.Content.ReadAsStringAsync().Result;
                            var result = JsonConvert.DeserializeObject<ApiListItemNowFoodByUpdateResponseModel>(resultStr);

                            if (result.meta.code == 200)
                            {
                                var listItem = new List<NowFoodItemListModel>();
                                var model = new NowFoodItemListModel();

                                if (ItemNo != "" || TextSearch != "")
                                {
                                    foreach (var item in result.data)
                                    {
                                        model = new NowFoodItemListModel()
                                        {
                                            dish_id = item.dish_id,
                                            dish_group_id = item.dish_group_id,
                                            partner_dish_id = item.partner_dish_id,
                                            name = item.name,
                                            display_order = item.display_order,
                                            price = item.price,
                                            description = item.description,
                                            is_active = item.is_active,
                                            partner_dish_group_id = item.partner_dish_group_id,
                                            created_time = item.created_time,
                                            updated_time = item.updated_time,
                                            picture = item.picture
                                        };
                                        listItem.Add(model);
                                    }
                                    var listData = listItem.Where(c => c.partner_dish_id == ItemNo.Trim() || c.name.ToLower().Contains(TextSearch.ToLower().Trim()));
                                    data = listData.ToList();
                                }
                                else if (ItemNo == "" && TextSearch == "")
                                {
                                    foreach (var item in result.data)
                                    {
                                        model = new NowFoodItemListModel()
                                        {
                                            dish_id = item.dish_id,
                                            dish_group_id = item.dish_group_id,
                                            partner_dish_id = item.partner_dish_id,
                                            name = item.name,
                                            display_order = item.display_order,
                                            price = item.price,
                                            description = item.description,
                                            is_active = item.is_active,
                                            partner_dish_group_id = item.partner_dish_group_id,
                                            created_time = item.created_time,
                                            updated_time = item.updated_time,
                                            picture = item.picture
                                        };
                                        listItem.Add(model);
                                    }
                                    data = listItem.ToList();
                                }
                            }
                            else
                            {
                                kq = new ResultResponse
                                {
                                    Status = HttpStatusCode.BadRequest,
                                    Message = $"Có lỗi xảy ra",
                                    Data = null
                                };
                                return Json(kq);
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        return Json(data);
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            else if (PartnerCode == "GRF")
            {
                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(kq);
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            var result = new ResultResponse();
                            var grabfoodCategory = new List<GrabFood_Category>();

                            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                            client.BaseAddress = new Uri(linkAPI);
                            client.Timeout = TimeSpan.FromMinutes(2);

                            if (!string.IsNullOrEmpty(StoreNo))
                            {
                                StoreNo = StoreNo.Trim();
                            }

                            var path = $"{GrabFood_Get_Menu}?partnerMerchantID={StoreNo}";
                            var response = await client.GetAsync(path);
                            var resultStr = response.Content.ReadAsStringAsync().Result;
                            var categoryList = JsonConvert.DeserializeObject<GrabFood_Category>(resultStr);

                            if (categoryList != null)
                            {
                                var listCategory = new List<GrabFood_Category>();
                                var modelCategory = new GrabFood_Category();
                                foreach (var a in listCategory)
                                {
                                    modelCategory = new GrabFood_Category()
                                    {
                                        Id = a.Id,
                                        Name = a.Name,
                                        Sequence = a.Sequence,
                                        AvailableStatus = a.AvailableStatus,
                                        Items = a.Items.ToList()
                                    };
                                    listCategory.Add(modelCategory);
                                }
                                grabfoodCategory = listCategory.ToList();
                                dataGrabfood = grabfoodCategory.ToList();
                            }
                            else
                            {
                                result = new ResultResponse
                                {
                                    Status = HttpStatusCode.BadRequest,
                                    Message = $"Có lỗi xảy ra",
                                    Data = null
                                };
                                return Json(result);
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        return Json(dataGrabfood);
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            else if (PartnerCode == "BEF")
            {
                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_BEFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(kq);
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                            client.BaseAddress = new Uri(linkAPI);
                            client.Timeout = TimeSpan.FromMinutes(2);

                            if (!string.IsNullOrEmpty(StoreNo))
                            {
                                StoreNo = StoreNo.Trim();
                            }

                            var path = $"{NowFood_Get}?StoreNo={StoreNo}";
                            var response = await client.GetAsync(path);
                            var resultStr = response.Content.ReadAsStringAsync().Result;
                            var result = JsonConvert.DeserializeObject<ApiListItemNowFoodByUpdateResponseModel>(resultStr);

                            if (result.meta.code == 200)
                            {
                                var listItem = new List<NowFoodItemListModel>();
                                var model = new NowFoodItemListModel();

                                if (ItemNo != "" || TextSearch != "")
                                {
                                    foreach (var item in result.data)
                                    {
                                        model = new NowFoodItemListModel()
                                        {
                                            dish_id = item.dish_id,
                                            dish_group_id = item.dish_group_id,
                                            partner_dish_id = item.partner_dish_id,
                                            name = item.name,
                                            display_order = item.display_order,
                                            price = item.price,
                                            description = item.description,
                                            is_active = item.is_active,
                                            partner_dish_group_id = item.partner_dish_group_id,
                                            created_time = item.created_time,
                                            updated_time = item.updated_time,
                                            picture = item.picture
                                        };
                                        listItem.Add(model);
                                    }

                                    var listData = listItem.Where(c => c.partner_dish_id == ItemNo.Trim() || c.name.ToLower().Contains(TextSearch.ToLower().Trim()));
                                    data = listData.ToList();

                                }
                                else if (ItemNo == "" && TextSearch == "")
                                {
                                    foreach (var item in result.data)
                                    {
                                        model = new NowFoodItemListModel()
                                        {
                                            dish_id = item.dish_id,
                                            dish_group_id = item.dish_group_id,
                                            partner_dish_id = item.partner_dish_id,
                                            name = item.name,
                                            display_order = item.display_order,
                                            price = item.price,
                                            description = item.description,
                                            is_active = item.is_active,
                                            partner_dish_group_id = item.partner_dish_group_id,
                                            created_time = item.created_time,
                                            updated_time = item.updated_time,
                                            picture = item.picture
                                        };
                                        listItem.Add(model);
                                    }
                                    //List<NowFoodItemListModel> NowFoodList = JsonConvert.DeserializeObject<List<NowFoodItemListModel>>(result.data.ToString());
                                    data = listItem.ToList();
                                }
                            }
                            else
                            {
                                kq = new ResultResponse
                                {
                                    Status = HttpStatusCode.BadRequest,
                                    Message = $"Có lỗi xảy ra",
                                    Data = null
                                };
                                return Json(kq);
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        return Json(data);
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return Json(data);
        }

        [HttpPost]
        public async Task<ActionResult> GetItemShopeeFoodByUpdate(string StoreNo, string ItemNo, string TextSearch)
        {
            var data = new List<NowFoodItemListModel>();
            var kq = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
            if (string.IsNullOrEmpty(linkAPI))
            {
                kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(kq);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        if (!string.IsNullOrEmpty(StoreNo))
                        {
                            StoreNo = StoreNo.Trim();
                        }

                        var path = $"{NowFood_Get}?StoreNo={StoreNo}";
                        var response = await client.GetAsync(path);
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<ApiListItemNowFoodByUpdateResponseModel>(resultStr);

                        if (result.meta.code == 200)
                        {
                            var listItem = new List<NowFoodItemListModel>();
                            var model = new NowFoodItemListModel();
                            if (ItemNo != "" || TextSearch != "")
                            {
                                foreach (var item in result.data)
                                {
                                    model = new NowFoodItemListModel()
                                    {
                                        dish_id = item.dish_id,
                                        dish_group_id = item.dish_group_id,
                                        partner_dish_id = item.partner_dish_id,
                                        name = item.name,
                                        display_order = item.display_order,
                                        price = item.price,
                                        description = item.description,
                                        is_active = item.is_active,
                                        partner_dish_group_id = item.partner_dish_group_id,
                                        created_time = item.created_time,
                                        updated_time = item.updated_time,
                                        picture = item.picture
                                    };
                                    listItem.Add(model);
                                }
                                var listData = listItem.Where(c => c.partner_dish_id == ItemNo.Trim() || c.name.ToLower().Contains(TextSearch.ToLower().Trim()));
                                data = listData.ToList();
                            }
                            else if (ItemNo == "" && TextSearch == "")
                            {
                                foreach (var item in result.data)
                                {
                                    model = new NowFoodItemListModel()
                                    {
                                        dish_id = item.dish_id,
                                        dish_group_id = item.dish_group_id,
                                        partner_dish_id = item.partner_dish_id,
                                        name = item.name,
                                        display_order = item.display_order,
                                        price = item.price,
                                        description = item.description,
                                        is_active = item.is_active,
                                        partner_dish_group_id = item.partner_dish_group_id,
                                        created_time = item.created_time,
                                        updated_time = item.updated_time,
                                        picture = item.picture
                                    };
                                    listItem.Add(model);
                                }
                                data = listItem.ToList();
                            }
                        }
                        else
                        {
                            kq = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"Có lỗi xảy ra",
                                Data = null
                            };
                            return Json(kq);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    return Json(data);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public ActionResult _UpdateDetailItemNoByNowFood(UpdateItemNoListByNowFoodModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_NOWFOOD").Value;
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

            try
            {
                var model = new UpdateItemNoListByNowFoodModel
                {
                    restaurant_id = int.Parse(req.restaurant_id.ToString()),    // Mã Store
                    partner_restaurant_id = req.partner_restaurant_id.Trim(),   // Mã Store Header
                    partner_dish_id = req.partner_dish_id,
                    name = req.name,
                    partner_dish_group_id = req.partner_dish_group_id,
                    price = req.price,
                    name_en = string.Empty,
                    display_order = 0,              // Default = 0, ngành hàng
                    description = string.Empty,
                    picture_id = req.picture_id
                };

                var endPoint = $"{NowFood_Update}";
                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI);
                        var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var jsonAPI = readData.Result;
                        result.Data = JsonConvert.DeserializeObject<Api_UpdateItemByNowFoodResponseModel>(jsonAPI);
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

        //----- GRAB FOOD -----

        [HttpPost]
        public async Task<ActionResult> Get_All_Item_By_GrabFood()
        {
            try
            {
                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

                if (string.IsNullOrEmpty(linkAPI))
                {
                    var kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(kq);
                }

                using (var client = new HttpClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var endPoint = $"{GrabFood_Get_Item}?isTopping=true&ModifierGroupID=TOPPING";
                    //var path = "http://10.235.78.110:8000/partner/v1/Grab/Get_all_item                     

                    var response = await client.GetAsync(endPoint);
                    var data = response.Content.ReadAsStringAsync().Result;
                    var result = new ResultResponse();

                    if (response.StatusCode.ToString() == "OK")
                    {
                        var itemList = JsonConvert.DeserializeObject<List<Get_All_Item_Grab_Food_Model>>(data);
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = response.ReasonPhrase,
                            Data = itemList.ToList()
                        };
                        return Json(result);
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Có lỗi xảy ra",
                            Data = null
                        };
                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetItemGrabFoodByUpdate(string StoreNo, string Category, string ItemNo, string TextSearch)
        {
            var data = new List<GrabFood_Category>();
            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {
                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(kq);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var result = new ResultResponse();
                    var grabfoodCategory = new List<GrabFood_Category>();

                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var path = $"{GrabFood_Get_Menu}";
                    if (!string.IsNullOrEmpty(Category))
                    {
                        path = $"{GrabFood_Get_Menu}?partnerMerchantID={StoreNo}&categoriesID={Category}";
                    }
                    else
                    {
                        path = $"{GrabFood_Get_Menu}?partnerMerchantID={StoreNo}";
                    }

                    var response = await client.GetAsync(path);
                    var resultStr = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var categoryList = JsonConvert.DeserializeObject<List<GrabFood_Category>>(resultStr);
                        if (categoryList != null)
                        {
                            var listCategory = new List<GrabFood_Category>();
                            var modelCategory = new GrabFood_Category();

                            if (!string.IsNullOrEmpty(TextSearch))
                            {
                                foreach (var a in categoryList)
                                {
                                    foreach (var b in a.Items)
                                    {
                                        if (b.Id == ItemNo)
                                        {
                                            modelCategory = new GrabFood_Category()
                                            {
                                                Id = a.Id,
                                                Name = a.Name,
                                                Sequence = a.Sequence,
                                                AvailableStatus = a.AvailableStatus,
                                                Items = a.Items.Where(c => c.Id == ItemNo).ToList()
                                            };
                                            listCategory.Add(modelCategory);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var a in categoryList)
                                {
                                    modelCategory = new GrabFood_Category()
                                    {
                                        Id = a.Id,
                                        Name = a.Name,
                                        Sequence = a.Sequence,
                                        AvailableStatus = a.AvailableStatus,
                                        Items = a.Items.ToList()
                                    };
                                    listCategory.Add(modelCategory);
                                }
                            }
                            grabfoodCategory = listCategory.ToList();
                            data = grabfoodCategory.ToList();
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = response.ReasonPhrase,
                                Data = null
                            };
                            return Json(result);
                        }

                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = response.ReasonPhrase,
                            Data = data
                        };
                        return Json(result);
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Có lỗi xảy ra",
                            Data = null
                        };
                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // 23/04/2025, tungnt8: new

        [HttpPost]
        public async Task<ActionResult> GetItemGrabFoodByUpdateV2(string StoreNo, string CategoryID, string ItemNo, string Status, string TextSearch)
        {
            var data = new List<Get_Item>();
            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {
                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(kq);
            }

            if (StoreNo == null || StoreNo == "")
            {
                StoreNo = "";
            }

            if (CategoryID == "" || CategoryID == null)
            {
                CategoryID = "";
            }

            try
            {
                using (var client = new HttpClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var path = "";
                    var result = new ResultResponse();
                    var ListItemGrab = new List<Get_Item>();
                    var grabfoodCategory = new List<GrabFood_Category>();
                    var listData = new List<Get_Item>();

                    path = $"{GrabFood_Get_Item_List}?Store={StoreNo}";

                    var response = await client.GetAsync(path);
                    var resultStr = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var listItemGrab = JsonConvert.DeserializeObject<List<Get_Item>>(resultStr);
                        if (listItemGrab != null)
                        {
                            var model = new Get_Item();
                            if (!string.IsNullOrEmpty(CategoryID))
                            {
                                foreach (var a in listItemGrab)
                                {
                                    if (a.CategoryID == CategoryID)
                                    {
                                        model = new Get_Item()
                                        {
                                            Id = a.Id,
                                            Name = a.Name,
                                            Description = a.Description,
                                            Price = a.Price,
                                            AvailableStatus = a.AvailableStatus,
                                            CategoryID = a.CategoryID,
                                            CategoryName = a.CategoryName,
                                            Photo = a.Photo,
                                            IsTopping = a.IsTopping,
                                            IsCampaign = a.IsCampaign,  // = 1 : combo
                                            ModifierGroup = a.ModifierGroup.ToList()
                                        };
                                        listData.Add(model);
                                    }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(TextSearch))
                                {
                                    foreach (var a in listItemGrab)
                                    {
                                        if (a.Id == ItemNo)
                                        {
                                            model = new Get_Item()
                                            {
                                                Id = a.Id,
                                                Name = a.Name,
                                                Description = a.Description,
                                                Price = a.Price,
                                                AvailableStatus = a.AvailableStatus,
                                                CategoryID = a.CategoryID,
                                                CategoryName = a.CategoryName,
                                                Photo = a.Photo,
                                                IsTopping = a.IsTopping,
                                                IsCampaign = a.IsCampaign, // = 1 : combo
                                                ModifierGroup = a.ModifierGroup.ToList()
                                            };
                                            listData.Add(model);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var a in listItemGrab)
                                    {
                                        model = new Get_Item()
                                        {
                                            Id = a.Id,
                                            Name = !string.IsNullOrEmpty(a.Name) ? a.Name : string.Empty,
                                            Description = !string.IsNullOrEmpty(a.Description) ? a.Description : string.Empty,
                                            Price = a.Price,
                                            AvailableStatus = a.AvailableStatus,
                                            CategoryID = !string.IsNullOrEmpty(a.CategoryID) ? a.CategoryID : string.Empty,
                                            CategoryName = !string.IsNullOrEmpty(a.CategoryName) ? a.CategoryName : string.Empty,
                                            Photo = !string.IsNullOrEmpty(a.Photo) ? a.Photo : string.Empty,
                                            IsTopping = a.IsTopping,
                                            IsCampaign = a.IsCampaign,  // = 1 : combo
                                            ModifierGroup = a.ModifierGroup.ToList()
                                        };
                                        listData.Add(model);
                                    }
                                }
                            }

                            ListItemGrab = listData.ToList();
                            // filter = Status
                            if (!string.IsNullOrEmpty(Status))
                            {
                                data = ListItemGrab.Where(a => a.AvailableStatus == Status).ToList();
                            }
                            else
                            {
                                data = ListItemGrab.ToList();
                            }
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = response.ReasonPhrase,
                                Data = null
                            };
                            return Json(result);
                        }
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = response.ReasonPhrase,
                            Data = data
                        };
                        return Json(result);
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Có lỗi xảy ra",
                            Data = null
                        };
                        return Json(result);
                    }

                    // -- Không dùng

                    //else // 16/06/2025,filter by Store
                    //{
                    //    path = $"{GrabFood_Get_Menu}?StoreNo={StoreNo}";
                    //    var response = await client.GetAsync(path);

                    //    if (response.StatusCode == HttpStatusCode.OK)
                    //    {
                    //        var resultStr = response.Content.ReadAsStringAsync().Result;
                    //        var categoryList = JsonConvert.DeserializeObject<List<GrabFood_Category>>(resultStr);
                    //        if (categoryList != null)
                    //        {
                    //            var listCategory = new List<GrabFood_Category>();
                    //            var modelCategory = new GrabFood_Category();
                    //            foreach (var a in categoryList)
                    //            {
                    //                modelCategory = new GrabFood_Category()
                    //                {
                    //                    Id = a.Id,
                    //                    Name = a.Name,
                    //                    Sequence = a.Sequence,
                    //                    AvailableStatus = a.AvailableStatus,
                    //                    Items = a.Items.ToList()
                    //                };
                    //                listCategory.Add(modelCategory);
                    //            }
                    //            grabfoodCategory = listCategory.ToList();
                    //            result = new ResultResponse
                    //            {
                    //                Status = HttpStatusCode.OK,
                    //                Message = response.ReasonPhrase,
                    //                Data = grabfoodCategory
                    //            };
                    //        }
                    //        else
                    //        {
                    //            result = new ResultResponse
                    //            {
                    //                Status = HttpStatusCode.BadRequest,
                    //                Message = response.ReasonPhrase,
                    //                Data = null
                    //            };
                    //            return Json(result);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        result = new ResultResponse
                    //        {
                    //            Status = HttpStatusCode.BadRequest,
                    //            Message = response.ReasonPhrase,
                    //            Data = null
                    //        };
                    //        return Json(result);
                    //    }
                    //    return Json(result);
                    //}


                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetOptionGroupGrabFoodByUpdate(string StoreNo, string Category, string ItemNo)
        {
            var data = new List<Get_Item>();
            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {
                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };

                return Json(kq);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var ListItemGrab = new List<Get_Item>();
                    var result = new ResultResponse();

                    var path = $"{GrabFood_Get_Item_List}";
                    var response = await client.GetAsync(path);
                    var resultStr = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var listItemGrab = JsonConvert.DeserializeObject<List<Get_Item>>(resultStr);
                        if (listItemGrab != null)
                        {
                            var listData = new List<Get_Item>();
                            var model = new Get_Item();

                            foreach (var a in listItemGrab)
                            {
                                if (a.Id == ItemNo)
                                {
                                    model = new Get_Item()
                                    {
                                        Id = a.Id,
                                        Name = a.Name,
                                        Description = a.Description,
                                        Price = a.Price,
                                        AvailableStatus = a.AvailableStatus,
                                        CategoryID = a.CategoryID,
                                        CategoryName = a.CategoryName,
                                        Photo = a.Photo,
                                        IsTopping = a.IsTopping,
                                        IsCampaign = a.IsCampaign,
                                        ModifierGroup = a.ModifierGroup.ToList()
                                    };
                                    listData.Add(model);
                                    break;
                                }
                            }
                            ListItemGrab = listData.ToList();
                            data = ListItemGrab.ToList();
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = response.ReasonPhrase,
                                Data = null
                            };
                            return Json(result);
                        }

                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = response.ReasonPhrase,
                            Data = data
                        };
                        return Json(result);
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Có lỗi xảy ra",
                            Data = null
                        };
                        return Json(result);
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public ActionResult CreateItemOptionByGrabFood(Modifier_Create req)
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

            try
            {
                var user = base.LoginUser.UserName;
                var model = new Modifier_Create
                {
                    ItemID = req.ItemID,
                    Name = string.Empty,
                    ModifierGroupId = req.ModifierGroupId,
                    AvailableStatus = req.AvailableStatus,
                    User = user
                };

                var endPoint = $"{GrabFood_Create_Options}?isTopping=true"; // Add Item cho Topping

                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        if (response.StatusCode.ToString() == "Created")  // = 201 : thêm thành công
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.OK,
                                Message = response.ReasonPhrase,
                                Data = string.Empty
                            };
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = response.StatusCode,
                                Message = response.ReasonPhrase,
                                Data = string.Empty
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

        [HttpPost]
        public ActionResult CreateGroupOptionByGrabFood(Add_Options_Item req)
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

            try
            {
                var model = new Add_Options_Item
                {
                    ModifierGroupID = req.ModifierGroupID,
                    AvailableStatus = req.AvailableStatus,
                    ItemID = req.ItemID,
                    ParentId = 0,
                    User = base.LoginUser.UserName
                };

                var endPoint = $"{GrabFood_Add_Options_Item}"; // Add Group Option
                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        if (response.StatusCode.ToString() == "Created")  // = 201 : thêm thành công
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.OK,
                                Message = response.ReasonPhrase,
                                Data = string.Empty
                            };
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = response.StatusCode,
                                Message = response.ReasonPhrase,
                                Data = string.Empty
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

        [HttpPost]
        public ActionResult Update_Item_Async_By_Grabfood(Item_Update_Model req)
        {
            var data = new List<Item_Update>();
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;

            if (string.IsNullOrEmpty(linkAPI))
            {

                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };

                return Json(kq);
            }

            try
            {
                var listOption = new List<ModifierGroup_Update>();

                if (req.ListOptionGroup != null)
                {
                    if (req.ListOptionGroup.ChonSIze == true && req.ListOptionGroup.Size5k == false && req.ListOptionGroup.Size10k == false)
                    {
                        var kq = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng chọn size"
                        };
                        return Json(kq);
                    }

                    if (req.ListOptionGroup.Size5k)
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "CHON_SIZE_5K",
                            AvailableStatus = "AVAILABLE"
                        });
                    }
                    else
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "CHON_SIZE_5K",
                            AvailableStatus = "UNAVAILABLE"
                        });
                    }

                    if (req.ListOptionGroup.Size10k)
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "CHON_SIZE_10K",
                            AvailableStatus = "AVAILABLE"
                        });
                    }
                    else
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "CHON_SIZE_10K",
                            AvailableStatus = "UNAVAILABLE"
                        });
                    }

                    if (req.ListOptionGroup.DoNgot)
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "OPTION_NGOT",
                            AvailableStatus = "AVAILABLE"
                        });
                    }
                    else
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "OPTION_NGOT",
                            AvailableStatus = "UNAVAILABLE"
                        });
                    }

                    if (req.ListOptionGroup.Vitra)
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "OPTION_TRA",
                            AvailableStatus = "AVAILABLE"
                        });
                    }
                    else
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "OPTION_TRA",
                            AvailableStatus = "UNAVAILABLE"
                        });
                    }

                    if (req.ListOptionGroup.ThemTopping)
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "TOPPING",
                            AvailableStatus = "AVAILABLE"
                        });
                    }
                    else
                    {
                        listOption.Add(new ModifierGroup_Update
                        {
                            ModifierGroupId = "TOPPING",
                            AvailableStatus = "UNAVAILABLE"
                        });
                    }
                }

                //-----------------------------------

                var model = new Item_Update
                {
                    Id = req.Id,
                    Name = req.Name.Trim(),
                    CategoryId = req.CategoryId,
                    AvailableStatus = req.AvailableStatus,
                    Photo = (!string.IsNullOrEmpty(req.Photo)) ? req.Photo : string.Empty,
                    Description = string.IsNullOrEmpty(req.Description) ? string.Empty : req.Description.Trim(),
                    ModifierGroup = listOption,
                    User = base.LoginUser.UserName
                };

                var endPoint = $"{GrabFood_Item_Update}";

                using (var client = new HttpClient())
                {
                    var jsonAPI = JsonConvert.SerializeObject(model);
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var response = client.PostAsync(endPoint, new StringContent(jsonAPI, Encoding.UTF8, "application/json")).Result;
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
                        return Json(result);
                    }
                    else
                    {
                        result = new ResultResponse
                        {
                            Status = dataResponse.StatusCode,
                            Message = dataResponse.Message,
                            Data = dataResponse.Msg
                        };
                        return Json(result);
                    }
                }
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

        [HttpPost]
        public ActionResult UpdateOptionItemByGrabFood(string ListGroupOption)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            try
            {
                var model = new GrabFood_Modifier_Update();
                var listItem = new List<GrabFood_Modifier_Update>();

                var listGroupOption = JsonConvert.DeserializeObject<List<GrabFood_Modifier_Update>>(ListGroupOption);

                foreach (var a in listGroupOption)
                {
                    if (string.IsNullOrEmpty(a.Name))
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Tên sản phẩm không được để trống. Vui lòng kiểm tra lại",
                            Data = null
                        };
                        return Json(result);
                    }
                    else
                    {
                        if (a.Name.Length > 40)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"Tên sản phẩm có chiều dài tối đa 40 ký tự. Vui lòng kiểm tra lại",
                                Data = null
                            };
                            return Json(result);
                        }
                    }

                    model = new GrabFood_Modifier_Update
                    {
                        Id = a.Id,
                        ModifierGroupId = a.ModifierGroupId,
                        Name = a.Name,
                        Sequence = a.Sequence,
                        AvailableStatus = a.AvailableStatus,
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

                var endPoint_Option = $"{GrabFood_Option_Update}";

                using (var client = new HttpClient())
                {
                    try
                    {
                        //------ Call api : /partner/v1/Grab/Update_Modifier_Async

                        var jsonRequestapiOption = JsonConvert.SerializeObject(listItem);

                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var responseOption = client.PostAsync(endPoint_Option, new StringContent(jsonRequestapiOption, Encoding.UTF8, "application/json")).Result;

                        if (responseOption.StatusCode == HttpStatusCode.OK || responseOption.StatusCode.ToString() == "Created")  // Cập nhật thành công
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.OK,
                                Message = "Cập nhật thành công",
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

        public async Task<ActionResult> _SetupItemListLockByGrabFood()
        {
            var listData = new List<Item_Update>();
            var kq = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;
            if (string.IsNullOrEmpty(linkAPI))
            {
                kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(kq);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var result = new ResultResponse();
                    var model = new Item_Update();
                    var listItem = new List<Get_All_Item_Grab_Food_Model>();

                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var path = $"{GrabFood_Get_Item}?isTopping=false";
                    var response = await client.GetAsync(path);
                    var resultStr = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        listItem = JsonConvert.DeserializeObject<List<Get_All_Item_Grab_Food_Model>>(resultStr);
                        if (listItem != null)
                        {
                            foreach (var a in listItem.ToList())
                            {
                                model = new Item_Update
                                {
                                    Id = a.Id,
                                    Name = a.Name,
                                    Description = a.Description,
                                    AvailableStatus = a.AvailableStatus,
                                    User = base.LoginUser.UserName
                                };
                                listData.Add(model);
                            }
                        }
                    }
                    return Json(listData);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [DisplayName("Mapping cửa hàng")]
        public ActionResult StorePartnerMapping()
        {
            ViewBag.ListStorePLH = _partnerBLO.LoadComboxStorePLH();
            return View();
        }

        [HttpPost]
        public JsonResult GetStoreMappingByGrabFood()
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
            var status = Request?.Form["Status"];
            var textSearch = Request?.Form["TextSearch"];

            var data = _partnerBLO.GetStoreMappingByGrabFood(storeNo, status, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<GrabFood_Store_Model> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        [ParentAuthorize("StorePartnerMapping")]
        public ActionResult ImportExcel_StoreByGrabFood(HttpPostedFileBase importFile)
        {
            try
            {
                if (importFile == null)
                    return Json(new
                    {
                        Status = 0,
                        Message = "Không có file nào được chọn"
                    });

                string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
                if (fileExtension != ".xls" && fileExtension != ".xlsx")
                {
                    return Json(new
                    {
                        Status = 0,
                        Message = "Định dạng file excel không đúng"
                    });
                }

                var UserName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;

                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);

                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();

                    // Convert data

                    var listStore = new List<ImportStoreGrabFoodModel>();

                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        var excel_GrabMerchantID = data.Rows[i]["GrabMerchantID"] == null ? string.Empty : data.Rows[i]["GrabMerchantID"].ToString().TrimEnd();
                        var excel_StoreNo = data.Rows[i]["StoreNo"] == null ? string.Empty : data.Rows[i]["StoreNo"].ToString().TrimEnd();
                        var excel_StoreName = data.Rows[i]["StoreName"] == null ? string.Empty : data.Rows[i]["StoreName"].ToString().TrimEnd();

                        var temp = new ImportStoreGrabFoodModel
                        {
                            GrabMerchantID = excel_GrabMerchantID,
                            StoreNo = excel_StoreNo,
                            StoreName = excel_StoreName,
                            UserName = UserName
                        };
                        listStore.Add(temp);
                    }

                    // Validate

                    if (listStore.Count == 0)
                    {
                        return Json(new { Status = 0, Message = $"Không có dữ liệu import. Vui lòng kiểm tra lại file import" });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.GrabMerchantID)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [GrabMerchantID] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.StoreNo)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [StoreNo] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.StoreName)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [StoreName] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    // Existed

                    var checkExisted_GrabMerchantID = listStore.GroupBy(x => new { x.GrabMerchantID }, (key, g) => new
                    {
                        GrabMerchantID = key.GrabMerchantID,
                        ListData = g.ToList()
                    }).Where(x => x.ListData.Count > 1)
                     .Select(x => $"{x.GrabMerchantID}"
                   ).ToList();

                    //var checkExistedGrabMerchantID = listStore.GroupBy(x => new { x.GrabMerchantID, x.StoreNo, x.StoreName }, (key, g) => new
                    //{
                    //    GrabMerchantID = key.GrabMerchantID,
                    //    StoreNo = key.StoreNo,
                    //    StoreName = key.StoreName,
                    //    ListData = g.ToList()
                    //}).Where(x => x.ListData.Count > 1)
                    //  .Select(x => $"{x.GrabMerchantID}-{x.StoreNo}-{ x.ListData.FirstOrDefault()?.StoreName}"
                    //).ToList();

                    var checkExistedData = listStore.GroupBy(x => new { x.StoreNo, x.StoreName }, (key, g) => new
                    {
                        Code = key.StoreName,
                        StoreNo = key.StoreNo,
                        ListData = g.ToList()
                    }).Where(x => x.ListData.Count > 1)
                      .Select(x => $"{x.StoreNo}-{ x.ListData.FirstOrDefault()?.StoreName}"
                    ).ToList();

                    // Check trùng CH trước khi thêm mới

                    if (checkExisted_GrabMerchantID.Count > 0)
                    {
                        var errorData = string.Join("|", checkExisted_GrabMerchantID).TrimEnd();
                        return Json(new
                        {
                            Status = 2,
                            Message = $"Danh sách mã CH GrabFood {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại file import",
                            Data = errorData
                        });
                    }

                    if (checkExistedData.Count > 0)
                    {
                        var errorData = string.Join("|", checkExistedData).TrimEnd();
                        return Json(new
                        {
                            Status = 2,
                            Message = $"Danh sách mã CH PLH {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại file import",
                            Data = errorData
                        });
                    }
                    var insert = _partnerBLO.ImportExcel_MappingStoreGrabFood(listStore);
                    return Json(new { Status = insert.Item1, Message = insert.Item2, Data = insert.Item3 });
                }
                catch (Exception ex)
                {
                    return Json(new { Status = 0, Message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = 0, Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateStatusStoreGrab(string GrabMerchantID, string StoreNo, string Status)
        {
            try
            {
                var UserName = base.LoginUser.UserName;
                var data = _partnerBLO.UpdateStatusStoreGrab(GrabMerchantID, StoreNo, Status, UserName);
                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
                });
            }
        }

        [HttpPost]
        public JsonResult UpdateStoreGrabFood(string GrabMerchantIDOld, string GrabMerchantIDNew, string StoreNo)
        {
            try
            {
                if (string.IsNullOrEmpty(GrabMerchantIDNew))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "Vui lòng nhập mã cửa hàng GrabFood"
                    });
                }

                var UserName = base.LoginUser.UserName;
                var data = _partnerBLO.UpdateStoreGrab(GrabMerchantIDOld, GrabMerchantIDNew, StoreNo, UserName);

                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
                });
            }
        }

        [DisplayName("Tạo Topping")]
        public ActionResult SetupToppingOptionPartner()
        {
            ViewBag.ListToppingGrab = _partnerBLO.GetToppingByGrabFood();
            return View();
        }
        public ActionResult _ViewItemListToppingPartner()
        {
            return PartialView();
        }

        [HttpPost]
        public ActionResult LoadItemGroupByTopping()
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

                var itemNo = Request?.Form["ItemNo"];
                var mota = Request?.Form["ItemName"];
                var groupCode = Request?.Form["GroupCode"];
                var recordsTotal = 0;

                var result = _partnerBLO.ListItemByGroup(itemNo, mota, groupCode, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<GrabFood_SetupItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch
            {
                return View(new List<GrabFood_SetupItemModel>());
            }
        }

        [HttpPost]
        public JsonResult CreateToppingPartner(string jsonToppingGrab, string partner, string isFunc)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "Thành công"
            };

            try
            {

                linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_GRABFOOD").Value;
                if (string.IsNullOrEmpty(linkAPI))
                {
                    var kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Không tìm thấy link API trên hệ thống"
                    };
                    result = kq;
                }

                if (isFunc == "Created")
                {
                    if (string.IsNullOrEmpty(jsonToppingGrab))
                    {
                        var kq = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = "Vui lòng chọn sản phẩm tạo topping"
                        };
                        result = kq;
                    }
                }
                else if (isFunc == "Deleted")
                {
                    if (string.IsNullOrEmpty(jsonToppingGrab))
                    {
                        var kq = new ResultResponse
                        {
                            Status = HttpStatusCode.OK,
                            Message = "Xóa Topping thành công"
                        };
                        result = kq;
                    }
                }

                var modelToppingGrab = JsonConvert.DeserializeObject<List<ToppingGrabPartnerRequestModel>>(jsonToppingGrab);

                // model add topping

                var modelGrabTopping = new GrabFood_Modifier_Create();
                var modelGrabToppingList = new List<GrabFood_Modifier_Create>();

                if (modelToppingGrab != null)
                {
                    if (isFunc == "Created")
                    {
                        foreach (var item in modelToppingGrab)
                        {
                            if (item.ItemNoRef == null || item.ItemNoRef == "")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = HttpStatusCode.BadRequest,
                                    Message = "Vui lòng chọn mã sản phẩm"
                                };
                                result = kq;
                            }

                            if (item.ItemNameRef == null || item.ItemNameRef == "")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = HttpStatusCode.BadRequest,
                                    Message = "Tên sản phẩm không được để trống / rỗng. Vui lòng kiểm tra lại"
                                };
                                result = kq;
                            }

                            if (item.UOMRef == null || item.UOMRef == "")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = HttpStatusCode.BadRequest,
                                    Message = "Đơn vị sản phẩm không được để trống / rỗng. Vui lòng kiểm tra lại"
                                };
                                result = kq;
                            }

                            modelGrabTopping = new GrabFood_Modifier_Create
                            {
                                ItemID = item.ItemNoRef,
                                Name = item.ItemNameRef,
                                UOM = item.UOMRef,
                                ModifierGroupId = "TOPPING",
                                AvailableStatus = "AVAILABLE",  // luôn có hiệu lực khi thêm
                                Merchant = "GRABFOOD",
                                User = base.LoginUser.UserName
                            };
                            modelGrabToppingList.Add(modelGrabTopping);
                        }
                    }
                    else if (isFunc == "Deleted")
                    {
                        foreach (var item in modelToppingGrab)
                        {
                            modelGrabTopping = new GrabFood_Modifier_Create
                            {
                                ItemID = item.ItemNoRef,
                                Name = item.ItemNameRef,
                                UOM = item.UOMRef,
                                ModifierGroupId = "TOPPING",
                                AvailableStatus = "UNAVAILABLE",  // xóa topping
                                Merchant = "GRABFOOD",
                                User = base.LoginUser.UserName
                            };
                            modelGrabToppingList.Add(modelGrabTopping);
                        }
                    }
                }
                else
                {
                    modelGrabToppingList = null;
                }

                var endpointTopping = $"{GrabFood_Item_Option_Create}";  // api/v1/Grab/Item/options

                using (var client = new HttpClient())
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    if (modelGrabToppingList != null)
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(modelGrabToppingList);
                        var response = client.PostAsync(endpointTopping, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var data = JsonConvert.DeserializeObject<ApiResponseModel>(resultStr);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (isFunc == "Created")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = data.StatusCode,
                                    Message = "Tạo Topping thành công"
                                };
                                result = kq;
                            }
                            else if (isFunc == "Deleted")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = data.StatusCode,
                                    Message = "Xóa Topping thành công"
                                };
                                result = kq;
                            }
                        }
                        else
                        {
                            if (isFunc == "Created")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = data.StatusCode,
                                    Message = data.Message
                                };
                                result = kq;
                            }
                            else if (isFunc == "Deleted")
                            {
                                var kq = new ResultResponse
                                {
                                    Status = data.StatusCode,
                                    Message = $"Có lỗi xảy ra. Vui lòng kiểm tra lại API tạo topping"
                                };
                                result = kq;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(result);
        }

        // Đồng bộ Menu GrabFood
        [HttpPost]
        public ActionResult SyncMenuByGrabFood(string StoreNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };

            if (StoreNo == null || StoreNo == "")
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Vui lòng chọn cửa hàng hoặc (chọn ALL)",
                    Data = null
                };
                return Json(result);
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

            try
            {

                using (var client = new HttpClient())
                {
                    try
                    {
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                        client.BaseAddress = new Uri(linkAPI);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                        var endPoint_SyncMenu = $"{GrabFood_SyncMenu_Notification}/{StoreNo}";
                        var response = client.PutAsync(endPoint_SyncMenu, new StringContent(StoreNo, Encoding.UTF8, "application/json")).Result;
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var data = JsonConvert.DeserializeObject<ApiResponse>(resultStr);

                        if (response.StatusCode == HttpStatusCode.OK)  // Sync thành công
                        {
                            result = new ResultResponse
                            {
                                Status = data.StatusCode,
                                Message = data.Message,
                                Data = data.Msg
                            };
                        }
                        else
                        {
                            result = new ResultResponse
                            {
                                Status = data.StatusCode,
                                Message = data.Message,
                                Data = data.Msg
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

        //--------- BeFood ---------
        public ActionResult LoadComboxStoreByBeFood()
        {
            var data = _partnerBLO.LoadComboxStoreByBeFood();
            return Json(data);
        }

        [HttpPost]
        [ParentAuthorize("StorePartnerMapping")]
        public JsonResult GetStoreMappingByBeFood()
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
            var status = Request?.Form["Status"];
            var textSearch = Request?.Form["TextSearch"];

            var data = _partnerBLO.GetStoreMappingByBeFood(storeNo, status, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<BeFood_Store_Model> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        [ParentAuthorize("StorePartnerMapping")]
        public ActionResult ImportExcel_StoreByBeFood(HttpPostedFileBase importFile)
        {
            try
            {
                if (importFile == null)
                    return Json(new
                    {
                        Status = 0,
                        Message = "Không có file nào được chọn"
                    });

                string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
                if (fileExtension != ".xls" && fileExtension != ".xlsx")
                {
                    return Json(new
                    {
                        Status = 0,
                        Message = "Định dạng file excel không đúng"
                    });
                }

                var UserName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;
                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();

                    // Convert data
                    var listStore = new List<ImportExcelStoreBeFoodModel>();
                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        var excel_BeFoodMerchantID = data.Rows[i]["BeFoodMerchantID"] == null ? string.Empty : data.Rows[i]["BeFoodMerchantID"].ToString().TrimEnd();
                        var excel_StoreNo = data.Rows[i]["StoreNo"] == null ? string.Empty : data.Rows[i]["StoreNo"].ToString().TrimEnd();
                        var excel_StoreName = data.Rows[i]["StoreName"] == null ? string.Empty : data.Rows[i]["StoreName"].ToString().TrimEnd();

                        var temp = new ImportExcelStoreBeFoodModel
                        {
                            restaurant_id = excel_BeFoodMerchantID,
                            partner_restaurant_id = excel_StoreNo,
                            restaurant_name = excel_StoreName,
                            UserName = UserName
                        };
                        listStore.Add(temp);
                    }

                    // Validate

                    if (listStore.Count == 0)
                    {
                        return Json(new { Status = 0, Message = $"Không có dữ liệu import. Vui lòng kiểm tra lại file import" });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.restaurant_id)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [BeFoodMerchantID] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.partner_restaurant_id)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [StoreNo] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.restaurant_name)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [StoreName] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    // Existed

                    var checkExisted_BeFoodMerchantID = listStore.GroupBy(x => new { x.restaurant_id }, (key, g) => new
                    {
                        BeFoodMerchantID = key.restaurant_id,
                        ListData = g.ToList()
                    }).Where(x => x.ListData.Count > 1)
                     .Select(x => $"{x.BeFoodMerchantID}"
                   ).ToList();

                    var checkExistedData = listStore.GroupBy(x => new { x.partner_restaurant_id, x.restaurant_name }, (key, g) => new
                    {
                        Code = key.restaurant_name,
                        StoreNo = key.partner_restaurant_id,
                        ListData = g.ToList()
                    }).Where(x => x.ListData.Count > 1)
                      .Select(x => $"{x.StoreNo}-{ x.ListData.FirstOrDefault()?.restaurant_name}"
                    ).ToList();

                    // Check trùng CH trước khi thêm mới

                    if (checkExisted_BeFoodMerchantID.Count > 0)
                    {
                        var errorData = string.Join("|", checkExisted_BeFoodMerchantID).TrimEnd();
                        return Json(new
                        {
                            Status = 2,
                            Message = $"Danh sách mã CH BeFood {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại file import",
                            Data = errorData
                        });
                    }

                    if (checkExistedData.Count > 0)
                    {
                        var errorData = string.Join("|", checkExistedData).TrimEnd();
                        return Json(new
                        {
                            Status = 2,
                            Message = $"Danh sách mã CH Phúc Long {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại file import",
                            Data = errorData
                        });
                    }
                    var insert = _partnerBLO.ImportExcel_MappingStoreBeFood(listStore);
                    return Json(new { Status = insert.Item1, Message = insert.Item2, Data = insert.Item3 });
                }
                catch (Exception ex)
                {
                    return Json(new { Status = 0, Message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = 0, Message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateStatusStoreBeFood(string BeFoodMerchantID, string StoreNo, string Status)
        {
            try
            {
                var UserName = base.LoginUser.UserName;
                var data = _partnerBLO.UpdateStatusStoreBeFood(BeFoodMerchantID, StoreNo, Status, UserName);
                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
                });
            }
        }

        [HttpPost]
        public JsonResult UpdateStoreBeFood(string BeFoodMerchantIDOld, string BeFoodMerchantIDNew, string StoreNo)
        {
            try
            {
                if (string.IsNullOrEmpty(BeFoodMerchantIDNew))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "Vui lòng nhập mã cửa hàng BeFood"
                    });
                }

                var UserName = base.LoginUser.UserName;
                var data = _partnerBLO.UpdateStoreBeFood(BeFoodMerchantIDOld, BeFoodMerchantIDNew, StoreNo, UserName);

                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
                });
            }
        }

        // 11/08/2025,tungnt8: Zalofood
        public ActionResult LoadComboxStoreByZaloFood()
        {
            var data = _partnerBLO.LoadComboxStoreByZaloFood();
            return Json(data);
        }

        [HttpPost]
        [ParentAuthorize("StorePartnerMapping")]
        public JsonResult GetStoreMappingByZaloFood()
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
            var status = Request?.Form["Status"];
            var textSearch = Request?.Form["TextSearch"];
            var data = _partnerBLO.GetStoreMappingByZaloFood(storeNo, status, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ZaloFood_Store_Model> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult UpdateStoreZaloFood(string store_id_old, string store_id_new, string merchant_store_id)
        {
            try
            {
                if (string.IsNullOrEmpty(store_id_new))
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "Vui lòng nhập mã cửa hàng ZaloFood"
                    });
                }

                var userName = base.LoginUser.UserName;
                var data = _partnerBLO.UpdateStoreZaloFood(store_id_old, store_id_new, merchant_store_id, userName);
                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });

            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
                });
            }
        }

        [HttpPost]
        public JsonResult UpdateStatusStoreZaloFood(string store_id, string merchant_store_id, string status)
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var data = _partnerBLO.UpdateStatusStoreZaloFood(store_id, merchant_store_id, status, userName);
                return Json(new ResultResponseModel
                {
                    Status = data.Status,
                    Message = data.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
                });
            }
        }

        [HttpPost]
        [ParentAuthorize("StorePartnerMapping")]
        public ActionResult ImportExcel_MappingStoreZaloFood(HttpPostedFileBase importFile)
        {
            try
            {
                if (importFile == null)
                    return Json(new
                    {
                        Status = 0,
                        Message = "Không có file nào được chọn"
                    });

                string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
                if (fileExtension != ".xls" && fileExtension != ".xlsx")
                {
                    return Json(new
                    {
                        Status = 0,
                        Message = "Định dạng file excel không đúng"
                    });
                }

                var userName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;
                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();

                    // Convert data
                    var listStore = new List<ImportExcelStoreZaloFoodModel>();
                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        var excel_store_id = data.Rows[i]["store_id"] == null ? string.Empty : data.Rows[i]["store_id"].ToString().TrimEnd();
                        var excel_merchant_store_id = data.Rows[i]["merchant_store_id"] == null ? string.Empty : data.Rows[i]["merchant_store_id"].ToString().TrimEnd();
                        var excel_merchant_name = data.Rows[i]["merchant_name"] == null ? string.Empty : data.Rows[i]["merchant_name"].ToString().TrimEnd();

                        var temp = new ImportExcelStoreZaloFoodModel
                        {
                            store_id = excel_store_id,
                            merchant_store_id = excel_merchant_store_id,
                            merchant_name = excel_merchant_name,
                            UserName = userName
                        };
                        listStore.Add(temp);
                    }

                    // Validate
                    if (listStore.Count == 0)
                    {
                        return Json(new { Status = 0, Message = $"Không có dữ liệu import. Vui lòng kiểm tra lại file import" });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.store_id)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [store_id] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.merchant_store_id)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [merchant_store_id] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listStore.Any(x => string.IsNullOrEmpty(x.merchant_name)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [merchant_name] đang bị null/rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    // Existed
                    var checkExisted_store_id = listStore.GroupBy(x => new { x.store_id }, (key, g) => new
                    {
                        store_id = key.store_id,
                        ListData = g.ToList()
                    }).Where(x => x.ListData.Count > 1)
                     .Select(x => $"{x.store_id}"
                   ).ToList();

                    var checkExistedData = listStore.GroupBy(x => new { x.merchant_store_id, x.merchant_name }, (key, g) => new
                    {
                        Code = key.merchant_name,
                        StoreNo = key.merchant_store_id,
                        ListData = g.ToList()
                    }).Where(x => x.ListData.Count > 1)
                      .Select(x => $"{x.StoreNo}-{ x.ListData.FirstOrDefault()?.merchant_name}"
                    ).ToList();

                    // Check trùng CH trước khi thêm mới

                    if (checkExisted_store_id.Count > 0)
                    {
                        var errorData = string.Join("|", checkExisted_store_id).TrimEnd();
                        return Json(new
                        {
                            Status = 2,
                            Message = $"Danh sách mã CH ZaloFood {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại file import",
                            Data = errorData
                        });
                    }

                    if (checkExistedData.Count > 0)
                    {
                        var errorData = string.Join("|", checkExistedData).TrimEnd();
                        return Json(new
                        {
                            Status = 2,
                            Message = $"Danh sách mã CH Phúc Long {errorData} này bị trùng thông tin. Vui lòng kiểm tra lại file import",
                            Data = errorData
                        });
                    }

                    var insert = _partnerBLO.ImportExcel_MappingStoreZaloFood(listStore);
                    return Json(new { Status = insert.Item1, Message = insert.Item2, Data = insert.Item3 });

                }
                catch (Exception ex)
                {
                    return Json(new { Status = 0, Message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = 0, Message = ex.Message });
            }
        }

        //[HttpPost]
        //public JsonResult UpdateStatusStoreBeFood(string BeFoodMerchantID, string StoreNo, string Status)
        //{
        //    try
        //    {
        //        var UserName = base.LoginUser.UserName;
        //        var data = _partnerBLO.UpdateStatusStoreBeFood(BeFoodMerchantID, StoreNo, Status, UserName);
        //        return Json(new ResultResponseModel
        //        {
        //            Status = data.Status,
        //            Message = data.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new ResultResponseModel
        //        {
        //            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
        //            Message = "Đã có lỗi xảy ra!, Vui lòng kiểm tra lại"
        //        });
        //    }
        //}






        // 07/07/2025, tungnt8: new
        //[HttpGet]
        //public ActionResult _ViewItemListBeFood(string StoreNo, string CategoryID, string ItemNo, string Status, string TextSearch)
        //{
        //    var model = new ViewSetupExtraFeeModel(); 
        //    try
        //    {
        //        model = GetItemBeFoodByUpdate(StoreNo, CategoryID, ItemNo, Status, TextSearch);
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    return PartialView(model);
        //}

        //[HttpPost]
        //public async Task<ActionResult> GetItemBeFoodByUpdate(string StoreNo, string CategoryID, string ItemNo, string Status, string TextSearch)
        //{
        //    var data = new List<Get_Item>();
        //    linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI_BEFOOD").Value;
        //    if (string.IsNullOrEmpty(linkAPI))
        //    {
        //        var kq = new ResultResponse
        //        {
        //            Status = HttpStatusCode.BadRequest,
        //            Message = $"Không tìm thấy link API trên hệ thống",
        //            Data = null
        //        };

        //        return Json(kq);
        //    }

        //    if (StoreNo == null || StoreNo == "")
        //    {
        //        StoreNo = "";
        //    }

        //    if (CategoryID == "" || CategoryID == null)
        //    {
        //        CategoryID = "";
        //    }

        //    try
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        //            client.BaseAddress = new Uri(linkAPI);
        //            client.Timeout = TimeSpan.FromMinutes(2);
        //            client.DefaultRequestHeaders.Add("Authorization", BeFood_Basic_Pass);

        //            var path = "";
        //            var result = new ResultResponse();
        //            var ListItemGrab = new List<ViewItemResponseModel>();
        //            var grabfoodCategory = new List<BeFood_Category>();
        //            var listData = new List<ViewItemResponseModel>();

        //            path = $"{BeFood_Get_Item}?Store={StoreNo}";

        //            var response = await client.GetAsync(path);
        //            var resultStr = response.Content.ReadAsStringAsync().Result;

        //            if (response.StatusCode == HttpStatusCode.OK)
        //            {
        //                var listItemGrab = JsonConvert.DeserializeObject<List<Get_Item>>(resultStr);
        //                if (listItemGrab != null)
        //                {
        //                    var model = new Get_Item();
        //                    if (!string.IsNullOrEmpty(CategoryID))
        //                    {
        //                        foreach (var a in listItemGrab)
        //                        {
        //                            if (a.CategoryID == CategoryID)
        //                            {
        //                                model = new Get_Item()
        //                                {
        //                                    Id = a.Id,
        //                                    Name = a.Name,
        //                                    Description = a.Description,
        //                                    Price = a.Price,
        //                                    AvailableStatus = a.AvailableStatus,
        //                                    CategoryID = a.CategoryID,
        //                                    CategoryName = a.CategoryName,
        //                                    Photo = a.Photo,
        //                                    IsTopping = a.IsTopping,
        //                                    IsCampaign = a.IsCampaign,  // = 1 : combo
        //                                    ModifierGroup = a.ModifierGroup.ToList()
        //                                };
        //                                listData.Add(model);
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (!string.IsNullOrEmpty(TextSearch))
        //                        {
        //                            foreach (var a in listItemGrab)
        //                            {
        //                                if (a.Id == ItemNo)
        //                                {
        //                                    model = new Get_Item()
        //                                    {
        //                                        Id = a.Id,
        //                                        Name = a.Name,
        //                                        Description = a.Description,
        //                                        Price = a.Price,
        //                                        AvailableStatus = a.AvailableStatus,
        //                                        CategoryID = a.CategoryID,
        //                                        CategoryName = a.CategoryName,
        //                                        Photo = a.Photo,
        //                                        IsTopping = a.IsTopping,
        //                                        IsCampaign = a.IsCampaign, // = 1 : combo
        //                                        ModifierGroup = a.ModifierGroup.ToList()
        //                                    };
        //                                    listData.Add(model);
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            foreach (var a in listItemGrab)
        //                            {
        //                                model = new Get_Item()
        //                                {
        //                                    Id = a.Id,
        //                                    Name = !string.IsNullOrEmpty(a.Name) ? a.Name : string.Empty,
        //                                    Description = !string.IsNullOrEmpty(a.Description) ? a.Description : string.Empty,
        //                                    Price = a.Price,
        //                                    AvailableStatus = a.AvailableStatus,
        //                                    CategoryID = !string.IsNullOrEmpty(a.CategoryID) ? a.CategoryID : string.Empty,
        //                                    CategoryName = !string.IsNullOrEmpty(a.CategoryName) ? a.CategoryName : string.Empty,
        //                                    Photo = !string.IsNullOrEmpty(a.Photo) ? a.Photo : string.Empty,
        //                                    IsTopping = a.IsTopping,
        //                                    IsCampaign = a.IsCampaign,  // = 1 : combo
        //                                    ModifierGroup = a.ModifierGroup.ToList()
        //                                };
        //                                listData.Add(model);
        //                            }
        //                        }
        //                    }

        //                    ListItemGrab = listData.ToList();
        //                    // filter = Status
        //                    if (!string.IsNullOrEmpty(Status))
        //                    {
        //                        data = ListItemGrab.Where(a => a.AvailableStatus == Status).ToList();
        //                    }
        //                    else
        //                    {
        //                        data = ListItemGrab.ToList();
        //                    }
        //                }
        //                else
        //                {
        //                    result = new ResultResponse
        //                    {
        //                        Status = HttpStatusCode.BadRequest,
        //                        Message = response.ReasonPhrase,
        //                        Data = null
        //                    };
        //                    return Json(result);
        //                }
        //                result = new ResultResponse
        //                {
        //                    Status = HttpStatusCode.OK,
        //                    Message = response.ReasonPhrase,
        //                    Data = data
        //                };
        //                return Json(result);
        //            }
        //            else
        //            {
        //                result = new ResultResponse
        //                {
        //                    Status = HttpStatusCode.BadRequest,
        //                    Message = $"Có lỗi xảy ra",
        //                    Data = null
        //                };
        //                return Json(result);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}



    }
}