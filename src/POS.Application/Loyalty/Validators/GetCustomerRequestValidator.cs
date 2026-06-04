using FluentValidation;
using POS.Application.Loyalty.DTOs;

namespace POS.Application.Loyalty.Validators;

public class GetCustomerRequestValidator : AbstractValidator<GetCustomerRequest>
{
    public GetCustomerRequestValidator()
    {
        RuleFor(x => x.NumberCard)
            .NotEmpty().WithMessage("numberCard không được để trống")
            .MinimumLength(9).WithMessage("numberCard phải có ít nhất 9 ký tự");

        RuleFor(x => x.PosId).NotEmpty().WithMessage("posID không được để trống");
        RuleFor(x => x.StoreNo).NotEmpty().WithMessage("storeNo không được để trống");
    }
}
