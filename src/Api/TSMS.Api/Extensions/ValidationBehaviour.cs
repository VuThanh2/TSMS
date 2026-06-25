using FluentValidation;
using MediatR;
using SharedKernel.Primitives;

namespace TSMS.Api.Extensions;

// MediatR pipeline behavior — tự động chạy FluentValidation cho mọi Command/Query
// có Validator được register. Nếu không có Validator thì bỏ qua, handler chạy bình thường.
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var firstFailure = failures.First();
        var error = Error.Create("Validation.Failed", firstFailure.ErrorMessage);
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (responseType.IsGenericType
            && responseType.GetGenericTypeDefinition() == typeof(Result<>)) {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result.Failure), [typeof(Error)])!;
            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        // Fallback nếu handler không trả Result
        throw new ValidationException(failures);
    }
}