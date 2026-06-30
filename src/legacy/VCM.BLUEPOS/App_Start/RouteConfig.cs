using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace VCM.BLUEPOS
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            //routes.MapRoute(
            //    name: "login",
            //    url: "dang-nhap",
            //    defaults: new { controller = "Authen", action = "Login" }
            //);

            //routes.MapRoute(
            //   name: "logout",
            //   url: "dang-xuat",
            //   defaults: new { controller = "Authen", action = "Logout" }
            //);

            routes.MapRoute(
                name: "Login",
                url: "dang-nhap",
                defaults: new { controller = "Account", action = "Login" }
            );

            routes.MapRoute(
               name: "Logout",
               url: "dang-xuat",
               defaults: new { controller = "Account", action = "Logout" }
            );

            routes.MapRoute(
                name: "RoleGroup",
                url: "nhom-quyen",
                defaults: new { controller = "Role", action = "RoleGroup" }
            );

            routes.MapRoute(
                 name: "PermUser",
                 url: "phan-quyen-user",
                 defaults: new { controller = "Role", action = "PermUser" }
            );

            routes.MapRoute(
                 name: "PermUserPOSWeb",
                 url: "phan-quyen-user-pos-web",
                 defaults: new { controller = "Role", action = "PermUserPOSWeb" }
            );

            routes.MapRoute(
                name: "MngMenu",
                url: "quan-ly-menu",
                defaults: new { controller = "Role", action = "MngMenu" }
            );

            routes.MapRoute(
                name: "MngInvoice",
                url: "quan-ly-hoa-don-dien-tu",
                defaults: new { controller = "Invoice", action = "MngInvoice" }
            );

            routes.MapRoute(
                name: "MngInvoiceTemp",
                url: "quan-ly-hoa-don-dien-tu-temp",
                defaults: new { controller = "Invoice", action = "MngInvoiceTemp" }
            );

            routes.MapRoute(
               name: "ReportInvoice",
               url: "bao-cao-hoa-don-dien-tu",
               defaults: new { controller = "Invoice", action = "ReportInvoice" }
            );

            routes.MapRoute(
               name: "VATNumber",
               url: "dai-hoa-don",
               defaults: new { controller = "Invoice", action = "VATNumber" }
           );

            routes.MapRoute(
               name: "NoseriVAT",
               url: "no-se-ri-vat",
               defaults: new { controller = "Invoice", action = "NoseriVAT" }
            );

            routes.MapRoute(
                name: "IssueInvoice",
                url: "xuat-hoa-don-dien-tu",
                defaults: new { controller = "Invoice", action = "IssueInvoice" }
            );

            routes.MapRoute(
                name: "DummyOrder",
                url: "hoa-don-dummy",
                defaults: new { controller = "Invoice", action = "DummyOrder" }
            );

            // 25/11/2024, tungnt8
            routes.MapRoute(
                name: "DummyOrderMTT",
                url: "hoa-don-dummy-mtt",
                defaults: new { controller = "Invoice", action = "DummyOrderMTT" }
            );

            // 26/11/2024, tungnt8
            routes.MapRoute(
                name: "CustomerInfoByInvoiceMTT",
                url: "khach-hang-hoa-don-mtt",
                defaults: new { controller = "Invoice", action = "CustomerInfoByInvoiceMTT" }
            );

            routes.MapRoute(
                name: "XMLInvoice",
                url: "tao-xml-hoa-don",
                defaults: new { controller = "Invoice", action = "XMLInvoice" }
            );

            routes.MapRoute(
               name: "BranchInvoice",
               url: "danh-sach-chi-nhanh",
               defaults: new { controller = "Invoice", action = "BranchInvoice" }
            );

            routes.MapRoute(
              name: "SignNormal",
              url: "ky-so-normal",
              defaults: new { controller = "Invoice", action = "SignNormal" }
            );

            routes.MapRoute(
              name: "SetupMain",
              url: "cai-dat-chuong-trinh-khuyen-mai",
              defaults: new { controller = "SetupPromotion", action = "SetupMain" }
            );

            routes.MapRoute(
              name: "SetupSpecialComboList",
              url: "cai-dat-special-combo",
              defaults: new { controller = "SetupPromotion", action = "SetupSpecialComboList" }
            );

            routes.MapRoute(
              name: "SetupPrice",
              url: "cai-dat-gia-san-pham",
              defaults: new { controller = "SetupPrice", action = "SetupPrice" }
            );

            //routes.MapRoute(
            //  name: "SetupItem",
            //  url: "san-pham/{page}/{keyword}",
            //  defaults: new { controller = "SetupItem", action = "SetupItem", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
            //);

            // 04/08/2025,tungnt8: dang lam, mapping Level 3
            routes.MapRoute(
              name: "SetupItemV2",
              url: "san-pham/{page}/{keyword}",
              //defaults: new { controller = "SetupItem", action = "SetupItemV2", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
              defaults: new { controller = "SetupItem", action = "SetupItem", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
            );

            routes.MapRoute(
              name: "ItemPartnerList",
              url: "san-pham-doi-tac-on-app/{page}/{keyword}",
              defaults: new { controller = "SetupItem", action = "ItemPartnerList", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
            );

            routes.MapRoute(
              name: "SetupItemPartner",
              url: "san-pham-doi-tac/{page}/{keyword}",
              defaults: new { controller = "SetupItem", action = "SetupItemPartner", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "PosGroupList",
                url: "quan-ly-danh-muc-san-pham",
                defaults: new { controller = "SetupItem", action = "PosGroupList" }
            );

            routes.MapRoute(
              name: "SetupCoupon",
              url: "cai-dat-coupon",
              defaults: new { controller = "SetupCoupon", action = "SetupCoupon" }
            );

            routes.MapRoute(
              name: "CreatedCpnVch",
              url: "tao-ma-coupon-voucher",
              defaults: new { controller = "SetupCoupon", action = "CreatedCpnVch" }
            );

            routes.MapRoute(
              name: "SetupLoyalty",
              url: "khai-bao-ty-le-doi-qua",
              defaults: new { controller = "SetupLoyalty", action = "SetupLoyalty" }
            );

            routes.MapRoute(
              name: "GiftCouponList",
              url: "bao-cao-coupon-da-phat-hanh",
              defaults: new { controller = "SetupLoyalty", action = "GiftCouponList" }
            );


            // --- 22/05/2024 ---

            routes.MapRoute(
              name: "SetupMemberEarnItem",
              url: "khai-bao-ty-le-tich-tem",
              defaults: new { controller = "SetupItem", action = "SetupMemberEarnItem" }
            );

            routes.MapRoute(
                  name: "Error",
                  url: "loi-he-thong",
                  defaults: new { controller = "Authen", action = "Error" }
            );

            routes.MapRoute(
                name: "ProductList",
                url: "danh-muc-san-pham",
                defaults: new { controller = "Product", action = "ProductList" }
            );

            routes.MapRoute(
                name: "PriceList",
                url: "danh-muc-bang-gia",
                defaults: new { controller = "Price", action = "PriceList" }
            );

            routes.MapRoute(
                name: "CreatePriceList",
                url: "khai-bao-gia",
                defaults: new { controller = "Price", action = "CreatePriceList" }
            );

            routes.MapRoute(
                name: "UpdatePriceList",
                url: "cap-nhat-gia",
                defaults: new { controller = "Price", action = "UpdatePriceList" }
            );

            routes.MapRoute(
                name: "CreatePriceGroup",
                url: "tao-price-group",
                defaults: new { controller = "Price", action = "CreatePriceGroup" }
            );

            // 16/03/2026,tungnt8
            routes.MapRoute(
                name: "CreatePriceGroupV2",
                url: "tao-price-group-v2",
                defaults: new { controller = "Price", action = "CreatePriceGroupV2" }
            );

            routes.MapRoute(
                name: "BarcodeList",
                url: "danh-muc-barcode",
                defaults: new { controller = "Barcode", action = "BarcodeList" }
            );

            routes.MapRoute(
                name: "PromotionList",
                url: "danh-muc-khuyen-mai",
                defaults: new { controller = "Promotion", action = "PromotionList" }
            );

            routes.MapRoute(
                name: "CheckPromotionList",
                url: "tra-cuu-khuyen-mai",
                defaults: new { controller = "Promotion", action = "CheckPromotionList" }
            );

            routes.MapRoute(
                name: "CreateArticle",
                url: "tao-article",
                defaults: new { controller = "Product", action = "CreateArticle" }
            );

            routes.MapRoute(
                name: "UpdateArticle",
                url: "cap-nhat-article",
                defaults: new { controller = "Product", action = "UpdateArticle" }
            );

            routes.MapRoute(
                name: "ProductLock",
                url: "product-lock",
                defaults: new { controller = "Product", action = "ProductLock" }
            );

            routes.MapRoute(
                name: "CreateVoucher",
                url: "tao-voucher",
                defaults: new { controller = "Voucher", action = "CreateVoucher" }
            );

            routes.MapRoute(
                name: "UpdateVoucher",
                url: "cap-nhat-voucher",
                defaults: new { controller = "Voucher", action = "UpdateVoucher" }
            );

            routes.MapRoute(
                name: "VoucherCouponList",
                url: "danh-muc-coupon-voucher",
                defaults: new { controller = "Voucher", action = "VoucherCouponList" }
            );


            routes.MapRoute(
                name: "EmployeeList",
                url: "danh-sach-nhan-vien",
                defaults: new { controller = "MasterData", action = "EmployeeList" }
            );

            routes.MapRoute(
                name: "StoreList",
                url: "danh-sach-cua-hang",
                defaults: new { controller = "MasterData", action = "StoreList" }
            );

            routes.MapRoute(
                name: "SetupPOSList",
                url: "khai-bao-may-pos-ban-hang",
                defaults: new { controller = "MasterData", action = "SetupPOSList" }
            );

            routes.MapRoute(
                name: "BankPOSList",
                url: "khai-bao-may-pos-ngan-hang",
                defaults: new { controller = "MasterData", action = "BankPOSList" }
            );

            routes.MapRoute(
                name: "BankList",
                url: "danh-sach-ngan-hang",
                defaults: new { controller = "MasterData", action = "BankList" }
            );

            routes.MapRoute(
                name: "POSVersionList",
                url: "cap-nhat-pos-version",
                defaults: new { controller = "MasterData", action = "POSVersionList" }
            );

            routes.MapRoute(
                name: "SetupSalesOrderType",
                url: "khai-bao-sales-order-type",
                defaults: new { controller = "MasterData", action = "SetupSalesOrderType" }
            );

            routes.MapRoute(
                name: "ChangePassWord",
                url: "thay-doi-mat-khau",
                defaults: new { controller = "MasterData", action = "ChangePassWord" }
            );

            routes.MapRoute(
                name: "SetupImageSlider",
                url: "khai-bao-image-slider",
                defaults: new { controller = "MasterData", action = "SetupImageSlider" }
            );

            routes.MapRoute(
                name: "SetupQRInformation",
                url: "khai-bao-qr-hoa-don",
                defaults: new { controller = "MasterData", action = "SetupQRInformation" }
            );

            routes.MapRoute(
                name: "SetupPrintByPOSWeb",
                url: "cai-dat-may-in-pos-web",
                defaults: new { controller = "MasterData", action = "SetupPrintByPOSWeb" }
            );

            routes.MapRoute(
                name: "VoucherPublished",
                url: "voucher-da-phat-hanh",
                defaults: new { controller = "Voucher", action = "VoucherPublished" }
            );

            routes.MapRoute(
                name: "SyncDataByDate",
                url: "dong-bo-du-lieu-dau-ngay",
                defaults: new { controller = "SyncData", action = "SyncDataByDate" }
            );

            routes.MapRoute(
                name: "SyncDataByTable",
                url: "dong-bo-du-lieu-theo-bang",
                defaults: new { controller = "SyncData", action = "SyncDataByTable" }
            );

            // 06/02/2023 : Ban hang tren POS WEB

            routes.MapRoute(
                name: "SyncDataByPOSWeb",
                url: "dong-bo-du-lieu-pos-web",
                defaults: new { controller = "SyncData", action = "SyncDataByPOSWeb" }
            );

            routes.MapRoute(
                name: "SyncDataBySAP",
                url: "dong-bo-du-lieu-sap",
                defaults: new { controller = "SyncData", action = "SyncDataBySAP" }
            );

            routes.MapRoute(
                name: "QueryScript",
                url: "query-script",
                defaults: new { controller = "SyncData", action = "QueryScript" }
            );

            routes.MapRoute(
                name: "ExcuteScript",
                url: "excute-data-bang-script",
                defaults: new { controller = "SyncData", action = "ExcuteScript" }
            );

            routes.MapRoute(
                name: "SyncFileSalesPosToCentral",
                url: "sync-file-sales-by-pos-to-central",
                defaults: new { controller = "SyncData", action = "SyncFileSalesPosToCentral" }
            );
            
            routes.MapRoute(
                 name: "SyncSalePosToCentral",
                 url: "dong-bo-sale-to-central",
                 defaults: new { controller = "SyncData", action = "SyncSalePosToCentral" }
            );

            // 21/11/2024,tungt8

            routes.MapRoute(
                name: "MissSalePosToCentralReport",
                url: "sync-sale-to-central-by-store-all",
                defaults: new { controller = "SyncData", action = "MissSalePosToCentralReport" }
            );

            routes.MapRoute(
                name: "HotKeyTable",
                url: "bang-chon-cham",
                defaults: new { controller = "HotKey", action = "HotKeyTable" }
            );

            routes.MapRoute(
                name: "OrderList",
                url: "danh-sach-don-hang",
                defaults: new { controller = "Order", action = "OrderList" }
            );

            routes.MapRoute(
                name: "ReportDeleteOrder",
                url: "bao-cao-huy-hang",
                defaults: new { controller = "Report", action = "ReportDeleteOrder" }
            );

            routes.MapRoute(
                name: "DetailedRevenueReport",
                url: "bao-cao-doanh-thu-chi-tiet",
                defaults: new { controller = "Report", action = "DetailedRevenueReport" }
            );

            routes.MapRoute(
                name: "PaymentOrderSalesReport",
                url: "bao-cao-hinh-thuc-thanh-toan",
                defaults: new { controller = "Report", action = "PaymentOrderSalesReport" }
            );

            routes.MapRoute(
                name: "RevenueOrderSalesByStaff",
                url: "bao-cao-doanh-thu-nhan-vien",
                defaults: new { controller = "Report", action = "RevenueOrderSalesByStaff" }
            );

            routes.MapRoute(
                name: "RevenueOrderSalesByStore",
                url: "bao-cao-doanh-thu-theo-cua-hang",
                defaults: new { controller = "Report", action = "RevenueOrderSalesByStore" }
            );

            routes.MapRoute(
                name: "RevenueOrderSalesByMCH",
                url: "bao-cao-doanh-thu-theo-nganh-hang",
                defaults: new { controller = "Report", action = "RevenueOrderSalesByMCH" }
            );

            routes.MapRoute(
                name: "RevenueOrderSalesDetailByMCH",
                url: "bao-cao-doanh-thu-theo-quay-hang",
                defaults: new { controller = "Report", action = "RevenueOrderSalesDetailByMCH" }
            );

            routes.MapRoute(
                name: "VoucherReceiptSalesReport",
                url: "bao-cao-su-dung-bien-nhan-mua-hang-voucher",
                defaults: new { controller = "Report", action = "VoucherReceiptSalesReport" }
            );

            routes.MapRoute(
               name: "PromotionDiscountValueReport",
               url: "bao-cao-tong-hop-giam-gia",
               defaults: new { controller = "Report", action = "PromotionDiscountValueReport" }
            );

            routes.MapRoute(
               name: "DetailPromotionDiscountValueReport",
               url: "bao-cao-chi-tiet-giam-gia",
               defaults: new { controller = "Report", action = "DetailPromotionDiscountValueReport" }
            );

            routes.MapRoute(
               name: "ReportSalesDetailPromotion",
               url: "bao-cao-chi-tiet-khuyen-mai",
               defaults: new { controller = "Report", action = "ReportSalesDetailPromotion" }
           );

            routes.MapRoute(
                name: "ReportShiftEndVM",
                url: "bao-cao-ket-ca-vm",
                defaults: new { controller = "Report", action = "ReportShiftEndVM" }
            );

            routes.MapRoute(
                name: "ReportShiftEndVMP",
                url: "bao-cao-ket-ca-vmp",
                defaults: new { controller = "Report", action = "ReportShiftEndVMP" }
            );

            routes.MapRoute(
                name: "ReportShiftEndPLG",
                url: "bao-cao-ket-ca-plg",
                defaults: new { controller = "Report", action = "ReportShiftEndPLG" }
            );

            routes.MapRoute(
                name: "SalesFailBusDateReport",
                url: "bao-cao-ban-sai-ngay-kinh-doanh",
                defaults: new { controller = "Report", action = "SalesFailBusDateReport" }
            );

            routes.MapRoute(
                name: "KPIStaffReport",
                url: "bao-cao-toc-do-tinh-kpi-nhan-vien",
                defaults: new { controller = "Report", action = "KPIStaffReport" }
            );

            routes.MapRoute(
                name: "SaleOdoo",
                url: "don-hang-odoo",
                defaults: new { controller = "Report", action = "SaleOdoo" }
            );

            routes.MapRoute(
                name: "PromotionOfferTypeByComboReport",
                url: "bao-cao-tong-hop-km-combo",
                defaults: new { controller = "Report", action = "PromotionOfferTypeByComboReport" }
            );

            routes.MapRoute(
                name: "DetailPromotionOfferTypeByComboReport",
                url: "bao-cao-chi-tiet-km-combo",
                defaults: new { controller = "Report", action = "DetailPromotionOfferTypeByComboReport" }
            );

            routes.MapRoute(
                name: "ReportUsedCup",
                url: "bao-cao-su-dung-ly",
                defaults: new { controller = "Report", action = "ReportUsedCup" }
            );
            routes.MapRoute(
               name: "ReportSalesType",
               url: "bao-cao-sales-type",
               defaults: new { controller = "Report", action = "ReportSalesType" }
           );

            routes.MapRoute(
                name: "RevenueOrderSalesByProduct",
                url: "bao-cao-doanh-thu-san-pham",
                defaults: new { controller = "Report", action = "RevenueOrderSalesByProduct" }
            );

            routes.MapRoute(
                name: "GeneralPaymentOrderSalesReport",
                url: "bao-cao-tong-hop-phuong-thuc-thanh-toan",
                defaults: new { controller = "Report", action = "GeneralPaymentOrderSalesReport" }
            );

            routes.MapRoute(
                name: "RevenueSalesReportByHourly",
                url: "bao-cao-doanh-thu-theo-gio",
                defaults: new { controller = "Report", action = "RevenueSalesReportByHourly" }
            );

            routes.MapRoute(
                name: "CumulativeSalesReport",
                url: "bao-cao-cumulative-sales",
                defaults: new { controller = "Report", action = "CumulativeSalesReport" }
            );

            
            // Hoat dong cua hang

            routes.MapRoute(
                name: "ConfirmEndingShiftStores",   // xac nhan ket thuc ca
                url: "xac-nhan-ket-thuc-ca",
                defaults: new { controller = "StoreActivities", action = "ConfirmEndingShiftStores" }
            );

            routes.MapRoute(
                name: "ConfirmEndingDateStores",   // xac nhan ket thuc ngay
                url: "xac-nhan-ket-thuc-ngay",
                defaults: new { controller = "StoreActivities", action = "ConfirmEndingDateStores" }
            );

            routes.MapRoute(
                name: "HistoryShiftEnd",           // lich su ket thuc ca tai POS
                url: "lich-su-ket-ca",
                defaults: new { controller = "StoreActivities", action = "HistoryShiftEnd" }
            );

            routes.MapRoute(
                name: "HistoryDateEndStore",        // lich su ket thuc ngay tai POS
                url: "lich-su-ket-thuc-ngay",
                defaults: new { controller = "StoreActivities", action = "HistoryDateEndStore" }
            );

            routes.MapRoute(
                name: "HistoryDateEnd",             // lich su xac nhan ket thuc ngay
                url: "lich-su-xac-nhan-ket-thuc-ngay",
                defaults: new { controller = "StoreActivities", action = "HistoryDateEnd" }
            );

            routes.MapRoute(
                name: "ChangeBusinessDate",         // thay doi ngay kinh doanh
                url: "thay-doi-ngay-kinh-doanh",
                defaults: new { controller = "StoreActivities", action = "ChangeBusinessDate" }
            );

            routes.MapRoute(
                name: "ITConfirmEndDateStores",     // IT xac nhan ket thuc ngay
                url: "team-support-xac-nhan-ket-thuc-ngay",
                defaults: new { controller = "StoreActivities", action = "ITConfirmEndDateStores" }
            );

            // VinID            
            routes.MapRoute(
                name: "VinIDReport",                // du lieu giao dich vinID tai POS
                url: "giao-dich-vin-id-pos",
                defaults: new { controller = "VinID", action = "VinIDReport" }
            );

            routes.MapRoute(
                name: "TransactionBluePoints",     // giao dich diem blue
                url: "giao-dich-diem-blue",
                defaults: new { controller = "VinID", action = "TransactionBluePoints" }
            );

            routes.MapRoute(
               name: "SetupExtraFee",                   // Giao dich diem blue
               url: "phu-thu",
               defaults: new { controller = "ExtraFee", action = "SetupExtraFee" }
            );

            routes.MapRoute(
               name: "RewardIssue",
               url: "phat-hanh-ma-du-thuong",
               defaults: new { controller = "Reward", action = "RewardIssue" }
            );

            routes.MapRoute(
               name: "MonitorPOS",                      // MonitorPOS
               url: "trang-thai-may-pos",
               defaults: new { controller = "MonitorPOS", action = "MonitorPOS" }
            );

            routes.MapRoute(
               name: "SignalStoreList",                // Danh sách máy POS bat dau ngay
               url: "may-pos-bat-dau-ngay",
               defaults: new { controller = "MonitorPOS", action = "SignalStoreList" }
            );

            routes.MapRoute(
               name: "UpdateVoucherNumberPOS",         // Cập nhật số chứng từ POS
               url: "cap-nhat-so-chung-tu-pos",
               defaults: new { controller = "MasterData", action = "UpdateVoucherNumberPOS" }
            );

            routes.MapRoute(
               name: "ProvinceList",                    // Danh mục tỉnh thành
               url: "danh-muc-tinh-thanh",
               defaults: new { controller = "MasterData", action = "ProvinceList" }
            );

            routes.MapRoute(
               name: "BankCardTypeList",                // Khai báo thẻ ngân hàng
               url: "khai-bao-the-ngan-hang",
               defaults: new { controller = "MasterData", action = "BankCardTypeList" }
            );

            routes.MapRoute(
               name: "PartnerSetup",                  // Khai báo đối tác
               url: "khai-bao-partner-setup",
               defaults: new { controller = "MasterData", action = "PartnerSetup" }
            );

            routes.MapRoute(
               name: "SetupEWalletList",               // Khai báo ví điện tử
               url: "khai-bao-vi-dien-tu",
               defaults: new { controller = "MasterData", action = "SetupEWalletList" }
            );

            routes.MapRoute(
              name: "UserLoginPOSWeb",               // Danh sách login POS WEB
              url: "danh-sach-login-pos-web",
              defaults: new { controller = "MasterData", action = "UserLoginPOSWeb" }
           );

            routes.MapRoute(
               name: "Notify",
               url: "quan-ly-thong-bao",
               defaults: new { controller = "Notify", action = "Notify" }
            );

            /*----- CrownX -----*/

            routes.MapRoute(
               name: "TransactionDataCrownXPOS",
               url: "du-lieu-giao-dich-crownx-pos",
               defaults: new { controller = "CrownX", action = "TransactionDataCrownXPOS" }
            );

            routes.MapRoute(
               name: "TransactionByMemberStamp",
               url: "bao-cao-tong-hop-tich-tieu-tem",
               defaults: new { controller = "CrownX", action = "TransactionByMemberStamp" }
            );

            routes.MapRoute(
               name: "DetailTransactionByMemberEarnStamp",
               url: "bao-cao-chi-tiet-tich-tem",
               defaults: new { controller = "CrownX", action = "DetailTransactionByMemberEarnStamp" }
            );

            routes.MapRoute(
               name: "CheckStaffMemberRemn",
               url: "kiem-tra-han-muc-hoi-vien",
               defaults: new { controller = "CrownX", action = "CheckStaffMemberRemn" }
            );

            routes.MapRoute(
               name: "ReportStaffMemberRemnList",
               url: "bao-cao-hv-msn-su-dung-han-muc",
               defaults: new { controller = "CrownX", action = "ReportStaffMemberRemnList" }
            );

            /*----- LogsData -----*/

            routes.MapRoute(
               name: "EInvoiceAPIMappingErrorLog",
               url: "danh-sach-ma-loi-einvoice",
               defaults: new { controller = "LogsData", action = "EInvoiceAPIMappingErrorLog" }
            );

            routes.MapRoute(
               name: "EInvoicePublishLog",
               url: "danh-sach-log-phat-hanh",
               defaults: new { controller = "LogsData", action = "EInvoicePublishLog" }
            );

            routes.MapRoute(
               name: "EInvoiceErrorsLog",
               url: "danh-sach-log-loi",
               defaults: new { controller = "LogsData", action = "EInvoiceErrorsLog" }
            );

            routes.MapRoute(
               name: "Loyalty_CX_LogAPI",
               url: "log-api-cx",
               defaults: new { controller = "LogsData", action = "Loyalty_CX_LogAPI" }
            );

            routes.MapRoute(
               name: "LogAPIVoucher",
               url: "log-api-voucher",
               defaults: new { controller = "LogsData", action = "LogAPIVoucher" }
            );

            routes.MapRoute(
               name: "LogMissOrderSale",
               url: "log-miss-order-sale",
               defaults: new { controller = "LogsData", action = "LogMissOrderSale" }
            );

            routes.MapRoute(
               name: "LogCloseSalePOS",
               url: "log-ket-thuc-ngay-pos",
               defaults: new { controller = "LogsData", action = "LogCloseSalePOS" }
            );

            routes.MapRoute(
               name: "LogFileSalePOS",
               url: "log-file-sale-pos",
               defaults: new { controller = "LogsData", action = "LogFileSalePOS" }
            );

            routes.MapRoute(
               name: "LogSyncChangeTableByStore",
               url: "danh-sach-bang-dong-bo",
               defaults: new { controller = "LogsData", action = "LogSyncChangeTableByStore" }
            );

            routes.MapRoute(
               name: "CX_ViewCheckMember",
               url: "kiem-tra-thanh-vien-cx",
               defaults: new { controller = "CheckAPI", action = "CX_ViewCheckMember" }
            );

            routes.MapRoute(
               name: "Partner_ViewCheckVoucher",
               url: "kiem-tra-voucher-partner",
               defaults: new { controller = "CheckAPI", action = "Partner_ViewCheckVoucher" }
            );

            routes.MapRoute(
                 name: "PLG_ViewCheckCoupon",
                 url: "kiem-tra-coupon-tren-sap",
                 defaults: new { controller = "CheckAPI", action = "PLG_ViewCheckCoupon" }
            );

            /* --- WinLife --- */

            routes.MapRoute(
                 name: "OrderListWinLife",
                 url: "danh-sach-don-hang-winlife",
                 defaults: new { controller = "Order", action = "OrderListWinLife" }
            );

            routes.MapRoute(
                 name: "DetailRevenueOrderSalesReportWinLife",
                 url: "bao-cao-doanh-thu-chi-tiet-winlife",
                 defaults: new { controller = "Report", action = "DetailRevenueOrderSalesReportWinLife" }
            );

            routes.MapRoute(
                 name: "PaymentOrderSalesReportWinLife",
                 url: "bao-cao-hinh-thuc-thanh-toan-winlife",
                 defaults: new { controller = "Report", action = "PaymentOrderSalesReportWinLife" }
            );

            routes.MapRoute(
                 name: "GetRevenueOrderSalesByStoreWinLife",
                 url: "bao-cao-doanh-thu-cua-hang-winlife",
                 defaults: new { controller = "Report", action = "GetRevenueOrderSalesByStoreWinLife" }
            );

            routes.MapRoute(
                name: "ScanOrder",
                url: "scan-don-hang",
                defaults: new { controller = "Common", action = "ScanOrder" }
            );

            // ---- MENU : NOWFOOD / BEFOOD ----

            routes.MapRoute(
                name: "ItemListNowFood",
                url: "danh-sach-san-pham-now-food",
                defaults: new { controller = "Partner", action = "ItemListNowFood" }
            );

            //routes.MapRoute(
            //    name: "ItemListNowFoodByCreate",
            //    url: "tao-moi-san-pham-now-food/{page}/{keyword}",
            //    defaults: new { controller = "Partner", action = "ItemListNowFoodByCreate", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
            //);

            routes.MapRoute(
                name: "ItemListByMappingPartnerCreate",
                url: "khai-bao-san-pham/{page}/{keyword}",
                defaults: new { controller = "Partner", action = "ItemListByMappingPartnerCreate", page = UrlParameter.Optional, keyword = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "APIItemListNowFoodByStore",
                url: "danh-sach-san-pham-now-food-by-store",
                defaults: new { controller = "Partner", action = "APIItemListNowFoodByStore" }
            );

            //routes.MapRoute(
            //    name: "ItemListNowFoodByUpdate",
            //    url: "cap-nhat-san-pham-now-food",
            //    defaults: new { controller = "Partner", action = "ItemListNowFoodByUpdate" }
            //);

            // 26/02/2025,tungnt8            
            routes.MapRoute(
                name: "UpdateItemByPartner",
                url: "cap-nhat-san-pham-doi-tac",
                defaults: new { controller = "Partner", action = "UpdateItemByPartner" }
            );

            routes.MapRoute(
                name: "ItemListNowFoodByLock",
                url: "mo-khoa-san-pham-now-food",
                defaults: new { controller = "Partner", action = "ItemListNowFoodByLock" }
            );

            routes.MapRoute(
               name: "OrderSalesByNowFood",
               url: "danh-sach-don-hang-now-food",
               defaults: new { controller = "Partner", action = "OrderSalesByNowFood" }
            );

            //routes.MapRoute(
            //  name: "StoreHeadNowFoodByCreate",
            //  url: "khai-bao-cua-hang-head-now-food",
            //  defaults: new { controller = "Partner", action = "StoreHeadNowFoodByCreate" }
            //);

            // 15/04/2025, tungnt8

            routes.MapRoute(
                name: "StorePartnerMapping",
              url: "mapping-cua-hang-doi-tac",
              defaults: new { controller = "Partner", action = "StorePartnerMapping" }
            );

            // 15/04/2025: tạo topping

            routes.MapRoute(
              name: "SetupToppingOptionPartner",
              url: "tao-topping-doi-tac",
              defaults: new { controller = "Partner", action = "SetupToppingOptionPartner" }
            );

            routes.MapRoute(
             name: "ItemListNowFoodByCheckCup",
             url: "kiem-tra-loai-ly",
             defaults: new { controller = "Partner", action = "ItemListNowFoodByCheckCup" }
            );

            routes.MapRoute(
              name: "ListCampaign",
              url: "danh-sach-ctkm",
              defaults: new { controller = "Campaign", action = "ListCampaign" }
            );

            routes.MapRoute(
               name: "Default",
               url: "{controller}/{action}",
               defaults: new { controller = "Home", action = "Index" }
            );

        }
    }
}
