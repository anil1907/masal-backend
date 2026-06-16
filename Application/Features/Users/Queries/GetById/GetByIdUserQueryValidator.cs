using FluentValidation;

namespace Application.Features.Users.Queries.GetById;

public class GetByIdUserQueryValidator : AbstractValidator<GetByIdUserQuery>
{
    public GetByIdUserQueryValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithMessage("Id is required.");
    }
}
