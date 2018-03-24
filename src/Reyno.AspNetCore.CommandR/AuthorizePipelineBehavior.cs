using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Reyno.AspNetCore.CommandR {

    public class AuthorizePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> {
        private readonly HttpContext _httpContext;
        private readonly IServiceProvider _provider;

        public AuthorizePipelineBehavior(
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider provider
            ) {
            _httpContext = httpContextAccessor.HttpContext;
            _provider = provider;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next) {

            // find authorizers
            var authorizers = _provider.GetServices<RequestAuthorizer<TRequest>>();

            // run all the authorizers
            var taskResult = Task.WhenAll(
                authorizers.Select(a => a.Authorize(request, _httpContext))
                );

            // handle any exceptions
            try {
                await taskResult;
            } catch (Exception e) {
                if (taskResult.IsCanceled) {
                    throw new TaskCanceledException();
                } else if (taskResult.IsFaulted) {
                    throw taskResult.Exception;
                } else {
                    throw;
                }
            }

            // if we got this far, the request is authorized
            return await next();

        }

    }
}
