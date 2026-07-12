using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using POS.Api.Controllers;
using POS.Application.Features.Partner;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.Logging;

namespace POS.UnitTests.Features.Partner;

/// <summary>
/// Unit test cho <see cref="PaymentController"/> — sinh bởi skill payment-test-generator.
/// Test qua seam interface Application (IGotITService/IUrboxService), mock bằng Moq.
/// Không gọi HTTP/DB thật; không đụng production code.
/// Kịch bản: routing-theo-partner, success→200, partner-fail→400, unknown→BadRequest, exception→500.
/// </summary>
public class PaymentControllerTests
{
    private static (PaymentController Sut, Mock<IGotITService> GotIt, Mock<IUrboxService> Urbox)
        BuildSut()
    {
        var gotit = new Mock<IGotITService>();
        var urbox = new Mock<IUrboxService>();
        var kibana = new Mock<IKibanaService>();
        var fileLog = new Mock<IFileLogHelper>();
        var sut = new PaymentController(kibana.Object, fileLog.Object, gotit.Object, urbox.Object);
        return (sut, gotit, urbox);
    }

    private static Tuple<bool, string, List<DataVoucherPartnerResponse>> GotItResult(
        bool success, string msg = "msg")
        => Tuple.Create(success, msg, new List<DataVoucherPartnerResponse>());

    private static Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>
        UrboxResult(bool success, string msg = "msg")
        => Tuple.Create(success, msg, new List<DataVoucherPartnerResponse>(),
                        new List<UrboxProducts>());

    // ── ValidateVoucher ──────────────────────────────────────────────────

    [Fact]
    public async Task ValidateVoucher_gotitSuccess_returns200_andCallsCheckMultiple()
    {
        var (sut, gotit, urbox) = BuildSut();
        gotit.Setup(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                         It.IsAny<CancellationToken>()))
             .ReturnsAsync(GotItResult(true));

        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "GOTIT", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
        gotit.Verify(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                          It.IsAny<CancellationToken>()), Times.Once);
        urbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateVoucher_gotitFails_returns400()
    {
        var (sut, gotit, _) = BuildSut();
        gotit.Setup(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                         It.IsAny<CancellationToken>()))
             .ReturnsAsync(GotItResult(false, "voucher invalid"));

        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "gotit", PosNo = "P01" }, // ToUpper() → GOTIT
            CancellationToken.None);

        result.Should().BeAssignableTo<ObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ValidateVoucher_urboxSuccess_returns200_andCallsCheckSerial()
    {
        var (sut, _, urbox) = BuildSut();
        urbox.Setup(x => x.CheckSerialUrbox(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                            It.IsAny<CancellationToken>()))
             .ReturnsAsync(UrboxResult(true));

        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "URBOX", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
        urbox.Verify(x => x.CheckSerialUrbox(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                             It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateVoucher_unknownPartner_returnsBadRequest()
    {
        var (sut, gotit, urbox) = BuildSut();

        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "FOO", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
        gotit.VerifyNoOtherCalls();
        urbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateVoucher_serviceThrows_returns500()
    {
        var (sut, gotit, _) = BuildSut();
        gotit.Setup(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                         It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "GOTIT", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeAssignableTo<ObjectResult>()
              .Which.StatusCode.Should().Be(500);
    }

    // ── UpdateStatusVoucher ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusVoucher_gotitSuccess_returns200_andCallsMarkUse()
    {
        var (sut, gotit, _) = BuildSut();
        gotit.Setup(x => x.MarkUseMultiple(It.IsAny<UpdateStatusVoucherPartnerRequest>(),
                                           It.IsAny<CancellationToken>()))
             .ReturnsAsync(GotItResult(true));

        var result = await sut.UpdateStatusVoucher(
            new UpdateStatusVoucherPartnerRequest { Partner = "GOTIT", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
        gotit.Verify(x => x.MarkUseMultiple(It.IsAny<UpdateStatusVoucherPartnerRequest>(),
                                            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusVoucher_urboxSuccess_returns200_andCallsPayCode()
    {
        var (sut, _, urbox) = BuildSut();
        urbox.Setup(x => x.PayCodelUrbox(It.IsAny<UpdateStatusVoucherPartnerRequest>(),
                                         It.IsAny<CancellationToken>()))
             .ReturnsAsync(UrboxResult(true));

        var result = await sut.UpdateStatusVoucher(
            new UpdateStatusVoucherPartnerRequest { Partner = "URBOX", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
        urbox.Verify(x => x.PayCodelUrbox(It.IsAny<UpdateStatusVoucherPartnerRequest>(),
                                          It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusVoucher_unknownPartner_returnsBadRequest()
    {
        var (sut, _, _) = BuildSut();

        var result = await sut.UpdateStatusVoucher(
            new UpdateStatusVoucherPartnerRequest { Partner = "BAR", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateStatusVoucher_serviceThrows_returns500()
    {
        var (sut, _, urbox) = BuildSut();
        urbox.Setup(x => x.PayCodelUrbox(It.IsAny<UpdateStatusVoucherPartnerRequest>(),
                                         It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await sut.UpdateStatusVoucher(
            new UpdateStatusVoucherPartnerRequest { Partner = "URBOX", PosNo = "P01" },
            CancellationToken.None);

        result.Should().BeAssignableTo<ObjectResult>()
              .Which.StatusCode.Should().Be(500);
    }
}
