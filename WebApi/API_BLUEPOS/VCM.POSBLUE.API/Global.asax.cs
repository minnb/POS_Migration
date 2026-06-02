using Serilog;
using Serilog.Filters;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using System;
using System.Configuration;
using System.Net;
using System.Threading;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TCX.API.Common.Dtos;  
using TCX.API.Common.Dtos.Ops;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Shared;
using VCM.POSBLUE.Data.Redis;

namespace VCM.POSBLUE.API
{
    public partial class WebApiApplication : System.Web.HttpApplication
    {
        private static Timer _opsMonitoringTimer;
        private static readonly int _opsIntervalMinutes = 10;

        protected void Application_Start()
        {
            var logConfig = new LoggerConfiguration()
                    //.WriteTo.File(System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/info/log-.txt"),
                    //              rollingInterval: RollingInterval.Hour,
                    //              rollOnFileSizeLimit: true,
                    //              retainedFileCountLimit: 10240,
                    //              fileSizeLimitBytes: 2024000)
                    .Enrich.With(new IpAddressEnricher())
                    .Enrich.WithMachineName()
                    .Enrich.WithProperty("ResponseTime", 0)
                    .Enrich.WithProperty("WebApi", "")
                    .Enrich.WithProperty("PosNo", "")
                    .Enrich.WithProperty("HttpContext", "")
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(ConfigurationManager.ConnectionStrings["Elasticsearch"].ConnectionString))
                    {
                        AutoRegisterTemplate = true,
                        OverwriteTemplate = true,
                        DetectElasticsearchVersion = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                        IndexFormat = "wcm_pos_log-{0:yyyy-MM-dd-HH}",
                        ModifyConnectionSettings = x => x.BasicAuthentication(WebConfigurationManager.AppSettings["Elasticsearch_USER"], WebConfigurationManager.AppSettings["Elasticsearch_PASS"])
                    })
                    .Filter.ByExcluding(Matching.WithProperty<string>("WebApi", n => n.Contains("api/salary/view-salary")))
                    .Filter.ByExcluding(Matching.WithProperty<string>("WebApi", n => n.Contains("api/common/POSMonitor")))
                    .CreateLogger();

            Log.Logger = logConfig;
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GlobalConfiguration.Configuration.MessageHandlers.Add(new MessageLoggingHandler());
            ServicePointManager.DefaultConnectionLimit = 1000;
            
            AppGlobals.Environment = WebConfigurationManager.AppSettings["Environment"];
            AppGlobals.WebApiSystem = "BLUEPOS";
            AppGlobals.ConnStrCentralMD = ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString;
            AppGlobals.ConnStrLoyalty = ConfigurationManager.ConnectionStrings["DAPPER_LOYALTY"].ConnectionString;
            AppGlobals.RedisActive = WebConfigurationManager.AppSettings["RedisActive"];

            RabbitMQConnection.Initialize(ConfigurationManager.ConnectionStrings["RabbitMQ"].ConnectionString);
            RabbitMQClusterConnection.Initialize(ConfigurationManager.ConnectionStrings["RabbitMQCluster"].ConnectionString, WebConfigurationManager.AppSettings["RabbitMQClusterUser"]);
            RedisSentinelConnection.Initialize(ConfigurationManager.ConnectionStrings["RedisSentinels"].ConnectionString);
            DapperLoyaltyFactory.Initialize(AppGlobals.ConnStrLoyalty);
            DapperCentralMDFactory.Initialize(AppGlobals.ConnStrCentralMD);

            // Initialize OpenTelemetry with modular architecture
            var serviceName = ConfigurationManager.AppSettings["ServiceName"];
            var otlpEndpoint = ConfigurationManager.AppSettings["OtlpEndpoint"];
            var environment = ConfigurationManager.AppSettings["Environment"] ?? "development";

            // Get Redis connection for instrumentation
            ConnectionMultiplexer redisConnection = null;
            try
            {
                redisConnection = RedisConnect.GetConnection();
            }
            catch (Exception)
            {
                // Redis connection failed - continue without Redis instrumentation
            }

            // Initialize OpsMonitoring Timer - chạy mỗi 5 phút
            StartOpsMonitoringTimer();
        }
        protected void Application_End()
        {
            // Dispose OpsMonitoring Timer
            _opsMonitoringTimer?.Dispose();
            // Shutdown Serilog
            Log.CloseAndFlush();
        }

        private static void StartOpsMonitoringTimer()
        {
            var interval = TimeSpan.FromMinutes(_opsIntervalMinutes);
            _opsMonitoringTimer = new Timer(async _ =>
            {
                try
                {
                    MemoryCacheService memoryCacheService = new MemoryCacheService();
                    await OpsMonitoringHelper.OpsMonitoring(memoryCacheService, "WCM.POSBLUE.API");
                }
                catch (Exception ex)
                {
                    TCX.API.Common.Helpers.FileHelper.WriteExpLogs("Global.OpsMonitoringTimer", ex);
                }
            }, null, interval, interval);
        }
    }
}
