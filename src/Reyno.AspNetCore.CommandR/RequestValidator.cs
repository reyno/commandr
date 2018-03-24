using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace Reyno.AspNetCore.CommandR {
    public abstract class RequestValidator<TRequest> : AbstractValidator<TRequest> { }
}
