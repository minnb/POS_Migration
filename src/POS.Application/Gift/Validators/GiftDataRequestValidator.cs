using FluentValidation;
using POS.Application.Gift.DTOs;

namespace POS.Application.Gift.Validators;

public class GiftDataRequestValidator : AbstractValidator<GiftDataRequest>
{
    public GiftDataRequestValidator()
    {
        RuleFor(x => x.PosNo).NotEmpty();
        RuleFor(x => x.GiftCode).NotEmpty().Must(c => c.Length > 0).WithMessage("GiftCode không được rỗng");
        RuleFor(x => x.Status).NotEmpty();
    }
}
