using POS.Application.Features.SpAudit;
using POS.Common.Enums;

namespace POS.UnitTests.SpAudit;

public class SpComplexityClassifierTests
{
    private static string Filler(int lines) =>
        string.Join('\n', Enumerable.Repeat("    -- filler line", lines));

    [Fact]
    public void Simple_select_short_no_control_flow_is_Simple()
    {
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

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Simple, complexity);
    }

    [Fact]
    public void While_and_try_catch_around_120_lines_is_Moderate()
    {
        var sql = $$"""
            CREATE PROCEDURE dbo.Sp_ProcessBatch
                @BatchSize INT
            AS
            BEGIN
                DECLARE @i INT = 0;
                BEGIN TRY
                    WHILE @i < @BatchSize
                    BEGIN
                        SET @i = @i + 1;
                    END
                END TRY
                BEGIN CATCH
                    THROW;
                END CATCH
            {{Filler(110)}}
            END
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Moderate, complexity);
    }

    [Fact]
    public void Cursor_usage_is_Complex()
    {
        var sql = """
            CREATE PROCEDURE dbo.Sp_IterateStores
            AS
            BEGIN
                DECLARE cur CURSOR FOR SELECT No FROM dbo.Store;
                OPEN cur;
                FETCH NEXT FROM cur INTO @No;
                CLOSE cur;
                DEALLOCATE cur;
            END
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Complex, complexity);
    }

    [Fact]
    public void Dynamic_sql_sp_executesql_is_Complex()
    {
        var sql = """
            CREATE PROCEDURE dbo.Sp_RunDynamic
                @Sql NVARCHAR(MAX)
            AS
            BEGIN
                EXEC sp_executesql @Sql;
            END
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Complex, complexity);
    }

    [Fact]
    public void Nested_exec_of_another_procedure_is_Complex()
    {
        var sql = """
            CREATE PROCEDURE dbo.Sp_Wrapper
                @FromDate DATETIME,
                @ToDate DATETIME
            AS
            BEGIN
                EXEC dbo.Rpt_ReportSaleByTime @FromDate, @ToDate;
            END
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Complex, complexity);
    }

    [Fact]
    public void Over_300_lines_with_no_other_signal_is_Complex()
    {
        var sql = $$"""
            CREATE PROCEDURE dbo.Sp_HugeButPlain
            AS
            BEGIN
                SELECT 1;
            {{Filler(305)}}
            END
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Complex, complexity);
    }

    [Fact]
    public void Temp_table_only_under_50_lines_is_Moderate()
    {
        var sql = """
            CREATE PROCEDURE dbo.Sp_TempTableOnly
            AS
            BEGIN
                CREATE TABLE #tmp (Id INT);
                INSERT INTO #tmp SELECT 1;
                SELECT * FROM #tmp;
            END
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Moderate, complexity);
    }

    [Fact]
    public void Lowercase_cursor_keyword_is_case_insensitive_Complex()
    {
        var sql = """
            create procedure dbo.sp_iterate
            as
            begin
                declare cur cursor for select 1;
                open cur;
                close cur;
                deallocate cur;
            end
            """;

        var (complexity, _) = SpComplexityClassifier.Classify(sql);

        Assert.Equal(SpComplexity.Complex, complexity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Null_or_empty_definition_is_Moderate_with_note_and_does_not_throw(string? definition)
    {
        var (complexity, note) = SpComplexityClassifier.Classify(definition);

        Assert.Equal(SpComplexity.Moderate, complexity);
        Assert.NotEmpty(note);
    }
}
