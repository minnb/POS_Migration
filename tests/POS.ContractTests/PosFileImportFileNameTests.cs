using POS.Common.Helpers;
using POS.Infrastructure.Workers;

namespace POS.ContractTests;

/// <summary>
/// Khóa convention tên file mà PosFileImportService dựa vào: <c>Type_PosNo_TransactionId.txt</c>
/// (đã bỏ .txt trước khi parse), với StoreNo = LEFT(PosNo, 4). Đổi cách parse mà quên cập nhật
/// nguồn sinh file → test đỏ.
/// </summary>
public class PosFileImportFileNameTests
{
    [Theory]
    [InlineData("SALE_120101_1201010001", "SALE", "120101", "1201010001", "1201")]
    [InlineData("REGISTER_888802_ABC", "REGISTER", "888802", "ABC", "8888")]
    // TransactionId chứa '_' → phần thứ 3 gộp toàn bộ phần còn lại
    [InlineData("SALE_120101_2026_07_01_99", "SALE", "120101", "2026_07_01_99", "1201")]
    // PosNo ngắn hơn 4 ký tự → StoreNo = toàn bộ PosNo (Left an toàn)
    [InlineData("SALE_99_TX1", "SALE", "99", "TX1", "99")]
    public void TryParseFileName_ValidNames_ParsesParts(
        string stem, string expType, string expPos, string expTx, string expStore)
    {
        var ok = PosFileImportService.TryParseFileName(stem, out var type, out var posNo, out var txId);

        Assert.True(ok);
        Assert.Equal(expType, type);
        Assert.Equal(expPos, posNo);
        Assert.Equal(expTx, txId);
        Assert.Equal(expStore, StringHelper.Left(posNo, 4));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SALE")]                 // thiếu PosNo + TransactionId
    [InlineData("SALE_120101")]          // thiếu TransactionId
    [InlineData("SALE__1201010001")]     // PosNo rỗng
    [InlineData("SALE_120101_")]         // TransactionId rỗng
    public void TryParseFileName_InvalidNames_ReturnsFalse(string stem)
    {
        var ok = PosFileImportService.TryParseFileName(stem, out _, out _, out _);
        Assert.False(ok);
    }
}
