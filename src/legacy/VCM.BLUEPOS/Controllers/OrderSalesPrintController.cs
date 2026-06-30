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
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;
using VCM.BLUEPOS.Business.Order;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common.Helpers.Constants;

namespace PLG.Controllers
{
    public class OrderSalesPrintController : BaseController
    {
        private IOrderSalesPrintBLO _orderSalesPrintBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;

        public OrderSalesPrintController(IOrderSalesPrintBLO orderSalesPrintBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _orderSalesPrintBLO = orderSalesPrintBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }
        
        [HttpGet]
        public JsonResult PingIPAddress(string PosID)
        {
            var IPAddress = _commonBLO.GetIPAddessByPOSTerminal(PosID);
            var checkPing = _commonBLO.CheckPingIPLocal(IPAddress);
            return Json(checkPing, JsonRequestBehavior.AllowGet);

        }

        public ActionResult PrintInvoiceOrderSales(string StoreCode, string PosID, string OrderNo)
        {
            PrintInvoiceOrderSalesResponseModel listDataPrint = new PrintInvoiceOrderSalesResponseModel();
            var header = _orderSalesPrintBLO.GetTransHeaderByPrintInvoiceOrderSales(StoreCode, PosID, OrderNo);
            var listItem = _orderSalesPrintBLO.GetTransLineByPrintInvoiceOrderSales(StoreCode, PosID, OrderNo);
            var host = $"{Request.Url.Scheme}://{Request.Url.Authority}";

            listItem = listItem.OrderBy(m => m.LineNo).ToList();
            var listBluePoint = _orderSalesPrintBLO.GetTransBluePointByPrintInvoiceOrderSales(PosID, OrderNo);
            var listCoupon = _orderSalesPrintBLO.GetListCouponByPrintInvoiceOrderSales(PosID, OrderNo);
            var listVoucher = _orderSalesPrintBLO.GetListPromotionByPrintInvoiceOrderSales(PosID, OrderNo);
            var listData = new List<TransLinePrintInvoiceOrderSalesModel>();
            var listCouponData = new List<CouponInforModel>();
            var listVoucherData = new List<PromotionInforModel>();
            var listItemGroup = listItem.GroupBy(m => m.Barcode, (key, g) => new { Key = key, ListData = g.ToList() }).ToList();

            // Xử lý data
            foreach (var item in listItemGroup)
            {
                if (item.Key.Substring(0, 2) == "26") // Hàng cân ký
                {
                    foreach (var temp in item.ListData)
                    {
                        temp.Barcode = !string.IsNullOrEmpty(temp.SerialNo) ? temp.SerialNo : temp.Barcode;
                        temp.MemberPointsEarn = header.FirstOrDefault()?.MemberPointsEarn;
                        listData.Add(temp);

                        // Coupon
                        if (listCoupon.FirstOrDefault()?.OrderNo != null || listCoupon.Count > 0)
                        {
                            var listCouponTemp = listCoupon.Where(m => m.OrderLineNo == temp.LineNo).ToList();
                            var listCouponTempGroup = listCouponTemp.GroupBy(m => m.OfferType, (key, g) =>
                            new CouponInforModel
                            {
                                OrderLineNo = temp.LineNo,
                                CouponCode = key,
                                DiscountAmount = g.Sum(z => z.DiscountAmount),
                                CouponName = g.FirstOrDefault().CouponName,
                                Quantity = g.Sum(z => z.Quantity),
                                DiscountUnitAmount = g.FirstOrDefault().DiscountAmount

                            }).ToList();

                            listCouponData.AddRange(listCouponTempGroup);
                        }

                        // Voucher
                        if (listVoucher != null && listVoucher.Count > 0)
                        {
                            var listVoucherTemp = listVoucher.Where(m => m.OrderLineNo == temp.LineNo).ToList();
                            var listVoucherTempGroup = listVoucherTemp.GroupBy(m => m.VoucherCode, (key, g) =>
                            new PromotionInforModel
                            {
                                OrderLineNo = temp.LineNo,
                                VoucherCode = key,
                                DiscountAmount = g.Sum(z => z.DiscountAmount),
                                VoucherName = g.FirstOrDefault().VoucherName
                            }).ToList();

                            listVoucherData.AddRange(listVoucherTempGroup);

                        }
                    }
                }
                else
                {
                    var temp = item.ListData.FirstOrDefault();
                    temp.Quantity = item.ListData.Sum(m => m.Quantity);
                    temp.DiscountAmount = item.ListData.Sum(m => m.DiscountAmount);
                    temp.MemberPointsEarn = header.FirstOrDefault()?.MemberPointsEarn;
                    temp.LineAmountIncVAT = item.ListData.Sum(m => m.LineAmountIncVAT);

                    listData.Add(temp);
                    var listOrderLineNo = item.ListData.Select(m => m.LineNo).ToList();

                    // Coupon
                    if (listCoupon != null && listCoupon.Count > 0)
                    {
                        var listCouponTemp = listCoupon.Where(m => listOrderLineNo.Contains(m.OrderLineNo)).ToList();
                        var listCouponTempGroup = listCouponTemp.GroupBy(m => m.CouponCode, (key, g) =>
                        new CouponInforModel
                        {
                            OrderLineNo = temp.LineNo,
                            CouponCode = key,
                            DiscountAmount = g.Sum(z => z.DiscountAmount),
                            CouponName = g.FirstOrDefault().CouponName,
                            Quantity = g.Sum(z => z.Quantity),
                            DiscountUnitAmount = g.FirstOrDefault().DiscountAmount
                        }).ToList();

                        listCouponData.AddRange(listCouponTempGroup);
                    }

                    // Voucher
                    if (listVoucher != null && listVoucher.Count > 0)
                    {
                        var listVoucherTemp = listVoucher.Where(m => listOrderLineNo.Contains(m.OrderLineNo)).ToList();
                        var listVoucherTempGroup = listVoucherTemp.GroupBy(m => m.VoucherCode, (key, g) =>
                        new PromotionInforModel
                        {
                            OrderLineNo = temp.LineNo,
                            VoucherCode = key,
                            DiscountAmount = g.Sum(z => z.DiscountAmount),
                            VoucherName = g.FirstOrDefault().VoucherName
                        }).ToList();

                        listVoucherData.AddRange(listVoucherTempGroup);
                    }
                }
            }

            //

            listItem = listData.OrderBy(m => m.LineNo).ToList();
            var listPaymentEntry = _orderSalesPrintBLO.GetListPaymentEntryByPrintInvoiceOrderSales(PosID, OrderNo);
            var totalPoint = header.FirstOrDefault()?.MemberPointsEarn;
            var totalDiscountAmount = listItem.Sum(m => m.DiscountAmount);
            var totalAmount = listItem.Sum(m => m.LineAmountIncVAT);
            var totalPayment = listPaymentEntry.Where(m => m.AmountTendered > 0).Sum(m => m.AmountTendered);

            // Tong tien da giam
            var discountTotal = listItem.Sum(a => a.DiscountAmount);
            if (discountTotal > 0)
            {
                discountTotal = discountTotal * -1;
            }

            //
            var listPOSDataSetup = _orderSalesPrintBLO.GetPOSDataSetup(StoreCode, PosID);

            //
            var cashTenderType = listPOSDataSetup.FirstOrDefault(x => x.Code == "CASHTENDERTYPE")?.Value;
            var listUsedTender = _orderSalesPrintBLO.GetListTenderTypeSetUp(PosID, OrderNo);
            var listPaymentInfor = _orderSalesPrintBLO.GetListTenderTypePayment(PosID, OrderNo);
            var refundCash = listPaymentInfor.Where(x => x.TenderType == cashTenderType && x.AmountTendered < 0).ToList();
            listPaymentInfor.RemoveAll(x => x.AmountTendered < 0);

            var dataGroup = listPaymentInfor.GroupBy(x => x.TenderType, (key, g) => new
            {
                TenderType = key,
                Amount = g.Sum(y => y.AmountTendered),
                ApprovalCode = string.Join(",", g.Select(x => x.ApprovalCode).ToList()).TrimEnd(','),
                CardOrAccount = string.Join(",", g.Select(x => x.CardOrAccount).ToList()).TrimEnd(','),
                ReferenceNo = string.Join(",", g.Select(x => x.ReferenceNo).ToList()).TrimEnd(',')
            }).ToList();

            var refundCashGroup = refundCash.Select(x => new TenderTypePaymentModel
            {
                Code = x.TenderType,
                Amount = x.AmountTendered,
                Description = listUsedTender.FirstOrDefault(y => y.Code == x.TenderType)?.Description,
                ApprovalCode = string.Empty,
                CardOrAccount = string.Empty,
                ReferenceNo = string.Empty
            }).ToList();

            var result = dataGroup
                    .Select(x => new TenderTypePaymentModel
                    {
                        Code = x.TenderType,
                        Description = listUsedTender.FirstOrDefault(y => y.Code == x.TenderType)?.Description,
                        Amount = x.Amount,
                        ApprovalCode = x.ApprovalCode,
                        CardOrAccount = x.CardOrAccount,
                        ReferenceNo = x.ReferenceNo
                    }).ToList();

            result.AddRange(refundCashGroup);
            var ListPaymentInformation = result.ToList();

            //var cashMethod = "PTCS";    // Tiền mặt
            //var refundCashAmountTemp = listPaymentInfor.Where(m => m.Code == cashMethod && m.Amount < 0).Sum(m => m.Amount) ?? 0;
            //listPaymentInfor.RemoveAll(m => m.Code == cashMethod && m.Amount < 0);

            //
            var voucherItem = _orderSalesPrintBLO.GetTransCpnVchIssue(PosID, OrderNo);

            // Diem cho nhan vien
            var campaignPoint = listBluePoint.Where(m => m.DiscountType == GoBlueDiscountType.ZMS1).Sum(m => m.ExtraAmountEarn);

            // KM Masan
            var bluePoint = listBluePoint.Where(m => m.DiscountType == GoBlueDiscountType.ZMS2).Sum(m => m.ExtraAmountEarn);

            // Số tham chiếu : Scan & go
            var referenceNo = OrderNo;
            if (header.FirstOrDefault().TanencyNo == "SNG" || header.FirstOrDefault().TanencyNo == "PNP" || header.FirstOrDefault().TanencyNo == "APY") // SNG - ScanAndGo, PNP - Phano, APY - Aripay
            {
                referenceNo = header.FirstOrDefault().RefKey1;
            }

            //
            var blueReferenceNumber = listBluePoint.FirstOrDefault()?.ReferenceNumber;

            // Maketing 
            // var marketingData = _orderBLO.GetMarketingDataList(PosID, StoreCode);
            // var marketingData = _orderBLO.GetMarketingDataV2(PosID, StoreCode, OrderNo);

            //
            var companyInfor = _orderSalesPrintBLO.GetCompanyInforList(PosID, StoreCode);

            // Biên nhận mua hàng
            var returnVoucherData = _orderSalesPrintBLO.GetReturnVoucherAmountByPrintInvoiceOrderSales(StoreCode, PosID, OrderNo);
            var returnVoucherNo = returnVoucherData.FirstOrDefault()?.ReturnVoucherNo;
            var returnVoucherAmount = returnVoucherData.Sum(m => m.AmountInCurrency);
            var returnVoucherExpireDate = returnVoucherData.FirstOrDefault()?.ReturnVoucherExpire;  // yyyy/MM/dd
            var dateExpireReturnVoucherNo = string.IsNullOrEmpty(returnVoucherExpireDate) ? string.Empty : returnVoucherExpireDate.Split('/')[2] + "/" + returnVoucherExpireDate.Split('/')[1] + "/" + returnVoucherExpireDate.Split('/')[0];   // dd/MM/yyyy

            // Lý do trả hàng
            var returnItemReason = _orderSalesPrintBLO.GetReasonCodeByPrintInvoiceOrderSales(StoreCode, PosID, header.FirstOrDefault()?.RefKey2);

            // Tỷ lệ VinID
            var vinIDRate = _orderSalesPrintBLO.GetVinIDRateByLoyaltyRate(StoreCode, PosID);

            //
            listDataPrint = new PrintInvoiceOrderSalesResponseModel
            {
                StoreNo = header.FirstOrDefault()?.StoreNo,
                StoreName = header.FirstOrDefault()?.StoreName,
                Address = header.FirstOrDefault()?.Address,
                PhoneNo = header.FirstOrDefault()?.PhoneNo,
                POSTerminalNo = header.FirstOrDefault()?.POSTerminalNo,
                OrderNo = header.FirstOrDefault()?.OrderNo,

                OrderTime = header.FirstOrDefault()?.OrderTime,
                CashierID = header.FirstOrDefault()?.CashierID,
                MemberPointsRedeem = header.FirstOrDefault()?.MemberPointsRedeem,
                MemberCardNo = header.FirstOrDefault()?.MemberCardNo,
                IsOfflineVinID = header.FirstOrDefault()?.IsOfflineVinID,
                SalesIsReturn = header.FirstOrDefault()?.SalesIsReturn,

                ListData = listItem.ToList(),
                ListVoucher = listVoucherData.ToList(),
                ListCoupon = listCouponData.ToList(),
                ListBluePoint = listBluePoint.ToList(),
                //ListPaymentInfor = listPaymentInfor.ToList(),
                ListPaymentInfor = ListPaymentInformation.ToList(),
                ListPOSDataSetup = listPOSDataSetup.ToList(),

                TotalAmount = totalAmount,
                TotalDiscountAmount = discountTotal,
                TotalPayment = totalPayment,
                TotalPoint = totalPoint,

                CampaignPoint = campaignPoint,
                BluePoint = bluePoint,
                ReferenceNo = referenceNo,                       // Số tham chiếus
                Ref = blueReferenceNumber,                       // Số tham chiếu ReferenceNumber
                //RefundCashAmount = refundCashAmountTemp,       // Tien mat khach tra lai
                ListCpnVchIssue = voucherItem.ToList(),

                //MarketingList = marketingData,
                CompanyInforList = companyInfor.ToList(),
                Host = host,
                Status = "1",

                ReturnVoucherNo = string.IsNullOrEmpty(returnVoucherNo) ? "0" : returnVoucherNo,          // Biên nhận mua hàng
                ReturnVoucherAmount = returnVoucherAmount,
                ReturnVoucherExpireDate = string.IsNullOrEmpty(returnVoucherExpireDate) ? string.Empty : string.Format("{0: dd/MM/yyyy}", dateExpireReturnVoucherNo),
                Description = returnItemReason.FirstOrDefault()?.Description,      // Lý do trả hàng
                ReturnedOrderNo = header.FirstOrDefault()?.ReturnedOrderNo,        // ID hóa đơn trả hàng
                VinIDRate = vinIDRate.FirstOrDefault()?.Rate
            };

            //return PartialView("~/Views/Order/PrintInvoiceOrderSales.cshtml", listDataPrint);
            return View("~/Views/Order/PrintInvoiceOrderSales.cshtml", listDataPrint);

        }












    }
}