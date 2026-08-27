namespace Hospital.Application.Validators;

public interface IRequestValidator<in TRequest>
{
    IReadOnlyDictionary<string, string[]> Validate(TRequest request);
}
