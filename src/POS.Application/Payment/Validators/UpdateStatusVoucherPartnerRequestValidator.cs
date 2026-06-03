using FluentValidation;
using POS.Application.Payment.DTOs;

namespace POS.Application.Payment.Validators;

public class UpdateStatusVoucherPartnerRequestValidator : AbstractValidator<UpdateStatusVoucherPartnerRequest>
{
    public UpdateStatusVoucherPartnerRequestValidator()
    {
        RuleFor(x => x.Partner).NotEmpty();
        RuleFor(x => x.StoreNo).NotEmpty();
        RuleFor(x => x.PosNo).NotEmpty();
        RuleFor(x => x.OrderNo).NotEmpty();
        RuleFor(x => x.SerialNo).NotEmpty().Must(x => x.Count > 0).WithMessage("SerialNo phải có ít nhất 1 phần tử");
    }
}
