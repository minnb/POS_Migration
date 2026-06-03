using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web.Mvc;
using TCX.WebApiCore;
using VCM.POSBLUE.API.Helpers;
using VCM.POSBLUE.Model.Common;

namespace VCM.POSBLUE.API.Controllers
{

    public class HomeController : Controller
    {
        private readonly MemoryCacheService _memoryCacheService;
        public HomeController()
        {
            _memoryCacheService = new MemoryCacheService();
        }
        public ActionResult Index()
        {
            //string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\VCM.POSBLUE.API.dll");
            //string version = VersionHelper.GetDllModificationDate(dllPath);
            string version = _memoryCacheService.GetVersion("bin\\VCM.POSBLUE.API.dll");
            var result = new HealthCheckModel { IPServer = "", DbConnection = false, Version = version };
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var ipServer = addr.LastOrDefault()?.ToString();

                //string neoKey = AESUtils.Encrypt("Merchant=WCM,Store=1563,Cashier=4896,POSID=8963", "Nr0D6F/dUPjvV9b2+NuY4w==");
                result = new HealthCheckModel { IPServer = ipServer, DbConnection = true, Version = version };
            }
            catch (Exception ex)
            {
                result = new HealthCheckModel { IPServer = JsonConvert.SerializeObject(ex), DbConnection = false, Version = version };
            }

            return View(result);
        }
    }
}
