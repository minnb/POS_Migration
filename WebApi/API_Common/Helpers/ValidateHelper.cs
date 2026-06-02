using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TCX.API.Common.Dtos.Tax;

namespace TCX.API.Common.Helpers
{
    public static class ValidateHelper
    {
        public static bool TryParseJObject(string json, out JObject obj, out string error)
        {
            obj = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON is null/empty.";
                return false;
            }

            try
            {
                // Quick syntax validation
                var token = JToken.Parse(json);

                if (token.Type != JTokenType.Object)
                {
                    error = $"Expected JSON object but got {token.Type}.";
                    return false;
                }

                obj = (JObject)token;
                return true;
            }
            catch (JsonReaderException ex)
            {
                // Invalid JSON syntax
                error = $"Invalid JSON: {ex.Message} (Line {ex.LineNumber}, Position {ex.LinePosition})";
                return false;
            }
            catch (Exception ex)
            {
                error = $"Failed to parse JSON: {ex.Message}";
                return false;
            }
        }
        public static bool IsValidOrderNo(string orderNo)
        {
            if (string.IsNullOrWhiteSpace(orderNo)) return false;
            return orderNo.All(c =>
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z')
            );
        }
        public static bool IsVoucherSAP(string voucher)
        {
            if(!IsVoucherCapillary(voucher).Item1 && voucher.Length < 19)
            {
                return true;
            }
            return false;
        }
        public static Tuple<bool, string> IsDynamicVoucher(string voucher)
        {
            if (voucher.Length == 25 && StringHelper.Left(voucher, 3) == "WDV")
            {
                return new Tuple<bool, string>(true, "WINX");
            }
            return new Tuple<bool, string>(false, "");
        }
        public static Tuple<bool, string> IsVoucherCapillary(string voucher, string partner = "")
        {
            List<string> prefix = new List<string> { "CAP", "CDC", "CPV" }; 
            try
            {
                if(voucher.Length == 25 && StringHelper.Left(voucher, 3) == "WDV")
                {
                    return new Tuple<bool, string>(true, "WINX");
                }
                if (voucher.Length >= 7 && voucher.Length <= 10 && StringHelper.Uppering(partner) != "PLG")
                {
                    return new Tuple<bool, string>(true, "");
                }
                else if(prefix.Contains(StringHelper.Left(voucher, 3)) && voucher.Length <= 15)
                {
                    return new Tuple<bool, string>(true, "");
                }
                return new Tuple<bool, string>(false, "");
            }
            catch
            {
                return new Tuple<bool, string>(false, "");
            }
        }
        public static Tuple<bool, string> IsValidInvoiceCreated(InvoiceCreatedRequest request)
        {
            return new Tuple<bool, string>(true, "PASSED");
        }
        public static bool IsPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.Length >= 9 && phoneNumber.Length <= 11)
            {
                if (phoneNumber.Substring(0, 1) != "0")
                {
                    return false;
                }

                return true;
            }
            return false;
        }
        public static bool StringContainInArray(string str, string[] arr)
        {
            try
            {
                if(arr.Length == 0) return false;
                foreach(string a in arr) 
                {
                    if(str.Contains(a)) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
