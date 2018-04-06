using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Reyno.AspNetCore.CommandR {

    public class RequestAuthorizerPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> {
        private readonly HttpContext _httpContext;
        private readonly IServiceProvider _provider;
        private readonly RequestAuthorizerOptions _options;

        public RequestAuthorizerPipelineBehavior(
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider provider,
            IOptions<RequestAuthorizerOptions> optionsAccessor
            ) {
            _httpContext = httpContextAccessor.HttpContext;
            _provider = provider;
            _options = optionsAccessor.Value;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next) {

            // find authorizers
            var authorizers = _provider.GetServices<RequestAuthorizer<TRequest>>();

            // chcek if request authorization is required
            if (_options.AuthorizationRequired && !authorizers.Any())
                throw new ForbiddenException($"Request authorization is required but no request authorizer was found for {typeof(TRequest).FullName}");

            // run all the authorizers
            var taskResult = Task.WhenAll(
                authorizers.Select(a => a.Authorize(request, _httpContext))
                );

            // handle any exceptions
            try {

                // execute all the authorizers
                await taskResult;

                // look for any forbidden results
                var forbidden = taskResult.Result.Where(x => !x.Authorized);

                // throw if any authorizers returned forbidden
                if (forbidden.Any())
                    throw new ForbiddenException(forbidden.Select(x => x.Message));

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
