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
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Business.SetupItem;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Model;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;

namespace PLG.Controllers
{

    public class SetupItemController : BaseController
    {
        string urlImgServer = WebConfigurationManager.AppSettings["urlImg"];
        string cdn_Image = WebConfigurationManager.AppSettings["CDN_Image"];

        //------- GrabFood -------

        string GrabFood_Item_Create = WebConfigurationManager.AppSettings["GrabFood_Item_Create"];
        string GrabFood_Item_Option_Create = WebConfigurationManager.AppSettings["GrabFood_Item_Option_Create"];

        string GrabFood_Authorization = WebConfigurationManager.AppSettings["GrabFood_Authorization"];
        string GrabFood_Basic_Pass = WebConfigurationManager.AppSettings["GrabFood_Basic_Pass"];

        private string linkAPI { get; set; }
        private ISetupItemBLO _setupItemBLO { get; set; }
        private IAuthenBLO _authenBLO { get; set; }
        private ICommonBLO _commonBLO;
        private object response;

        public SetupItemController(ISetupItemBLO setupItemBLO, IAuthenBLO authen, ICommonBLO commonBLO)
        {
            _setupItemBLO = setupItemBLO;
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

        [DisplayName("Cài đặt sản phẩm")]
        public ActionResult SetupItem(int page = 1, string keyword = "")
        {
            var model = new ViewSetupItemModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;
                var data = _setupItemBLO.LoadItem(keyword, out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 0 ? (totalRow / pageSize) + 1 : totalRow / pageSize;
                //data = data.Where(a => a.ImageName == (System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "")).ToList();
                data = data.Select(a => new ItemProcedureModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageName = a.ImageName,
                    BarcodeNo = a.BarcodeNo,
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    Total = a.Total
                }).ToList();

                model = new ViewSetupItemModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    Keyword = keyword,
                    IsButtonAdd = role.RoleCode == "R0009" ? false : true
                };
            }
            catch (Exception ex)
            {
                model = new ViewSetupItemModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0,
                    IsButtonAdd = false
                };
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult _SetupItem(string Id)
        {
            var model = new ViewCapNhatSanPhamModel
            {
                Id = Id
            };

            try
            {
                model = _setupItemBLO.ViewInfoItem(Id);
                // [PERF] Dùng URL để hiển thị ảnh thay vì đọc file → base64 nhúng vào HTML
                if (!string.IsNullOrEmpty(model.InfoItem?.ImageName))
                {
                    model.InfoItem.ImageBase64 = $"{urlImgServer}/{model.InfoItem.ImageName}";
                }
            }
            catch (Exception ex)
            {
            }
            return PartialView(model);
        }


