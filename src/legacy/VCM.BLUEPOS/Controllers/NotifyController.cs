using Newtonsoft.Json;
using PLG.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Business.Notify;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.Notify;
using VCM.BLUEPOS.Models;
//using VMC.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class NotifyController : BaseController
    {
        private INotifyBLO _notifyBLO { get; set; }
        public NotifyController(INotifyBLO notifyBLO)
        {
            _notifyBLO = notifyBLO;
        }

        // GET: Notify
        [DisplayName("Quản lý thông báo")]
        public ActionResult Notify()
        {
            return View();
        }

        [HttpPost]
        public ActionResult NotifyLoad()
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

                var title = Request?.Form["Title"];
                var status = Request?.Form["Status"];

                var recordsTotal = 0;
                var result = _notifyBLO.LoadNotify(title, status, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<NotifyModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<NotifyModel>());
            }
        }

        [HttpGet]
        public ActionResult _SetupNotify(string Id)
        {
            var model = new ViewNotifyModel();

            try
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    model = _notifyBLO.ViewInfoNotify(Id);
                }

            }
            catch (Exception ex)
            {

            }

            return PartialView(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveNotify(NotifySaveModel model)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                model.FromDate = string.IsNullOrEmpty(model.FromDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.FromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.ToDate = string.IsNullOrEmpty(model.ToDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.ToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.CreatedDate = DateTime.Now;
                model.CreatedUser = base.LoginUser.UserName;

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

                var save = _notifyBLO.SaveNotify(model);
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

        [HttpPost]
        public ActionResult _Notify()
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.NoContent,
                Message = "",
                Data = null
            };
            try
            {
                var call = _notifyBLO.ViewTop1NotifyClient(base.LoginUser.UserName);
                if (call.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/Notify/_Notify.cshtml", call.Item3),
                        Data = call.Item3
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }


        [HttpPost]
        public JsonResult ConfirmNotify(NotifyConfirmModel model)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                model.CreatedDate = DateTime.Now;
                model.Username = base.LoginUser.UserName;
                model.Status = true;

                var save = _notifyBLO.ConfirmNotify(model);
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

        [HttpPost]
        public ActionResult DeleteNotify(string ID)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var response = _notifyBLO.DeleteNotify(ID);
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
        public ActionResult PreviewNotify(string ID)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.NoContent,
                Message = "",
                Data = null
            };
            try
            {
                var call = _notifyBLO.ViewInfoNotify(ID);
                if (call != null && call.Info != null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        Data = call.Info.Description
                    };
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }
    }
}