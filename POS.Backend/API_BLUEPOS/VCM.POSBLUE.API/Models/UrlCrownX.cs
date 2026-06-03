using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VCM.POSBLUE.API.Models
{
    public class UrlCrownX
    {
        public static string CXUrlProfile = "/api/pos/customer/profile";//bỏ
        public static string CXUrlInfo = "/api/pos/customer/info";
        public static string CXUrlInfoV2 = "/api/pos/v2/customer/info";
        public static string CXUrlSale = "/lyt-transactions/api/v1/pos/sale";
        public static string CXUrlRefund = "/lyt-transactions/api/v1/pos/refund";
        public static string CXUrlBlueSale = "/bl/api/pos/sale";
        public static string CXUrlBlueRefund = "/bl/api/pos/refund";

        public static string CXUrlGenerateOTP = "/vc/api/generateOTP/info";
        public static string CXUrlGetVoucher = "/vc/api/getVoucherSerial/info";
        public static string CXUrlUpdateVoucherStatus = "/vc/api/updateVoucherStatus";

        public static string CXWinLife_OTP = "/lp/api/pos/pl/customer/generateOTP";
        public static string CXWinLife_OTPV2 = "/pn/api/pos/pl/customer/generateOTP";
        public static string CXWinLife_Register = "/cs/api/pos/winlife/register";
        public static string CXWinLife_Register_Temp = "/cs/api/pos/winlife/register-temp-customer";

        public static string CXWinLife_UpdatePromotion = "/cs/api/pos/winlife/apply-promotion";
        public static string CXWinLife_WinCodeHistories = "/cs/api/pos/winlife/wincode-histories";

        public static string CXWinLife_SmartPOS_Customer_LastDigits_Phone = "/cs/api/pos/winlife/customer-by-last-digits-phone";
        public static string CXWinLife_SmartPOS_UpdateCustomer = "/cs/api/pos/winlife/update-customer-info";

    }
}