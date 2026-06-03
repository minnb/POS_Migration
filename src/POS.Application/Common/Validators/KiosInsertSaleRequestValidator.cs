using FluentValidation;
using POS.Application.Common.DTOs;

namespace POS.Application.Common.Validators;

public class KiosInsertSalePOSRequestValidator : AbstractValidator<KiosInsertSalePOSRequest>
{
    public KiosInsertSalePOSRequestValidator()
    {
        RuleFor(x => x.StoreNo).NotEmpty().WithMessage("CH/ST đang bị trống");
        RuleFor(x => x.SaleJson).NotEmpty().WithMessage("SaleJson không được để trống");
    }
}
