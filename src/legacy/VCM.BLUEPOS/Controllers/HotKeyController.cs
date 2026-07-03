using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading.Tasks;
using System.Data.SqlClient;
using TichHopSAP;
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.HotKey;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Business.HotKey;
using VCM.BLUEPOS.Business.Common;


namespace PLG.Controllers
{
    public class HotKeyController : BaseController
    {
        private IHotKeyBLO _hotkeyBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;

        public HotKeyController(IHotKeyBLO hotkeyBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _hotkeyBLO = hotkeyBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }
        // GET: HotKey
        public ActionResult HotKeyTable()
        {
            ViewBag.ListProvince = _commonBLO.GetComboxProvinceList();
            return View();
        }
        public ActionResult HotKeyLoad()
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

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long exp = now.ToUnixTimeSeconds();

            var provinceCode = Request?.Form["StoreGroup"];
            var data = _hotkeyBLO.LoadHotKey(provinceCode, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<POSHotKeyItemModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult InsertBarCode(string ProvinceCode, string BarCode)
        {
            try
            {
                var info = _hotkeyBLO.InsertBarCode(ProvinceCode, BarCode);
                if (info != null)
                {
                    return Json(info);
                }
            }
            catch (Exception ex)
            {
            }
            return Json(new Tuple<int, string>(0, "Có lỗi xảy ra"));
        }

        [HttpPost]
        public ActionResult DeleteHotKeyBarcode(string StoreGroup, string Barcode)
        {
            try
            {
                var data = _hotkeyBLO.DeleteHotKeyBarcode(StoreGroup.Trim(), Barcode);
                return Json(data);
            }
            catch(Exception ex)
            {
                return Json(false);
            }
        }






    }
}