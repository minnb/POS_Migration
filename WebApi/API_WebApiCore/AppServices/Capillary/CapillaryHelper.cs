using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.WebApiCore;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
public static class CapillaryHelper
{
    public static (bool, string) IsValidateSpendPoints(CentralMDRepository centralMDRepository, RedisManager redisManager, VinIDSalesRequest request, bool isStaffBill = false)
    {
        try
        {
            //if (!isStaffBill)
            //{
            //    return (false, $"Yêu cầu scan Hội viên từ app Wincare để sử dụng tính năng tiêu điểm Hội viên nhân viên");
            //}
            var loyaltyRateData = centralMDRepository.GetLoyaltyRateData(redisManager, LoyaltyRateEnum.CAPBLOCK.ToString());
            if (loyaltyRateData != null)
            {
                if (!MathHelper.IsDivisible(request.SpendPoints, (long)loyaltyRateData.Rate))
                {
                    return (false, $"Số điểm sử dụng phải là bội số của {loyaltyRateData.Rate}");
                }
                else if (request.SpendPoints / loyaltyRateData.Rate > request.OrderAmount)
                {
                    return (false, $"Số điểm sử dụng vượt quá số tiền thanh toán. Số điểm có thể sử dụng tối đa: {request.OrderAmount * loyaltyRateData.Rate}");
                }
            }
            return (true, "");
        }
        catch (Exception ex)
        {
            return (true, ex.Message);
        }
    }
    public static (int, long) PointConversionVND(MemoryCacheService memoryCacheService, long pointConversion)
    {
        try
        {
            var loyaltyRateData = memoryCacheService.GetLoyaltyRateData(LoyaltyRateEnum.CAPRATE.ToString());
            if (loyaltyRateData != null)
            {
                long pointToVND = (long)(loyaltyRateData.Rate * pointConversion);
                return (ParseHelper.ToPowerOfTenFromDecimalPlaces(loyaltyRateData.Rate) ,pointToVND);
            }
            return (0,0);
        }
        catch
        {
            return (0, 0);
        }
    }
    public static AddTransactionCapPOSRequest RefundTransactionCapillary(VinIDRefundRequest request)
    {
        decimal totalBill = request.TransLine.Where(x => x.LineAmountIncVAT > 0).Sum(x => x.LineAmountIncVAT);
        decimal vatAmount = request.TransLine.Where(x => x.VatAmount > 0).Sum(x => x.VatAmount);
        AddTransactionCapPOSRequest transactionCapPOSRequest = new AddTransactionCapPOSRequest
        {
            StoreNo = request.MerchantId,
            PosNo = request.TerminalId,
            MemberCard = request.CardNumber,
            TransactionType = request.TransactionType,
            Type = TransactionTypeCapEnum.RETURN.ToString(),
            BillNumber = request.OrigOrderNo,
            BillAmount = totalBill,
            Discount = (int)request.TransLine.Where(x => x.DiscountAmount > 0).Sum(x => x.DiscountAmount),
            BillingDate = request.OrderTime.ToString("yyyy-MM-dd HH:mm:ss"),
        };
        //payment
        List<PaymentModesCapillary> paymentModes = new List<PaymentModesCapillary>();
        foreach (var payment in request.TransPaymentEntry)
        {
            paymentModes.Add(new PaymentModesCapillary()
            {
                Mode = payment.TenderType,
                Value = (int)payment.AmountTendered,
                Attributes = new AttributesCardTypeCapillary()
                {
                    Card_type = payment.CardType,
                }
            });
        }

        //lineItem
        List<LineItemsCapillary> lineItemsV2 = new List<LineItemsCapillary>();
        foreach (var item in request.TransLine)
        {
            lineItemsV2.Add(new LineItemsCapillary()
            {
                Discount = (int)item.DiscountAmount,
                ItemCode = item.ItemCode,
                Description = item.Description,
                Rate = item.UnitPrice,
                Amount = item.LineAmountIncVAT,
                Qty = item.Quantity,
                Serial = item.LineNo,
                ExtendedFields = new LineItemsExtendedFields()
                {
                    Size = item.UnitOfMeasure,
                    Pos_line_no = item.ParentLineNo != null ? item.ParentLineNo.ToString() : item.LineNo.ToString(),
                    Uuid = item.ParentQuantity != null ? item.ParentQuantity.ToString() : item.Quantity.ToString(),
                    Discount_description = item.IsAward == true ? "AWARD" : "NO_AWARD"
                }
            });
        }

        TransactionExtendedFields extendedFields = new TransactionExtendedFields()
        {
            Amount_excluding_tax = totalBill - vatAmount,
            Delivery_date = request.OrderTime.Date,
            Tax_amount = vatAmount,
            Store_associate_id = request.TerminalId,
            Order_channel = ChannelCapEnum.BluePOS.ToString(),
            Order_date_time = request.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            Delivery_charges = 0,
            Cashier_id = request.CashierID
        };

        transactionCapPOSRequest.ExtendedFields = extendedFields;
        transactionCapPOSRequest.LineItemsV2 = lineItemsV2;
        transactionCapPOSRequest.PaymentModes = paymentModes;

        return transactionCapPOSRequest;
    }
    public static AddTransactionCapPOSRequest CreateTransactionCapillary(VinIDSalesRequest request)
    {
        int discountAmount = (int)request.TransLine.Where(x => x.DiscountAmount > 0).Sum(x => x.DiscountAmount);
        decimal vatAmount = request.TransLine.Where(x => x.VatAmount > 0).Sum(x => x.VatAmount);
        decimal totalBill = request.TransLine.Where(x => x.LineAmountIncVAT > 0).Sum(x => x.LineAmountIncVAT);
        AddTransactionCapPOSRequest transactionCapPOSRequest = new AddTransactionCapPOSRequest
        {
            StoreNo = request.MerchantId,
            PosNo = request.TerminalId,
            MemberCard = request.CardNumber,
            TransactionType = 1,
            OrderChannel = request.OrderChannel,
            IsMobile = request.IsMobile,
            Type = TransactionTypeCapEnum.REGULAR.ToString(),
            BillNumber = request.OrderNo,
            BillAmount = totalBill,
            Discount = discountAmount,
            BillingDate = request.OrderTime.ToString("yyyy-MM-dd HH:mm:ss")
        };

        //payment
        List<PaymentModesCapillary> paymentModes = new List<PaymentModesCapillary>();
        foreach (var payment in request.TransPaymentEntry)
        {
            paymentModes.Add(new PaymentModesCapillary()
            {
                Mode = payment.TenderType,
                Value = ParseHelper.IntOrZero(payment.AmountTendered.ToString()),
                Attributes = new AttributesCardTypeCapillary()
                {
                    Card_type = payment.CardType,
                }
            });
        }

        //lineItem
        List<LineItemsCapillary> lineItemsV2 = new List<LineItemsCapillary>();
        foreach (var item in request.TransLine)
        {
            lineItemsV2.Add(new LineItemsCapillary()
            {
                Discount = (int)item.DiscountAmount,
                ItemCode = item.ItemCode,
                Description = item.Description,
                Rate = item.UnitPrice,
                Amount = item.LineAmountIncVAT,
                Qty = item.Quantity,
                Serial = item.LineNo,
                ExtendedFields = new LineItemsExtendedFields()
                {
                    Size = item.UnitOfMeasure,
                    Pos_line_no = item.LineNo.ToString(),
                    Uuid = item.Quantity.ToString(),
                    Lineitem_brand_type = LoyaltyHelper.Get_lineitem_brand_type(item.DivisionCode),
                    Discount_description = item.IsAward == true ? "AWARD" : "NO_AWARD"
                }
            });
        }
        TransactionExtendedFields extendedFields = new TransactionExtendedFields()
        {
            Amount_excluding_tax = totalBill - vatAmount,
            Delivery_date = request.OrderTime.Date,
            Tax_amount = vatAmount,
            Store_associate_id = request.MerchantId,
            Order_channel = ChannelCapEnum.BluePOS.ToString(),
            Order_date_time = request.OrderTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Delivery_charges = 0,
            Cashier_id = request.CashierID
        };
        transactionCapPOSRequest.ExtendedFields = extendedFields;
        transactionCapPOSRequest.LineItemsV2 = lineItemsV2;
        transactionCapPOSRequest.PaymentModes = paymentModes;

        return transactionCapPOSRequest;
    }
    private static int MyRand(int[] arr, int[] freq, int n)
    {
        int[] prefix = new int[n];
        prefix[0] = freq[0];
        for (int i = 1; i < n; ++i)
        {
            prefix[i] = prefix[i - 1] + freq[i];
        }
        Random rand = new Random();
        int r = (rand.Next(prefix[n - 1]) + 1);
        int indexc = FindCeil(prefix, r, 0, n - 1);
        return arr[indexc];
    }
    private static int FindCeil(int[] prefix, int r, int low, int high)
    {
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (r > prefix[mid])
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }
        return (prefix[low] >= r) ? low : -1;
    }
    public static string GetTA()
    {

        int[] arr = new[] { 1, 2, 3 };
        int[] freq = new[] { 1, 1, 1 };
        int n = arr.Length;
        return "TA-" + MyRand(arr, freq, n);
    }
}