using Autofac;
using Autofac.Integration.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Account;
using VCM.BLUEPOS.Infrastructure;
using VCM.BLUEPOS.Invoice;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Models.Account;
using VCM.BLUEPOS.Models.Login;
//using VCM.BLUEPOS.Business.Authen;
//using VCM.BLUEPOS.Business.Account;
using VCM.BLUEPOS.Business;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Business.SetupPromotion;
using VCM.BLUEPOS.Business.Product;
using VCM.BLUEPOS.Business.Price;
using VCM.BLUEPOS.Business.Barcode;
using VCM.BLUEPOS.Business.Campaign;
using VCM.BLUEPOS.Business.Promotion;
using VCM.BLUEPOS.Business.Voucher;
using VCM.BLUEPOS.Business.MasterData;
using VCM.BLUEPOS.Business.Order;
using VCM.BLUEPOS.Business.Report;
using VCM.BLUEPOS.Business.StoreActivities;
using VCM.BLUEPOS.Business.SalesStaging;
using VCM.BLUEPOS.Business.VinID;
using VCM.BLUEPOS.Business.SyncData;
using VCM.BLUEPOS.Business.HotKey;
using VCM.BLUEPOS.Business.SetupPrice;
using VCM.BLUEPOS.Business.SetupItem;
using VCM.BLUEPOS.Business.Loyalty;
using VCM.BLUEPOS.Business.SetupCoupon;
using VCM.BLUEPOS.Business.SetupExtraFee;
using VCM.BLUEPOS.Business.MonitorPOS;
using VCM.BLUEPOS.Business.Notify;
using VCM.BLUEPOS.Business.CrownX;
using VCM.BLUEPOS.Business.LogsData;
using VCM.BLUEPOS.Business.Reward;
using VCM.BLUEPOS.Business.Partner;
using VCM.BLUEPOS.Business.ServiceLog;
using VCM.BLUEPOS.Data.ServiceLog;

namespace VCM.BLUEPOS.Models
{
    public class AutofacConfig
    {
        public static void ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            //
            builder.RegisterType<AccountData>().As<IAccount>().InstancePerLifetimeScope().AsSelf();
            builder.RegisterType<LoginData>().As<ILogin>().InstancePerLifetimeScope().AsSelf();
            builder.RegisterType<ServiceLogData>().As<IServiceLogData>().InstancePerLifetimeScope().AsSelf();

            //
            builder.RegisterType<AccountBLO>().As<IAccountBLO>().InstancePerLifetimeScope();
            builder.RegisterType<AuthenBLO>().As<IAuthenBLO>().InstancePerLifetimeScope();

            builder.RegisterType<StoreBLO>().As<IStoreBLO>().InstancePerLifetimeScope();
            builder.RegisterType<InvoiceBLO>().As<IInvoiceBLO>().InstancePerLifetimeScope();
            builder.RegisterType<ProductBLO>().As<IProductBLO>().InstancePerLifetimeScope();
            builder.RegisterType<PriceBLO>().As<IPriceBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SetupPromotionBLO>().As<ISetupPromotionBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SetupPriceBLO>().As<ISetupPriceBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SetupItemBLO>().As<ISetupItemBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SetupCouponBLO>().As<ISetupCouponBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SetupExtraFeeBLO>().As<ISetupExtraFeeBLO>().InstancePerLifetimeScope();
            builder.RegisterType<BarcodeBLO>().As<IBarcodeBLO>().InstancePerLifetimeScope();
            builder.RegisterType<PromotionBLO>().As<IPromotionBLO>().InstancePerLifetimeScope();
            builder.RegisterType<VoucherBLO>().As<IVoucherBLO>().InstancePerLifetimeScope();
            builder.RegisterType<CommonBLO>().As<ICommonBLO>().InstancePerLifetimeScope();
            builder.RegisterType<MasterDataBLO>().As<IMasterDataBLO>().InstancePerLifetimeScope();
            builder.RegisterType<OrderBLO>().As<IOrderBLO>().InstancePerLifetimeScope();
            builder.RegisterType<OrderSalesPrintBLO>().As<IOrderSalesPrintBLO>().InstancePerLifetimeScope();
            builder.RegisterType<ReturnOrderSalesPrintBLO>().As<IReturnOrderSalesPrintBLO>().InstancePerLifetimeScope();
            builder.RegisterType<ReportBLO>().As<IReportBLO>().InstancePerLifetimeScope();
            builder.RegisterType<StoreActivitiesBLO>().As<IStoreActivitiesBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SalesStagingBLO>().As<ISalesStagingBLO>().InstancePerLifetimeScope();
            builder.RegisterType<VinIDBLO>().As<IVinIDBLO>().InstancePerLifetimeScope();
            builder.RegisterType<SyncDataBLO>().As<ISyncDataBLO>().InstancePerLifetimeScope();
            builder.RegisterType<HotKeyBLO>().As<IHotKeyBLO>().InstancePerLifetimeScope();
            builder.RegisterType<LoyaltyBLO>().As<ILoyaltyBLO>().InstancePerLifetimeScope();
            builder.RegisterType<MonitorPOSBLO>().As<IMonitorPOSBLO>().InstancePerLifetimeScope();
            builder.RegisterType<NotifyBLO>().As<INotifyBLO>().InstancePerLifetimeScope();
            builder.RegisterType<CrownXBLO>().As<ICrownXBLO>().InstancePerLifetimeScope();
            builder.RegisterType<LogsDataBLO>().As<ILogsDataBLO>().InstancePerLifetimeScope();
            builder.RegisterType<RewardBLO>().As<IRewardBLO>().InstancePerLifetimeScope();
            builder.RegisterType<PartnerBLO>().As<IPartnerBLO>().InstancePerLifetimeScope();
            builder.RegisterType<CampaignBLO>().As<ICampaignBLO>().InstancePerLifetimeScope();
            builder.RegisterType<ServiceLogBLO>().As<IServiceLogBLO>().InstancePerLifetimeScope().AsSelf();


            //
            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

        }
    }

}