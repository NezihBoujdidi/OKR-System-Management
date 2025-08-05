namespace NXM.Tensai.Back.OKR.Application;

public record GetKeyResultByIdQuery(Guid Id) : IRequest<KeyResultDto>;

public class GetKeyResultByIdQueryValidator : AbstractValidator<GetKeyResultByIdQuery>
{
    public GetKeyResultByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Key Result ID must not be empty.");
    }
}

public class GetKeyResultByIdQueryHandler : IRequestHandler<GetKeyResultByIdQuery, KeyResultDto>
{
    private readonly IKeyResultRepository _keyResultRepository;

    public GetKeyResultByIdQueryHandler(IKeyResultRepository keyResultRepository)
    {
        _keyResultRepository = keyResultRepository;
    }

    public async Task<KeyResultDto> Handle(GetKeyResultByIdQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await new GetKeyResultByIdQueryValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var keyResult = await _keyResultRepository.GetByIdAsync(request.Id);
        if (keyResult == null || keyResult.IsDeleted)
        {
            throw new NotFoundException(nameof(KeyResult), request.Id);
        }

        return keyResult.ToDto();
    }
}
