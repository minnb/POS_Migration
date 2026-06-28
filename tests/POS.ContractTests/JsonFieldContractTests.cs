using POS.Common;
using POS.Common.Dtos.Loyalty;
using POS.Common.Dtos.POS.Gift;

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
}
