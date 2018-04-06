using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Reyno.AspNetCore.CommandR {
    public class RequestValidatorPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> {
        private readonly IServiceProvider _provider;

        public RequestValidatorPipelineBehavior(
            IServiceProvider provider
            ) {
            _provider = provider;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next) {

            var validators = _provider.GetServices<RequestValidator<TRequest>>();

            foreach (var validator in validators)
                await validator.ValidateAndThrowAsync(request);

            return await next();

        }

    }
}
