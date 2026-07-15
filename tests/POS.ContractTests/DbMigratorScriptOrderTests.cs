using POS.DbMigrator;

namespace POS.ContractTests;

/// <summary>
/// GUARDRAIL — bảo vệ bản vá "DbUp tự sắp xếp lại Track A theo alphabet tên SqlScript, bất kể thứ
/// tự đưa vào qua .WithScripts()" (xem docs/CHANGELOG.md mục "[2026-07-13] Fix
/// `usp_SetupSalePrice_Save` ... + fix DbUp sort-order", và comment tại
/// tools/POS.DbMigrator/Program.cs — ApplyTrackA).
///
/// Trước bản vá: <c>new SqlScript(e.File, ...)</c> — DbUp resort theo tên file thô, alphabet-sort
/// KHÔNG tương quan gì với <c>order</c> số trong manifest.json → script chạy sai thứ tự dù C# đã
/// OrderBy(Order) đúng, gây sự cố production thật (ghi đè mất fix mới mỗi lần --apply).
///
/// Sau bản vá: <see cref="ManifestScriptProvider.BuildScriptName"/> zero-pad <c>Order</c> (D6) vào
/// đầu tên để alphabet-sort của DbUp trùng khớp thứ tự <c>order</c>. Test này KHÔNG gọi DbUp thật
/// (không cần DB) — giả lập đúng hành vi resort của DbUp bằng
/// <c>OrderBy(name, StringComparer.Ordinal)</c> trên danh sách tên đã build, rồi so với thứ tự
/// mong đợi theo <c>Order</c>. Nếu sau này ai vô tình bỏ zero-pad (quay lại dùng tên file thô) hoặc
/// đổi logic build tên, test dưới đây phải ĐỎ ngay — không chờ tới khi sự cố tái diễn trên
/// production.
/// </summary>
public class DbMigratorScriptOrderTests
{
    private static string SqlDirectory => ManifestScriptProvider.ResolveSqlDirectory(AppContext.BaseDirectory, sqlDirOverride: null);

    /// <summary>Giả lập chính xác hành vi DbUp: DeployChanges.To...WithScripts(scripts) resort scripts theo Name, Ordinal.</summary>
    private static List<ManifestEntry> SimulateDbUpExecutionOrder(List<ManifestEntry> entries) =>
        entries.OrderBy(ManifestScriptProvider.BuildScriptName, StringComparer.Ordinal).ToList();

    [Fact]
    public void BuildScriptName_TenFileNguocAlphabetSoVoiOrder_VanThucThiDungThuTuOrder()
    {
        // Tên file cố tình đặt NGƯỢC alphabet so với order (kiểu bug thật đã gặp: SetupPromotion_*
        // vs SetupGroupItem_* vs Voucher_* không có số thứ tự nào trong tên) — nếu BuildScriptName
        // không zero-pad Order, DbUp sẽ resort ra "Aaa < Mmm < Zzz", tức thứ tự SAI so với Order.
        var entries = new List<ManifestEntry>
        {
            new() { Order = 100, File = "Zzz_ChayDauTien.sql", Target = "CentralMD", RunOnce = false },
            new() { Order = 200, File = "Mmm_ChayThuHai.sql", Target = "CentralMD", RunOnce = false },
            new() { Order = 300, File = "Aaa_ChayCuoiCung.sql", Target = "CentralMD", RunOnce = false },
        };

        var executionOrder = SimulateDbUpExecutionOrder(entries);

        Assert.Equal(
            ["Zzz_ChayDauTien.sql", "Mmm_ChayThuHai.sql", "Aaa_ChayCuoiCung.sql"],
            executionOrder.Select(e => e.File));
    }

