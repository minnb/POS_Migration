using FluentValidation;
using POS.Application.Common.DTOs;

namespace POS.Application.Common.Validators;

public class POSEODRequestValidator : AbstractValidator<POSEODRequest>
{
    public POSEODRequestValidator()
    {
        RuleFor(x => x.StoreNo).NotEmpty().WithMessage("StoreNo đang bị trống");
        RuleFor(x => x.POSTerminal).NotEmpty().WithMessage("POSTerminal đang bị trống");
    }
}
