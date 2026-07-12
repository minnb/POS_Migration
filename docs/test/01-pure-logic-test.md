# Mẫu 1 — Pure Logic Test (không mock, không DI)

## Khi nào dùng

Test 1 method/class **thuần túy** (`static`, không I/O, không phụ thuộc DB/HTTP/Redis) — cùng 1 input
luôn cho ra cùng 1 output. Đây là loại test **rẻ nhất và ổn định nhất**, nên ưu tiên tách logic ra dạng
này bất cứ khi nào có thể (VD: `SpComplexityClassifier`, helper format/parse, validation thuần).

## Nguyên tắc

- **Không cần Mock** — không có dependency nào để mock.
- Cấu trúc **Arrange – Act – Assert** rõ ràng, tách biệt 3 phần.
- Dùng `[Theory]` + `[InlineData]` khi cùng 1 method nhưng test nhiều biến thể input/output — tránh
  copy-paste nhiều `[Fact]` giống hệt nhau chỉ khác data.
- Tên method: `Method_condition_expectedResult` (VD `Classify_cursorUsage_returnsComplex`).
- Không throw exception ở input biên (null/rỗng) — assert rõ hành vi graceful, không crash.

## Code mẫu (rút gọn từ `tests/POS.UnitTests/SpAudit/SpComplexityClassifierTests.cs` thật trong repo)

```csharp
using POS.Application.Features.SpAudit;
using POS.Common.Enums;

namespace POS.UnitTests.SpAudit;

public class SpComplexityClassifierTests
{
    [Fact]
    public void Classify_simpleSelectShortNoControlFlow_returnsSimple()
    {
        // Arrange
        var sql = """
            CREATE PROCEDURE dbo.Sp_GetStoreByNo
                @StoreNo NVARCHAR(20)
            AS
            BEGIN
                SELECT No, Name
                FROM dbo.Store WITH (NOLOCK)
                WHERE No = @StoreNo;
            END
            """;

        // Act
        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        // Assert
        Assert.Equal(SpComplexity.Simple, complexity);
    }

    [Fact]
    public void Classify_cursorUsage_returnsComplex()
    {
        // Arrange
        var sql = """
            CREATE PROCEDURE dbo.Sp_IterateStores
            AS
            BEGIN
                DECLARE cur CURSOR FOR SELECT No FROM dbo.Store;
                OPEN cur;
                CLOSE cur;
                DEALLOCATE cur;
            END
            """;

        // Act
        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        // Assert
        Assert.Equal(SpComplexity.Complex, complexity);
    }

    // [Theory]: 1 method, nhiều input/output — tránh lặp lại nhiều [Fact] giống nhau
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_nullOrEmptyDefinition_returnsModerateWithNoteAndDoesNotThrow(string? definition)
    {
        // Act
        var (complexity, note) = SpComplexityClassifier.Classify(definition);

        // Assert
        Assert.Equal(SpComplexity.Moderate, complexity);
        Assert.NotEmpty(note);
    }
}
```

## Verify

```bash
dotnet test tests/POS.UnitTests --filter FullyQualifiedName~SpComplexityClassifierTests
```