    [Fact]
    public void BuildScriptName_KhongZeroPad_SeThucThiSaiThuTu_ChungMinhVaiTroCuaBanVa()
    {
        // Test "phản chứng" — dựng lại CHÍNH XÁC hành vi CŨ (trước bản vá, Name = e.File nguyên
        // văn) để chứng minh nếu thiếu zero-pad thì DbUp resort SAI thứ tự Order. Test này PHẢI
        // luôn xanh (nó test hành vi cũ, không phải code hiện tại) — mục đích là tài liệu sống
        // chứng minh bản vá BuildScriptName ở trên thực sự cần thiết, không phải phòng thủ thừa.
        var entries = new List<ManifestEntry>
        {
            new() { Order = 100, File = "Zzz_ChayDauTien.sql", Target = "CentralMD", RunOnce = false },
            new() { Order = 200, File = "Mmm_ChayThuHai.sql", Target = "CentralMD", RunOnce = false },
            new() { Order = 300, File = "Aaa_ChayCuoiCung.sql", Target = "CentralMD", RunOnce = false },
        };

        var executionOrderWithoutFix = entries
            .OrderBy(e => e.File, StringComparer.Ordinal) // hành vi CŨ: Name = e.File thô
            .ToList();

        Assert.Equal(
            ["Aaa_ChayCuoiCung.sql", "Mmm_ChayThuHai.sql", "Zzz_ChayDauTien.sql"],
            executionOrderWithoutFix.Select(e => e.File));
    }

    [Fact]
    public void ManifestThat_MoiEntryOrderPhaiNhoHon1Trieu_KhopGioiHanZeroPadD6()
    {
        // BuildScriptName dùng "{Order:D6}_..." — zero-pad 6 chữ số. Đúng miễn Order < 1_000_000
        // (7 chữ số trở lên vẫn CÓ THỂ đúng do ký tự đầu khác nhau, nhưng không còn đảm bảo tuyệt
        // đối — chặn sớm trước khi ai đó đặt order kiểu timestamp/epoch).
        var manifest = ManifestScriptProvider.Load(SqlDirectory);
        var tooLarge = manifest.Scripts.Where(s => s.Order is < 0 or >= 1_000_000).Select(s => $"{s.File} (order={s.Order})").ToList();

        Assert.True(tooLarge.Count == 0,
            $"Order vượt giới hạn an toàn của zero-pad D6 (phải trong [0, 999999]): {string.Join(", ", tooLarge)}.");
    }

    [Fact]
    public void ManifestThat_ThuTuThucThiThatCuaDbUp_PhaiKhopThuTuOrderChoTungTarget()
    {
        // Regression guard trên DỮ LIỆU THẬT của docs/sql/manifest.json (không phải dữ liệu giả) —
        // với TỪNG target, giả lập chính xác resort của DbUp (BuildScriptName rồi Ordinal-sort) và
        // xác nhận kết quả trùng khớp 100% với thứ tự OrderBy(Order) mà ManifestScriptProvider.TrackAFor
        // đã tính. Nếu 2 thứ tự lệch nhau ở BẤT KỲ target nào, nghĩa là bản vá zero-pad đã bị vô
        // hiệu (vd ai đó sửa BuildScriptName hoặc đặt order >= 1_000_000 làm sai lệch zero-pad).
        var manifest = ManifestScriptProvider.Load(SqlDirectory);

        foreach (var target in ManifestScriptProvider.ValidTargets)
        {
            var expectedOrder = ManifestScriptProvider.TrackAFor(manifest, target).Select(e => e.File).ToList();
            if (expectedOrder.Count == 0) continue;

            var actualDbUpOrder = SimulateDbUpExecutionOrder(ManifestScriptProvider.TrackAFor(manifest, target))
                .Select(e => e.File)
                .ToList();

            Assert.True(expectedOrder.SequenceEqual(actualDbUpOrder),
                $"[{target}] Thu tu DbUp se thuc thi (theo BuildScriptName + Ordinal-sort) KHONG khop " +
                $"thu tu 'order' trong manifest.json.\n  Ky vong: {string.Join(" -> ", expectedOrder)}\n" +
                $"  DbUp se chay: {string.Join(" -> ", actualDbUpOrder)}");
        }
    }
}
