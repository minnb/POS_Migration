using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.AkaChain;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore.AppServices.FMV
{
    public static class AkaChainMapping
    {
        public static PointModePOSResponse MappingAddTransaction(AddTransactionAkaChainResponse response, VinIDSalesRequest model)
        {
            long pointEarn = 0;
            var extraEarnByCampaign = new List<PointModePOSData>();
            if (response.RewardBalance != null && response.RewardBalance.Any())
            {
                foreach (var pair in response.RewardBalance)
                {
                    pointEarn += (long)pair.Value;
                    extraEarnByCampaign.Add(new PointModePOSData
                    {
                        LoyaltyMerchantId = pair.CurrencyId,
                        Amount = 0,
                        EarnedPoints = (int)pair.Value,
                        EntityType = response.State,
                        Type = pair.CurrencyName
                    });
                }
            }

            return new PointModePOSResponse
            {
                PointEarn = pointEarn,
                PointRedeem = (long) response.UsedPoint,
                RedemptionValue = (long) response.MemberBalance,
                Balance = response.MemberBalance > 0 ? (long?)response.MemberBalance : null,
                CurrentRate = 0,
                IsOfflineVinID = false,
                EmpCode = null,
                MasanerPackageInd = null,
                StaffPercentage = null,
                NormCustPercentage = null,
                RedemptionId = null,
                ReversalId = null,
                OrderNo = model.OrderNo,
                CreatedId = response.ActivityEntityValueId,
                ExtraEarnByCampaign = extraEarnByCampaign,
                TransLine = null,
                StatusCode = 200
            };
        }
        public static AddTransactionAkaChainRequest MappingInputDataRequest(VinIDSalesRequest model, string activityCode)
        {
            List<CartItemAkaChain> cartItems = new List<CartItemAkaChain>();
            foreach (var item in model.TransLine)
            {
                cartItems.Add(new CartItemAkaChain
                {
                    ProductCode = item.ItemCode,
                    ProductName = item.Description,
                    ProductCategory = item.Size,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = item.LineAmountIncVAT
                });
            }
            return new AddTransactionAkaChainRequest
            {
                MemberKeys = new MemberKeysAkaChain
                {
                    Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.CardNumber)
                },
                UsePoint = 0,
                IsSimulation = false,
                SimulationOfferId = null,
                CustomEntityDataId = null,
                ActivityCode = activityCode,
                State = "Open",
                CouponCode = new List<CouponDetailAkaChain>(),
                ActivityData = new ActivityDataAkaChain
                {
                    BusinessTime = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Bán hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                    TotalCalcAmount = model.BillAmount,
                    OrderCode = model.OrderNo,
                    CartItems = cartItems,
                    StoreCode = model.MerchantId,
                    OriginalOrderCode = null
                },
                TouchPointCode = null,
                UseBasePromotionSchemes = false
            };
        }
        public static InputReturnDataRequest MappingReturnInputDataRequest(VinIDRefundRequest model, string activityCode)
        {
            List<CartItemAkaChain> cartItems = new List<CartItemAkaChain>();
            foreach (var item in model.TransLine)
            {
                cartItems.Add(new CartItemAkaChain
                {
                    ProductCode = item.ItemCode,
                    ProductName = item.Description,
                    ProductCategory = item.Size,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = item.LineAmountIncVAT
                });
            }
            return new InputReturnDataRequest
            {
                MemberKeys = new ReturnMemberKeysAkaChain
                {
                    PartnerLoyaltyId = FormatHelper.PhoneNumberVietNam(model.CardNumber)
                },
                ActivityCode = activityCode,
                State = model.TransactionType == 2 ? ReturnState.FullReturn.ToString() : ReturnState.PartialReturn.ToString(),
                ActivityData = new ActivityDataAkaChain
                {
                    BusinessTime = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Trả hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                    TotalCalcAmount = model.RefundAmount,
                    OrderCode = model.OrderNo,
                    CartItems = cartItems,
                    StoreCode = model.MerchantId,
                    OriginalOrderCode = model.OrigOrderNo,
                    IsReturn = true
                }
            };
        }
        public static AddTransactionAkaChainRequest MappingCheckCouponRequest(CheckVoucherPartnerPOSRequest model, string activityCode)
        {
            List<CartItemAkaChain> cartItems = new List<CartItemAkaChain>();
            decimal totalAmount = 0;
            foreach (var item in model.Items)
            {
                totalAmount += item.LineAmount;
                cartItems.Add(new CartItemAkaChain
                {
                    ProductCode = item.ItemNo,
                    ProductName = "",
                    ProductCategory = "",
                    Quantity = item.Qty,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = item.LineAmount
                });
            }
            List<CouponDetailAkaChain> couponDetailAkaChains = new List<CouponDetailAkaChain>();
            if (model.SerialNo.Count > 0) 
            {
                foreach(var coupon in model.SerialNo) 
                {
                    couponDetailAkaChains.Add(new CouponDetailAkaChain()
                    {
                        CanUse = true,
                        Code = coupon,
                        Description = "",
                        Status = "Use",
                    });
                }
            }
            var time = DateTime.Now;
            return new AddTransactionAkaChainRequest
            {
                MemberKeys = new MemberKeysAkaChain
                {
                    Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.PhoneNumber)
                },
                UsePoint = 0,               
                ActivityCode = activityCode,
                State = "Open",
                CouponCode = couponDetailAkaChains,
                ActivityData = new ActivityDataAkaChain
                {
                    BusinessTime = time.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Bán hàng ngày {time:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                    TotalCalcAmount = totalAmount,
                    OrderCode = Guid.NewGuid().ToString(),
                    CartItems = cartItems,
                    StoreCode = model.StoreNo,
                    OriginalOrderCode = null
                },
                TouchPointCode = null,
                UseBasePromotionSchemes = false
            };
        }
        public static InfoMemberModel MappingInfoMember(MemberProfileAkaChain memberProfile)
        {
            return new InfoMemberModel
            {
                CardNumber = FormatHelper.PhoneNumberVietNam(memberProfile.Phone),
                VirtualCard = memberProfile.ReferralCode,
                CMND = "",
                MemberName = memberProfile.FullName,
                Title = "",
                CardLevel = "",
                MemberCSN = FormatHelper.PhoneNumberVietNam(memberProfile.Phone),
                PhoneNumber = FormatHelper.PhoneNumberVietNam(memberProfile.Phone),
                OtherInfo = "",
                QRCode = "",
                Dob = "",
                DateOfBirth = "",
                BirthdayGiftInd = false,//Quà tặng sinh nhật
                MemberPoint = memberProfile.TotalPoint,
                TotalPoint = memberProfile.TotalPoint,
                RedemptionValue = memberProfile.TotalPoint,
                ExtraPoint = false,
                CurrentRate = 0,
                IsOfflineVinID = false,
                IsShowMessage = false,
                IsRedeem = true,
                Status = "Hoạt động",
                System = MemberCapillaryEnum.FMV.ToString(),//KM HV WIN
                ClubCode = MemberCapillaryEnum.FMV.ToString(),//KM HV WIN
                Email = "",
                Gender = "",
                Address = "",
                ExternalId = "",
                AvailablePromotion = null,
                MemberBusiness = null,
                OtherStatus = null,
                ExtendedFields = null,
                MemberType = MemberCapillaryEnum.FMV.ToString(),
                Source = null,
                PointsSummaries = null,
            };
        }
    }
}