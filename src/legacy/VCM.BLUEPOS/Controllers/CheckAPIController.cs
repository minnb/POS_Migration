using Newtonsoft.Json;
using PLG.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.CheckAPI;
//using VMC.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class CheckAPIController : BaseController
    {
        string CX_CheckMember = WebConfigurationManager.AppSettings["CX_CheckMember"];
        string Partner_ChecVoucher = WebConfigurationManager.AppSettings["Partner_ChecVoucher"];
        string PLG_CheckCoupon = WebConfigurationManager.AppSettings["PLG_CheckCoupon"];
        string PLGUser = WebConfigurationManager.AppSettings["PLGUser"];
        string PLGPassword = WebConfigurationManager.AppSettings["PLGPassword"];

        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO { get; set; }
        private string linkAPI { get; set; }
        public CheckAPIController(ICommonBLO commonBLO, IAuthenBLO authenBLO)
        {
            _commonBLO = commonBLO;
            _authenBLO = authenBLO;
            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI").Value;
        }

        [DisplayName("Kiểm tra thành viên CX")]
        public ActionResult CX_ViewCheckMember()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CX_Check(string ClubCode, string Keyword)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
                if (string.IsNullOrEmpty(linkAPI))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };
                    return Json(result); ;
                }

                var storeNo = "2001";
                var posID = "200101";
                var endPoint = $"{CX_CheckMember}?numberCard={Keyword}&clubCode={ClubCode}&storeNo={storeNo}&posID={posID}&isLog=false";

                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI);

                        var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var jsonAPI = readData.Result;

                        result.Data = jsonAPI;
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

        [DisplayName("Kiểm tra voucher partner")]
        public ActionResult Partner_ViewCheckVoucher()
        {
            var listStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            return View(listStore);
        }

        [HttpPost]
        public ActionResult Partner_Check(string Partner, string IsVoucher, string IsEmployee, string StoreGiftBox, string ListSerial)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
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
                var domainName = Request.Url.Host;
                var storeNo = "2001";
                var posID = "200101";
                if (domainName != "pos-web.phuclong.com.vn")
                {
                    if (Partner == "GotIt")
                    {
                        storeNo = "136299";
                        posID = "136299";
                    }

                    else if (Partner == "UrBox")
                    {
                        storeNo = "phuclong1";
                        posID = "phuclong1";
                    }
                    else if (Partner == "GiftBox")
                    {
                        storeNo = "pltest";
                        posID = "pltest";
                    }
                }
                else
                {
                    if (Partner == "GotIt")
                    {
                        storeNo = "PL2041";
                        posID = "PL2041";
                    }
                    else if (Partner == "UrBox")
                    {
                        storeNo = "2041";
                        posID = "2041";
                    }
                    else if (Partner == "GiftBox")
                    {
                        storeNo = StoreGiftBox;
                        posID = StoreGiftBox;
                    }
                }
                var isEmployee = IsEmployee == "X" ? true : false;

                var listSerial = ListSerial.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                var listSerialModel = listSerial.Select(a => new SeriNoRequest
                {
                    seriNo = a,
                    isEmployee = isEmployee

                }).ToList();

                var model = new CheckAPIVoucherRequest
                {
                    isWeb = true,
                    storeNo = storeNo,
                    posID = posID,
                    partner = Partner,
                    isVoucher = IsVoucher == "X" ? true : false,
                    listSeriNo = listSerialModel
                };

                var endPoint = $"{Partner_ChecVoucher}";

                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI);

                        var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;

                        var readData = response.Content.ReadAsStringAsync();
                        var jsonAPI = readData.Result;
                        result.Data = jsonAPI;
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

        [DisplayName("Kiểm tra coupon")]
        public ActionResult PLG_ViewCheckCoupon()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Coupon_Check(string ListCoupon)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            try
            {
                if (string.IsNullOrEmpty(linkAPI))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy link API trên hệ thống",
                        Data = null
                    };

                    return Json(result); ;
                }

                var storeNo = "2001";
                var posID = "200101";

                var listSerial = ListCoupon.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                var listSerialModel = listSerial.Select(a => new SeriNoCouponRequest
                {
                    seriNo = a,
                    quantity = 1

                }).ToList();

                var model = new CheckAPICouponRequest
                {
                    storeNo = storeNo,
                    posID = posID,
                    listSeriNo = listSerialModel,
                    userPOS = "WEB",
                    isWeb = true
                };

                var endPoint = $"{PLG_CheckCoupon}";

                using (var client = new HttpClient())
                {
                    try
                    {
                        var jsonRequestAPI = JsonConvert.SerializeObject(model);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI);

                        var byteArray = Encoding.ASCII.GetBytes($"{PLGUser}:{PLGPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.PostAsync(endPoint, new StringContent(jsonRequestAPI, Encoding.UTF8, "application/json")).Result;

                        var readData = response.Content.ReadAsStringAsync();
                        var jsonAPI = readData.Result;
                        result.Data = jsonAPI;
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
    }
}