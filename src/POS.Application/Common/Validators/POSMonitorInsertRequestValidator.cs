using FluentValidation;
using POS.Application.Common.DTOs;

namespace POS.Application.Common.Validators;

public class POSMonitorInsertRequestValidator : AbstractValidator<POSMonitorInsertRequest>
{
    public POSMonitorInsertRequestValidator()
    {
        RuleFor(x => x.StoreNo).NotEmpty().WithMessage("StoreNo không được để trống");
        RuleFor(x => x.PosTerminalID).NotEmpty().WithMessage("PosTerminalID không được để trống");
        RuleFor(x => x.IpAddress).NotEmpty().WithMessage("IpAddress không được để trống");
    }
}
