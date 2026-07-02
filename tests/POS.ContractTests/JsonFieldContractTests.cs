using POS.Common;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.POS.Gift;
using POS.Common.Dtos.SetupCoupon;
using POS.Common.Dtos.Voucher;
using POS.Common.Dtos.Vouchers;

namespace POS.ContractTests;

/// <summary>
/// HỢP ĐỒNG JSON — khoá tên field response mà 5.000 máy POS đang parse.
///
/// Mỗi test liệt kê CHÍNH XÁC tập field JSON mong đợi của một DTO trọng yếu.
/// Đổi tên / thêm / xoá field bất kỳ → test đỏ, buộc review có chủ đích trước khi
/// thay đổi lọt ra production. Danh sách kỳ vọng viết theo thứ tự khai báo trong
/// source cho dễ đối chiếu; assertion tự sort 2 phía nên thứ tự không quan trọng.
///
/// Khi CỐ Ý đổi contract: cập nhật danh sách ở đây trong cùng commit — đó là dấu vết
/// cho thấy thay đổi field là có chủ đích, không phải tai nạn.
/// </summary>
public class JsonFieldContractTests
{
    private static void AssertFields(Type type, params string[] expected)
    {
        var actual = JsonContract.EffectiveFieldNames(type);
        var exp = expected.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(exp, actual);
    }

    [Fact]
    public void ResultResponse_envelope_locked()
        => AssertFields(typeof(ResultResponse),
            "Status", "Message", "Data", "MessageTechnical");

    [Fact]
    public void InfoMemberModel_locked()
        => AssertFields(typeof(InfoMemberModel),
            "CardNumber", "VirtualCard", "CMND", "MemberName", "Title", "PhoneNumber",
            "CardLevel", "MemberPoint", "TotalPoint", "RedemptionValue", "CurrentRate",
            "MemberCSN", "OtherInfo", "QRCode", "ExtraPoint", "IsRedeem", "IsOfflineVinID",
            "IsShowMessage", "Status", "System", "ClubCode", "DateOfBirth", "Dob",
            "BirthdayGiftInd", "Gender", "Email", "Address", "ExternalId", "OtherStatus",
            "MemberType", "Source", "ExtendedFields", "AvailablePromotion", "MemberBusiness",
            "PointsSummaries");

    [Fact]
    public void PaymentEntryLoyalty_locked()
        => AssertFields(typeof(PaymentEntryLoyalty),
            "LineNo", "TenderType", "AmountTendered", "CardType");

    [Fact]
    public void GiftDataRespone_locked()
        => AssertFields(typeof(GiftDataRespone),
            "GiftCode", "GiftStatus", "PosUsed", "TimeUsed");

    // ── 8.1/8.2 Setup Coupon (dashboard DTOs) ──────────────────────────────
    [Fact]
    public void CouponListItemDto_locked()
        => AssertFields(typeof(CouponListItemDto),
            "ItemNo", "Description", "Prefix", "LenCode", "IssueType", "CharOfNumber",
            "CharPosition", "StartingDate", "EndingDate", "QtyCoupon", "Status");

    [Fact]
    public void CouponCodeDto_locked()
        => AssertFields(typeof(CouponCodeDto),
            "ItemNo", "Code", "Enable");

    // ── 8.3/8.4 Voucher (dashboard DTOs) ───────────────────────────────────
    [Fact]
    public void VoucherListItemDto_locked()
        => AssertFields(typeof(VoucherListItemDto),
            "ItemNo", "SerialNo", "ItemName", "ArticleType", "UnitOfMeasure", "DiscountType",
            "DiscountValue", "ValueOfVoucher", "MaxAmount", "LimitQty", "IsCheckItem",
            "StartingDate", "EndingDate", "LastDateModified", "Status");

    [Fact]
    public void VoucherCodeDto_locked()
        => AssertFields(typeof(VoucherCodeDto),
            "ItemNo", "Code", "Enable", "Status", "AmountUsed", "OrderUsed");

    [Fact]
    public void VoucherPublishedItemDto_locked()
        => AssertFields(typeof(VoucherPublishedItemDto),
            "StoreNo", "PosNo", "BonusBuy", "SerialNo", "OrderNo", "ArticleNo", "ItemName",
            "ApplyType", "Status", "VoucherValue", "MaxAmount", "MaxQtyUse", "MaxQuantityIssue",
            "TranDateStr", "IsOffline", "IsSend", "IsCheckItem", "VoucherType",
            "FromDateStr", "EnDateStr", "CreatedDateStr");

    // ── SAP Internal Voucher (api/sap/*, 5.000 POS + SAP ERP) ──────────────
    [Fact]
    public void VoucherStatusResponse_locked()
        => AssertFields(typeof(VoucherStatusResponse),
            "Status", "Return", "ActicleNo", "ActicleType", "VoucherNumber", "Value",
            "Voucher_Currency", "Validity_From_Date", "Expiry_Date", "CompanyCode", "Partner",
            "IsEmployee", "PhoneNumber", "VoucherType", "AmountUsed", "OrderUsed");
}
