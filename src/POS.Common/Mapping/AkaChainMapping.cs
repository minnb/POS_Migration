using POS.Common.Dtos.AkaChain;
using POS.Common.Dtos.Capillary.Point;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.PartnerApi;
using POS.Common.Enums;
using POS.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace POS.Common.Mapping
{
    public static class AkaChainMapping
    {
        // ── Mapping (migrated from AkaChainMapping static class) ─────────────────
        public static InfoMemberModel MappingInfoMember(MemberProfileAkaChain profile) => new()
        {
            CardNumber = FormatHelper.PhoneNumberVietNam(profile.Phone ?? ""),
            VirtualCard = profile.ReferralCode,
            CMND = "",
            MemberName = profile.FullName,
            Title = "",
            CardLevel = "",
            MemberCSN = FormatHelper.PhoneNumberVietNam(profile.Phone ?? ""),
            PhoneNumber = FormatHelper.PhoneNumberVietNam(profile.Phone ?? ""),
            OtherInfo = "",
            QRCode = "",
            Dob = "",
            DateOfBirth = "",
            BirthdayGiftInd = false,
            MemberPoint = profile.TotalCoin,
            TotalPoint = profile.TotalCoin,
            RedemptionValue = profile.TotalCoin,
            ExtraPoint = false,
            CurrentRate = 0,
            IsOfflineVinID = false,
            IsShowMessage = false,
            IsRedeem = true,
            Status = "Hoạt động",
            System = MemberCapillaryEnum.CAP.ToString(),
            ClubCode = MemberCapillaryEnum.WINCARE.ToString(),
            Email = "",
            Gender = "",
            Address = "",
            ExternalId = profile.TotalPoint.ToString(),
            AvailablePromotion = null,
            MemberBusiness = null,
            OtherStatus = null,
            ExtendedFields = null,
            MemberType = MemberCapillaryEnum.WIN.ToString(),
            Source = null,
            PointsSummaries = null
        };
        public static PointModePOSResponse MappingAddTransaction(
        AddTransactionAkaChainResponse response, VinIDSalesRequest model, int pointEarn)
        {
            var extraEarn = new List<PointModePOSData>();

            if (response.RewardBalance?.Any() == true)
            {
                foreach (var pair in response.RewardBalance)
                {
                    extraEarn.Add(new PointModePOSData
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
                PointRedeem = (long)response.UsedPoint,
                RedemptionValue = (long)response.MemberBalance,
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
                ExtraEarnByCampaign = extraEarn,
                TransLine = null,
                StatusCode = 200
            };
        }
        public static PointModePOSResponse MappingReturnTransaction(
        object response, VinIDRefundRequest model, int pointEarn)
        {
            return new PointModePOSResponse
            {
                PointEarn = 0,
                PointRedeem = 0,
                RedemptionValue = 0,
                Balance = 0,
                CurrentRate = 0,
                IsOfflineVinID = false,
                EmpCode = null,
                MasanerPackageInd = null,
                StaffPercentage = null,
                NormCustPercentage = null,
                RedemptionId = null,
                ReversalId = null,
                OrderNo = model.OrderNo,
                CreatedId = "",
                ExtraEarnByCampaign = null,
                TransLine = null,
                StatusCode = 200
            };
        }
        public static AddTransactionAkaChainRequest MappingInputDataRequest(
        VinIDSalesRequest model, string activityCode)
        {
            var cartItems = (model.TransLine ?? []).Select(item => new CartItemAkaChain
            {
                ProductCode = item.ItemCode ?? "",
                ProductName = item.Description ?? "",
                ProductCategory = item.Size ?? "",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.LineAmountIncVAT
            }).ToList();

            return new AddTransactionAkaChainRequest
            {
                MemberKeys = new MemberKeysAkaChain
                {
                    Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.CardNumber)
                },
                UsePoint = (int)model.SpendPoints,
                IsSimulation = false,
                SimulationOfferId = null,
                CustomEntityDataId = null,
                ActivityCode = activityCode,
                State = AkaChainStateLoyaltyEnum.Closed.ToString(),
                CouponCode = [],
                ActivityData = new ActivityDataAkaChain
                {
                    BusinessTime = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Bán hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                    TotalCalcAmount = model.BillAmount,
                    OrderCode = model.OrderNo,
                    CartItems = cartItems,
                    StoreCode = model.MerchantId ?? "",
                    OriginalOrderCode = null
                },
                TouchPointCode = null,
                UseBasePromotionSchemes = false
            };
        }

        public static InputReturnDataRequest MappingReturnInputDataRequest(
        VinIDRefundRequest model, string activityCode)
        {
            var cartItems = (model.TransLine ?? []).Select(item => new CartItemAkaChain
            {
                ProductCode = item.ItemCode ?? "",
                ProductName = item.Description ?? "",
                ProductCategory = item.Size ?? "",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.LineAmountIncVAT
            }).ToList();

            return new InputReturnDataRequest
            {
                MemberKeys = new ReturnMemberKeysAkaChain
                {
                    PartnerLoyaltyId = "+" + FormatHelper.PhoneNumberWithCountryCode(model.CardNumber)
                },
                ActivityCode = activityCode,
                State = model.TransactionType == 2
                    ? ReturnState.FullReturn.ToString()
                    : ReturnState.PartialReturn.ToString(),
                ActivityData = new ActivityDataAkaChain
                {
                    BusinessTime = model.OrderTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Trả hàng ngày {model.OrderTime:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
                    TotalCalcAmount = model.RefundAmount,
                    OrderCode = model.OrderNo ?? "",
                    CartItems = cartItems,
                    StoreCode = model.MerchantId ?? "",
                    OriginalOrderCode = model.OrigOrderNo,
                    IsReturn = true
                }
            };
        }

        public static AddTransactionAkaChainRequest MappingCheckCouponRequest(
            CheckVoucherPartnerPOSRequest model, string activityCode)
        {
            decimal totalAmount = 0;
            var cartItems = (model.Items ?? []).Select(item =>
            {
                totalAmount += item.LineAmount;
                return new CartItemAkaChain
                {
                    ProductCode = item.ItemNo ?? "",
                    ProductName = "",
                    ProductCategory = "",
                    Quantity = item.Qty,
                    UnitPrice = item.UnitPrice,
                    TotalAmount = item.LineAmount
                };
            }).ToList();

            var coupons = (model.SerialNo ?? []).Select(c => new CouponDetailAkaChain
            {
                CanUse = true,
                Code = c,
                Description = "",
                Status = "Use"
            }).ToList();

            var now = DateTime.Now;
            return new AddTransactionAkaChainRequest
            {
                MemberKeys = new MemberKeysAkaChain
                {
                    Phone = "+" + FormatHelper.PhoneNumberWithCountryCode(model.PhoneNumber ?? "")
                },
                UsePoint = 0,
                ActivityCode = activityCode,
                State = "Open",
                CouponCode = coupons,
                ActivityData = new ActivityDataAkaChain
                {
                    BusinessTime = now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Description = $"Bán hàng ngày {now:yyyy-MM-ddTHH:mm:ss.fffffffZ}",
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
        public static InputMemberDataAkaChainRequest MappingInputMemberDataAsync(
        RegisterMemberDto model, string enrollmentFormCode)
        {
            return new InputMemberDataAkaChainRequest
            {
                EnrollmentFormCode = enrollmentFormCode,
                MemberData = new MemberDataInputAkaChain
                {
                    FullName = model.FullName,
                    Gender = model.Gender,
                    Address = model.Address,
                    DOB = DateTime.UtcNow.AddYears(-20).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Email = model.Email,
                    Phone = FormatHelper.PhoneNumberVietNam(model.PhoneNo),
                    Status = "Active",
                    TierGroup = "FMVMemberRanking"
                }
            };
        }

        // ── Response helper ───────────────────────────────────────────────────────
    }
}
