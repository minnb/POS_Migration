using FluentValidation;
using POS.Application.Gift.DTOs;

namespace POS.Application.Gift.Validators;

public class MMLSchemeRequestValidator : AbstractValidator<MMLSchemeRequest>
{
    public MMLSchemeRequestValidator()
    {
        RuleFor(x => x.PosNo).NotEmpty();
        RuleFor(x => x.OrderNo).NotEmpty();
        RuleFor(x => x.StoreNo).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}
