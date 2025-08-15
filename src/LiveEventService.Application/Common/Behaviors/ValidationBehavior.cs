using FluentValidation;
using MediatR;

namespace LiveEventService.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that executes all registered FluentValidation validators
/// for the incoming request and throws <see cref="FluentValidation.ValidationException"/>
/// when validation failures are detected.
/// </summary>
/// <typeparam name="TRequest">The type of request to validate.</typeparam>
/// <typeparam name="TResponse">The type of response to return.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators applicable to the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Executes validation prior to invoking the next handler in the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">Delegate to invoke the next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the next handler if validation passes.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Any())
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
