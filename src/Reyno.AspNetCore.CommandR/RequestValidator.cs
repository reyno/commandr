using FluentValidation;

namespace Reyno.AspNetCore.CommandR {

    public abstract class RequestValidator<TRequest> : AbstractValidator<TRequest> { }
}