        // 04/08/2025,tungnt8: mapping Level 3
        [DisplayName("Cài đặt sản phẩm")]
        public ActionResult SetupItemV2(int page = 1, string keyword = "")
        {
            var model = new ViewSetupItemModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;
                var data = _setupItemBLO.LoadItem(keyword, out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 0 ? (totalRow / pageSize) + 1 : totalRow / pageSize;
                //data = data.Where(a => a.ImageName == (System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "")).ToList();
                data = data.Select(a => new ItemProcedureModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageName = a.ImageName,
                    BarcodeNo = a.BarcodeNo,
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    Total = a.Total
                }).ToList();

                model = new ViewSetupItemModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    Keyword = keyword,
                    IsButtonAdd = role.RoleCode == "R0009" ? false : true
                };
            }
            catch (Exception ex)
            {
                model = new ViewSetupItemModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0,
                    IsButtonAdd = false
                };
            }
            return View(model);
        }

        // 04/08/2025,tungnt8: mapping Level 3

        [HttpGet]
        public ActionResult _SetupItemV2(string Id)
        {
            var model = new ViewCapNhatSanPhamModel
            {
                Id = Id
            };

            try
            {
                model = _setupItemBLO.ViewInfoItem(Id);
                if (!string.IsNullOrEmpty(model.InfoItem.ImageName))
                {
                    var pathImg = $"{cdn_Image}\\{model.InfoItem.ImageName}";
                    byte[] imageArray = System.IO.File.ReadAllBytes(pathImg);
                    model.InfoItem.ImageBase64 = Convert.ToBase64String(imageArray);
                }
            }
            catch (Exception ex)
            {
            }
            return PartialView(model);
        }

        public ActionResult _ViewListItemTopping(string itemHeader)
        {
            var model = new ListItemSetupRequest { ItemNoHeader = itemHeader };
            return PartialView(model);
        }

        [HttpPost]
        public ActionResult ListItemLoad()
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
                var itemHead = Request?.Form["ItemNoHeader"];

                var recordsTotal = 0;
                var result = _setupItemBLO.ListItemSetup(itemHead, itemNo, mota, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<SetupItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<SetupItemModel>());
            }
        }

        [HttpPost]
        public ActionResult LoadItemGroup()
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
                var itemHead = Request?.Form["ItemNoHeader"];
                var groupCode = Request?.Form["GroupCode"];
                var recordsTotal = 0;
                var result = _setupItemBLO.ListItemByGroup(itemHead, itemNo, mota, groupCode, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<SetupItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<SetupItemModel>());
            }
        }
        public ActionResult _ViewListItemCup(string itemHeader)
        {
            var listSalesType = _setupItemBLO.ListSalesType();
            var model = new ListItemSetupRequest { ItemNoHeader = itemHeader, ListSalesType = listSalesType };
            return PartialView(model);
        }
        public ActionResult _ViewListItemOption(string itemHeader)
        {
            var model = new ListItemSetupRequest { ItemNoHeader = itemHeader };
            return PartialView(model);
        }

        [HttpPost]
        public JsonResult SaveItem(string jsonHeader, string jsonBarcode, string jsonTopping, string jsonCup, string jsonOption, string jsonDinhLuong, string jsonPosGroup, string jsonPosGroupWeb)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };

            try
            {
                var modelHeader = JsonConvert.DeserializeObject<List<ItemHeaderRequestModel>>(jsonHeader);
                var modelBarcode = JsonConvert.DeserializeObject<List<BarcodeRequestModel>>(jsonBarcode);
                var modelTopping = JsonConvert.DeserializeObject<List<ToppingRequestModel>>(jsonTopping);
                var modelCup = JsonConvert.DeserializeObject<List<CupRequestModel>>(jsonCup);
                var modelOption = JsonConvert.DeserializeObject<List<OptionRequestModel>>(jsonOption);
                var modelDinhLuong = JsonConvert.DeserializeObject<List<DinhLuongRequestModel>>(jsonDinhLuong);
                var modelPosGroup = JsonConvert.DeserializeObject<List<PosGroupRequestModel>>(jsonPosGroup);
                var modelPosGroupWeb = JsonConvert.DeserializeObject<List<PosGroupRequestModel>>(jsonPosGroupWeb);

                if (modelHeader == null || modelHeader.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Có lỗi xãy ra do không lấy được thông tin sản phẩm"
                    };
                    return Json(result);
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

                // [PERF] Chỉ set ImgName khi user thực sự thay ảnh mới; giữ nguyên tên ảnh cũ nếu không đổi
                modelHeader.FirstOrDefault().ImgName = modelHeader.FirstOrDefault().ImgChanged
                    ? (!string.IsNullOrEmpty(modelHeader.FirstOrDefault().ImgBase64) ? "/Uploads/imgs" : "")
                    : null;  // null = giữ nguyên ImageName hiện tại trong DB

                if (modelCup != null && modelCup.Count > 0)
                {
                    if (modelCup.Count(d => d.IsDefault == true) == 0)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng chọn ly mặc định"
                        };
                        return Json(result);
                    }
                }

                if (modelPosGroup == null || modelPosGroup.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn danh mục sản phẩm"
                    };
                    return Json(result);
                }

                var model = new SetupItemRequestModel
                {
                    itemHeader = modelHeader.FirstOrDefault(),
                    listBarcode = modelBarcode,
                    listTopping = modelTopping,
                    listCup = modelCup,
                    listOption = modelOption,
                    listDinhLuong = modelDinhLuong,
                    listPosGroup = modelPosGroup,
                    listPosGroupWeb = modelPosGroupWeb
                };
               
                var data = _setupItemBLO.SaveItem(model);

                if (data.Item1)
                {
                    var itemData = data.Item3;

                    //var pathFileImg = HostingEnvironment.MapPath($"~/Uploads/imgs/{itemData.itemHeader.ItemNo}.jpg");
                    //if (System.IO.File.Exists(pathFileImg))
                    //{
                    //    System.IO.File.Delete(pathFileImg);
                    //}

                    var pathFileCDN_Img = $"{cdn_Image}\\{itemData.itemHeader.ItemNo}.jpg";

                    // [PERF] Chỉ ghi file khi user thực sự chọn ảnh mới
                    if (itemData.itemHeader.ImgChanged)
                    {
                        if (System.IO.File.Exists(pathFileCDN_Img))
                        {
                            System.IO.File.Delete(pathFileCDN_Img);
                        }

                        if (!string.IsNullOrEmpty(itemData.itemHeader.ImgBase64))
                        {
                            string imageName = itemData.itemHeader.ItemNo + ".jpg";
                            byte[] imageBytes = Convert.FromBase64String(itemData.itemHeader.ImgBase64);
                            string imgCDNPath = Path.Combine(cdn_Image, imageName);
                            System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                        }
                    }

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

        // tungnt8: 14/04/2025, thêm GrabFood

        [HttpPost]
        public JsonResult SaveItemV2(string jsonHeader, string jsonBarcode, string jsonTopping, string jsonCup, string jsonOption, string jsonDinhLuong, string jsonPosGroup, string jsonCategoryGrabFood, string partner)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };

            try
            {
                var modelHeader = JsonConvert.DeserializeObject<List<ItemHeaderRequestModel>>(jsonHeader);
                var modelBarcode = JsonConvert.DeserializeObject<List<BarcodeRequestModel>>(jsonBarcode);
                var modelTopping = JsonConvert.DeserializeObject<List<ToppingRequestModel>>(jsonTopping);
                var modelCup = JsonConvert.DeserializeObject<List<CupRequestModel>>(jsonCup);
                var modelOption = JsonConvert.DeserializeObject<List<OptionRequestModel>>(jsonOption);
                var modelDinhLuong = JsonConvert.DeserializeObject<List<DinhLuongRequestModel>>(jsonDinhLuong);
                var modelPosGroup = JsonConvert.DeserializeObject<List<PosGroupRequestModel>>(jsonPosGroup);
                //var modelToppingGrab = JsonConvert.DeserializeObject<List<ToppingGrabRequestModel>>(jsonToppingGrab);

                if (modelHeader == null || modelHeader.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Có lỗi xãy ra do không lấy được thông tin sản phẩm"
                    };
                    return Json(result);
                }

                var headerItem = modelHeader.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(headerItem.ItemName))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Vui lòng nhập tên sản phẩm"
                    };
                    return Json(result);
                }

                if (string.IsNullOrWhiteSpace(headerItem.UOM))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = "Vui lòng chọn đơn vị tính (UOM)"
                    };
                    return Json(result);
                }

                // [PERF] Chỉ set ImgName khi user thực sự thay ảnh mới
                headerItem.ImgName = headerItem.ImgChanged
                    ? (!string.IsNullOrEmpty(headerItem.ImgBase64) ? "/Uploads/imgs" : "")
                    : null;  // null = giữ nguyên ImageName hiện tại trong DB

                if (modelCup != null && modelCup.Count > 0)
                {
                    if (modelCup.Count(d => d.IsDefault == true) == 0)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng chọn ly mặc định"
                        };
                        return Json(result);
                    }
                }

                if (modelPosGroup == null || modelPosGroup.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn danh mục sản phẩm"
                    };
                    return Json(result);
                }

                var model = new SetupItemRequestModel
                {
                    itemHeader = headerItem,
                    listBarcode = modelBarcode,
                    listTopping = modelTopping,
                    listCup = modelCup,
                    listOption = modelOption,
                    listDinhLuong = modelDinhLuong,
                    listPosGroup = modelPosGroup,
                    listToppingGrab  = null
                };

                if (partner == "GRABFOOD")
                {
                    // B1: vẫn thực hiện lưu thông tin sản phẩm theo Phúc Long cũ

                    // var data = _setupItemBLO.SaveItem(model);

                    var data = _setupItemBLO.SaveItemV2(model);

                    if (data.Item1)
                    {
                        var itemData = data.Item3;
                        // [PERF] Chỉ ghi file khi user thực sự chọn ảnh mới
                        if (itemData.itemHeader.ImgChanged)
                        {
                            var pathFileCDN_Img = $"{cdn_Image}\\{itemData.itemHeader.ItemNo}.jpg";
                            if (System.IO.File.Exists(pathFileCDN_Img))
                            {
                                System.IO.File.Delete(pathFileCDN_Img);
                            }
                            if (!string.IsNullOrEmpty(itemData.itemHeader.ImgBase64))
                            {
                                string imageName = itemData.itemHeader.ItemNo + ".jpg";
                                byte[] imageBytes = Convert.FromBase64String(itemData.itemHeader.ImgBase64);
                                string imgCDNPath = Path.Combine(cdn_Image, imageName);
                                System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                            }
                        }
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

                    // B2: call api thực hiện lưu thông tin theo kênh đối tác Grabfood / Beefood

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

                    // model add category

                    var modelCategoryGrab = JsonConvert.DeserializeObject<List<GrabFood_Item_Create>>(jsonCategoryGrabFood);

                    if (modelCategoryGrab == null)
                    {
                        var kq = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng chọn ngành hàng (Grab/Bee)",
                            Data = null
                        };
                        return Json(kq);
                    }

                    var modelGrabCategory = new GrabFood_Item_Create();
                    foreach (var item in modelCategoryGrab)
                    {
                        modelGrabCategory = new GrabFood_Item_Create
                        {
                            Id = item.Id,
                            Name = item.Name,
                            CategoryID = item.CategoryID,
                            IsDefault = item.IsDefault,
                            UOM = item.UOM,
                            Merchant = partner,
                            User = base.LoginUser.UserName
                        };
                    }
             
                    try
                    {
                        var endPoint = $"{GrabFood_Item_Create}";

                        //var end_point_item_option = $"{GrabFood_Item_Option_Create}";  // api/v1/Grab/Item/options

                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri(linkAPI);
                            client.Timeout = TimeSpan.FromMinutes(2);
                            client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                            if (modelGrabCategory != null)
                            {
                                // Call api category
                                var jsonRequestAPI = JsonConvert.SerializeObject(modelGrabCategory);
                                var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
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
                                    if (response.StatusCode.ToString() == "Conflict")  // san pham da ton tai == 409
                                    {
                                        result = new ResultResponse
                                        {
                                            Status = HttpStatusCode.OK,
                                            Message = "Sản phẩm này đã tồn tại trong hệ thống (Grab/Bee). Vui lòng chọn sản phẩm khác",
                                            Data = string.Empty
                                        };
                                    }
                                    else
                                    {
                                        result = new ResultResponse
                                        {
                                            Status = dataResponse.StatusCode,
                                            Message = dataResponse.Message,
                                            Data = dataResponse.Msg
                                        };
                                    }                                    
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
                else  
                {
                    /*
                     * Truong hop nay không check button Grabfood/Beefood 
                     * Hay sản phẩm này không bán trên kênh Grabfood/Beefood
                     */

                    //var data = _setupItemBLO.SaveItem(model);

                    var data = _setupItemBLO.SaveItemV2(model);

                    if (data.Item1)
                    {
                        var itemData = data.Item3;
                        var pathFileCDN_Img = $"{cdn_Image}\\{itemData.itemHeader.ItemNo}.jpg";

                        if (System.IO.File.Exists(pathFileCDN_Img))
                        {
                            System.IO.File.Delete(pathFileCDN_Img);
                        }

                        if (!string.IsNullOrEmpty(itemData.itemHeader.ImgBase64))
                        {
                            //var pathImg = HostingEnvironment.MapPath("~/Uploads/imgs");
                            //string imgPath = Path.Combine(pathImg, imageName);
                            //System.IO.File.WriteAllBytes(imgPath, imageBytes);

                            string imageName = itemData.itemHeader.ItemNo + ".jpg";
                            byte[] imageBytes = Convert.FromBase64String(itemData.itemHeader.ImgBase64);
                            string imgCDNPath = Path.Combine(cdn_Image, imageName);
                            System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);

                        }
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

        [DisplayName("Cài đặt sản phẩm bán kênh NOWFOOD")]
        public ActionResult ItemPartnerList(int page = 1, string salestype = "", string storeno = "", string mch2 = "", string keyword = "", string status = "")
        {
            var model = new ViewSetupItemPartnerModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;

                var data = _setupItemBLO.GetItemPartnerList(salestype, storeno, mch2, keyword, status, out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

                ViewBag.ComboxNganhHang = _setupItemBLO.LoadCategoryByPartner();
                ViewBag.SaleOrderType = _commonBLO.LoadComboxSalesOrderType();
                ViewBag.ListStore = _commonBLO.GetComboxStoreList();

                data = data.Select(a => new ItemPartnerResponseModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    ImageId = a.ImageId,
                    //ImageName = System.IO.File.Exists($"{cdn_Image}\\{a.ImageName}") ? a.ImageName : "",
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    Size = a.Size,
                    UnitPrice = a.UnitPrice,
                    MCH2 = a.MCH2,
                    MCH2_Name = a.MCH2_Name,
                    StoreNo = a.StoreNo,
                    SaleType = a.SaleType,
                    Blocked = a.Blocked,
                    IsTopping = a.IsTopping,
                    Total = a.Total
                }).ToList();

                model = new ViewSetupItemPartnerModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    SalesType = salestype,
                    StoreNo = storeno,
                    Status = status,
                    Keyword = keyword,
                    MCH2 = mch2        // Ma nganh hang,                  
                    //IsButtonAdd = role.RoleCode == "R0009" ? false : true
                };

            }
            catch (Exception ex)
            {
                model = new ViewSetupItemPartnerModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0
                };
            }
            return View(model);
        }

        [DisplayName("Cài đặt sản phẩm bán kênh đối tác")]
        public ActionResult SetupItemPartner(int page = 1, string keyword = "", string  mch2 = "")
        {
            var model = new ViewSetupItemPartnerModel { };
            try
            {
                var role = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
                var totalRow = 0;
                var pageSize = 120;
                var data = _setupItemBLO.LoadItemPartnerSales(keyword, mch2, "", out totalRow, page, pageSize);
                var totasPage = totalRow % pageSize > 1 ? (totalRow / pageSize) + 1 : totalRow / pageSize;

                ViewBag.ComboxNganhHang = _commonBLO.LoadComboxNganhHang();
                ViewBag.SaleOrderType = _commonBLO.LoadComboxSalesOrderType();

                data = data.Select(a => new ItemPartnerResponseModel
                {
                    ItemNo = a.ItemNo,
                    ItemName = a.ItemName,
                    //ImageId = System.IO.File.Exists($"{cdn_Image}\\{a.ImageId}") ? a.ImageId : "",
                    //BarcodeNo = a.BarcodeNo,
                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                    MCH2 = a.MCH2,
                    MCH2_Name = a.MCH2_Name,
                    Total = a.Total
                }).ToList();

                model = new ViewSetupItemPartnerModel
                {
                    ListItem = data,
                    TotalRow = totalRow,
                    PageSize = pageSize,
                    TotalPageNumber = totasPage,
                    Keyword = keyword,
                    MCH2 = mch2   // Ma nganh hang
                };

            }
            catch (Exception ex)
            {
                model = new ViewSetupItemPartnerModel
                {
                    TotalRow = 0,
                    TotalPageNumber = 0,
                    PageSize = 0
                };
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult SaveItemListForPartner(SetupItemPartnerModel req)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };

            try
            {
                req.CreatedUser = LoginUser.UserName;
                req.CreatedDate = DateTime.Now;
                var listItem = JsonConvert.DeserializeObject<List<ItmeListPartnerModel>>(req.ListItemPartnerStr);

                if (listItem == null || listItem.Count == 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Có lỗi xãy ra do không có thông tin sản phẩm"
                    };
                }

                var model = new SetupItemPartnerModel
                {
                    ListItemPartner = listItem,
                    CreatedDate = req.CreatedDate,
                    CreatedUser = req.CreatedUser,
                    UpdatedDate = req.CreatedDate
                };
                
                var data = _setupItemBLO.SaveItemListForPartner(model);

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

        [HttpGet]
        public ActionResult _SetupItemPartnerDetail(string ItemNo)
        {
            var model = new ViewSetupItemPartnerDetailModel
            {
                ItemNo = ItemNo
            };

            try
            {
                model = _setupItemBLO.ViewInforItemDetailByPartner(ItemNo);
            }
            catch (Exception ex)
            {
            }
            return PartialView(model);
        }

        [HttpPost]
        public JsonResult UpdateItemDetailByPartner(UpdateItemPartnerModel req)
        {
            var data = _setupItemBLO.UpdateItemDetailByPartner(req);
            return Json(data);
        }

        [DisplayName("Quản lý danh mục sản phẩm")]
        public ActionResult PosGroupList()
        {
            ViewBag.PosGroups = _setupItemBLO.GetParentPOSGroup();
            return View();
        }

        [HttpGet]
        public ActionResult _GetListPosGroup(string searchValue)
        {
            var data = _setupItemBLO.GetPOSGroup(searchValue);
            return PartialView(data);
        }

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public JsonResult CreatePosGroup(POSGroupModel req)
        {
            try
            {
               var data = _setupItemBLO.CreatePosGroup(req);
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

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public JsonResult UpdatePosGroup(POSGroupModel req,bool isParent)
        {
            try
            {
               var data = _setupItemBLO.UpdatePosGroup(req, isParent);
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

        [HttpGet]
        [ParentAuthorize("PosGroupList")]
        public JsonResult GetDetailPosGroup(string id)
        {
            try
            {
                var Id = int.Parse(id);
                var data = _setupItemBLO.GetDetailPosGroup(Id);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public ActionResult GetPosGroupItemByGroupCode()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["searchText"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var groupCode = Request?.Form["GroupCode"];
                var recordsTotal = 0;
                var result = _setupItemBLO.GetPosGroupItemByGroupCode(groupCode, searchValue, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<PosGroupItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return Json(new List<SetupItemModel>());
            }
        }

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public ActionResult GetPosGroupItemIsValid()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["searchText"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var groupCode = Request?.Form["GroupCode"];
                var recordsTotal = 0;
                var result = _setupItemBLO.GetPosGroupItemIsValid(groupCode, searchValue, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<PosGroupItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return Json(new List<SetupItemModel>());
            }
        }

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public JsonResult UpdateStatusPosGroup(int id, int status)
        {
            try
            {
               var data = _setupItemBLO.UpdateStatusPosGroup(id, status);
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

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public JsonResult CreatePosGroupItem(PosGroupItemModel req)
        {
            try
            {
                var data = _setupItemBLO.CreatePosGroupItem(req);
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

        [HttpPost]
        [ParentAuthorize("PosGroupList")]
        public JsonResult DeletePosGroupItem(int id)
        {
            try
            {
                var data = _setupItemBLO.DeletePosGroupItem(id);
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

        //****** GrabFood ******
        public ActionResult _ViewListItemGrabFood(string itemHeader)
        {
            var listSalesType = _setupItemBLO.ListSalesType();
            var model = new ListItemSetupRequest { ItemNoHeader = itemHeader, ListSalesType = listSalesType };
            return PartialView(model);
        }

        [HttpGet]
        public ActionResult LoadComboxCategorybyGrabFood()
        {
            var data = _setupItemBLO.LoadComboxCategorybyGrabFood();
            return Json(data);
        }

        [HttpPost]
        public ActionResult CreateItemByGrabFood(string jsonCategoryGrabFood)
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
                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Không tìm thấy link API trên hệ thống",
                    Data = null
                };
                return Json(kq);
            }

            var modelCategoryGrab = JsonConvert.DeserializeObject<List<GrabFood_Item_Create>>(jsonCategoryGrabFood);
            var model = new GrabFood_Item_Create();
            foreach (var item in modelCategoryGrab)
            {
                if (item.CategoryID == null || item.CategoryID == "")
                {
                    var kq = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Vui lòng chọn ngành hàng",
                        Data = null
                    };
                    return Json(kq);
                }

                model = new GrabFood_Item_Create
                {
                    Id = item.Id,
                    Name = item.Name,
                    CategoryID = item.CategoryID,
                    IsDefault = item.IsDefault,
                    UOM = item.UOM,
                    User = base.LoginUser.UserName
                };
            }
           
            var endPoint = $"{GrabFood_Item_Create}";

            try
            {
                using (var client = new HttpClient())
                {
                    var jsonRequestAPI = JsonConvert.SerializeObject(model);
                    client.BaseAddress = new Uri(linkAPI);
                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.DefaultRequestHeaders.Add(GrabFood_Authorization, GrabFood_Basic_Pass);

                    var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        result = new ResultResponse
                        {
                            Status = response.StatusCode,
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
        public ActionResult CheckItemGrabFood(string itemNo)
        {
            var data = _setupItemBLO.CheckItemGrabFood(itemNo);
            return Json(data);
        }
        public ActionResult _ViewListItemToppingGrab(string itemHeader)
        {
            var model = new ListItemSetupRequest { ItemNoHeader = itemHeader };
            return PartialView(model);
        }
    }
}