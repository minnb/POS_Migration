using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using POS.Infrastructure.Logging;
using POS.Web.Services;

namespace POS.UnitTests.Services;

/// <summary>
/// Test thật cho <see cref="LogFileService"/> — luồng duyệt/tải file "Nhật ký hệ thống" (/admin/logs).
/// Ưu tiên chạy trên filesystem thật (temp dir / thư mục log thật của máy dev) thay vì mock, chỉ mock
/// qua <see cref="IDirectoryProbe"/> khi cần giả lập lỗi không thể tái tạo an toàn/portable
/// (UnauthorizedAccessException, symbolic link).
/// </summary>
public class LogFileServiceTests
{
    private static IConfiguration BuildConfig(string rootDir) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Logging:LogDirectory"] = rootDir })
            .Build();

    private static LogFileService CreateSut(string rootDir, IDirectoryProbe? probe = null) =>
        new(BuildConfig(rootDir), Mock.Of<IFileLogHelper>(), probe ?? new PhysicalDirectoryProbe());

    [Fact]
    public async Task GetDirectoryListingAsync_realConfiguredLogDirectory_listsActualSubfolders()
    {
        const string realLogRoot = @"D:\ROOT\Logs";
        if (!Directory.Exists(realLogRoot)) return; // guard: chỉ chạy thật trên máy có sẵn thư mục này

        var sut = CreateSut(realLogRoot);

        var result = await sut.GetDirectoryListingAsync("");

        result.ErrorMessage.Should().BeNull();
        result.Folders.Select(f => f.Name).Should().Contain(["api", "POS.Web"]);
    }

    [Fact]
    public async Task GetDirectoryListingAsync_directoryDoesNotExist_returnsErrorMessageNotThrow()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "pos-logtest-" + Guid.NewGuid().ToString("N"));
        var sut = CreateSut(tempRoot); // KHÔNG tạo thư mục — phải không tồn tại thật

        var result = await sut.GetDirectoryListingAsync("");

        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("không tồn tại");
        result.Folders.Should().BeEmpty();
        result.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDirectoryListingAsync_unauthorizedAccess_returnsPermissionErrorMessage()
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            var probe = new Mock<IDirectoryProbe>();
            probe.Setup(p => p.EnumerateDirectories(It.IsAny<string>()))
                 .Throws(new UnauthorizedAccessException("simulated deny"));
            probe.Setup(p => p.IsSymbolicLink(It.IsAny<string>())).Returns(false);
            var sut = CreateSut(tempRoot, probe.Object);

            var result = await sut.GetDirectoryListingAsync("");

            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("Không có quyền");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("../../../Windows")]
    [InlineData("C:/Windows")]
    public async Task GetDirectoryListingAsync_pathTraversalAttempt_isRejected(string maliciousRelativePath)
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            var sut = CreateSut(tempRoot);

            var result = await sut.GetDirectoryListingAsync(maliciousRelativePath);

            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("không hợp lệ");
            result.Folders.Should().BeEmpty();
            result.Files.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetDirectoryListingAsync_requestedDirectoryItselfIsSymlink_isRejected()
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            var probe = new Mock<IDirectoryProbe>();
            probe.Setup(p => p.IsSymbolicLink(tempRoot)).Returns(true); // root đang duyệt tự nó là symlink
            var sut = CreateSut(tempRoot, probe.Object);

            var result = await sut.GetDirectoryListingAsync("");

            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("symbolic link");
            result.Folders.Should().BeEmpty();
            result.Files.Should().BeEmpty();
            probe.Verify(p => p.EnumerateDirectories(It.IsAny<string>()), Times.Never); // chặn trước khi enumerate
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetDirectoryListingAsync_symbolicLinkEntryInsideValidRoot_isExcludedButRootStillListed()
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            var realSubDir = Path.Combine(tempRoot, "real-dir");
            Directory.CreateDirectory(realSubDir);
            var fakeSymlinkDir = Path.Combine(tempRoot, "evil-dir");

            var probe = new Mock<IDirectoryProbe>();
            probe.Setup(p => p.EnumerateDirectories(tempRoot)).Returns([realSubDir, fakeSymlinkDir]);
            probe.Setup(p => p.EnumerateFiles(tempRoot)).Returns([]);
            probe.Setup(p => p.IsSymbolicLink(tempRoot)).Returns(false); // root thật, không phải symlink
            probe.Setup(p => p.IsSymbolicLink(realSubDir)).Returns(false);
            probe.Setup(p => p.IsSymbolicLink(fakeSymlinkDir)).Returns(true);
            var sut = CreateSut(tempRoot, probe.Object);

            var result = await sut.GetDirectoryListingAsync("");

            result.ErrorMessage.Should().BeNull();
            result.Folders.Select(f => f.Name).Should().ContainSingle().Which.Should().Be("real-dir");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadLogFileAsync_symbolicLinkTarget_returnsFail()
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            var filePath = Path.Combine(tempRoot, "evil.log");
            File.WriteAllText(filePath, "nội dung không được lộ ra");

            var probe = new Mock<IDirectoryProbe>();
            probe.Setup(p => p.IsSymbolicLink(filePath)).Returns(true);
            var sut = CreateSut(tempRoot, probe.Object);

            var result = await sut.DownloadLogFileAsync("evil.log");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("symbolic link");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadLogFileAsync_realFile_returnsReadableStreamWithExpectedContent()
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            const string expectedContent = "dòng log thật — test streaming download";
            var filePath = Path.Combine(tempRoot, "real.log");
            await File.WriteAllTextAsync(filePath, expectedContent);
            var sut = CreateSut(tempRoot);

            var result = await sut.DownloadLogFileAsync("real.log");

            result.Success.Should().BeTrue();
            await using (result.Content)
            {
                using var reader = new StreamReader(result.Content!);
                var actualContent = await reader.ReadToEndAsync();
                actualContent.Should().Be(expectedContent);
            }
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadLogFileAsync_fileDoesNotExist_returnsFail()
    {
        var tempRoot = Directory.CreateTempSubdirectory("pos-logtest-").FullName;
        try
        {
            var sut = CreateSut(tempRoot);

            var result = await sut.DownloadLogFileAsync("khong-ton-tai.log");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("không tồn tại");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
