using System;
using System.Linq;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.Configuration;
using System.Threading.Tasks;
using VCM.BLUEPOS.Infrastructure;
using VCM.BLUEPOS.Business.MasterData;
using VCM.BLUEPOS.Common.Constants;


[assembly: OwinStartup(typeof(VCM.BLUEPOS.App_Start.Startup))]

namespace VCM.BLUEPOS.App_Start
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            try
            {
                var _adServiceData = ServiceLocator.GetInstance<IMasterDataBLO>();
                var _server = _adServiceData.GetServerInfoPOSDataSetupList();
                var apiLink = _server.FirstOrDefault(x => x.Code == POSDataSetupConstant.LINKAPI).Value;
                //VINIDAPI.SetBaseAddress($"{apiLink}");
            }
            catch (Exception ex)
            {
            }
        }
    }

}