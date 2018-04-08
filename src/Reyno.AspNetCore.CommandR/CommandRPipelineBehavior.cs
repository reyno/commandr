using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reyno.AspNetCore.CommandR {

    public class CommandRPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> {
        private readonly HttpContext _httpContext;
        private readonly ILogger<CommandRPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly CommandROptions _options;
        private readonly IServiceProvider _serviceProvider;

        public CommandRPipelineBehavior(
            IOptions<CommandROptions> optionsAccessor,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CommandRPipelineBehavior<TRequest, TResponse>> logger,
            IServiceProvider serviceProvider
            ) {
            _options = optionsAccessor.Value;
            _httpContext = httpContextAccessor.HttpContext;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private async Task AuthorizeRequest(TRequest request) {

            // need to ensure that we're logged in
            var policyProvider = _httpContext.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
            var policyEvaluator = _httpContext.RequestServices.GetRequiredService<IPolicyEvaluator>();
            var authenticateResult = await policyEvaluator.AuthenticateAsync(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                _httpContext
                );
            if (!authenticateResult.Succeeded) throw new ForbiddenException();

            // ***
            // now, we can attempt to authorize
            // ***

            // find authorizers
            var authorizers = _serviceProvider.GetServices<RequestAuthorizer<TRequest>>();

            // chcek if request authorization is required
            if (_options.RequireAuthorization && !authorizers.Any())
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
                    throw new ForbiddenException(forbidden.Select(x => x.Message).Where(s => !string.IsNullOrEmpty(s)));
            } catch (Exception e) {
                if (taskResult.IsCanceled) {
                    throw new TaskCanceledException();
                } else if (taskResult.IsFaulted) {
                    throw taskResult.Exception;
                } else {
                    throw;
                }
            }
        }

        private void LogRequest(TRequest request, bool success, double totalTime, double requestTime, double authorizeTime, double validateTime) {

            if (success)
                _logger.LogInformation("CommandR Request {Request} finished in {totalTime}ms", request.GetType().FullName, totalTime);
            else
                _logger.LogError("CommandR Request {Request} finished in {totalTime}ms", request.GetType().FullName, totalTime);

            try {
                _serviceProvider.GetService<TelemetryClient>().TrackEvent(
                    "CommandR Request",
                    // properties
                    new Dictionary<string, string> {
                        ["Request Type"] = request.GetType().FullName,
                        ["Success"] = success.ToString()
                    },
                    // metrics
                    new Dictionary<string, double> {
                        ["Total Time"] = totalTime,
                        ["Request Time"] = requestTime,
                        ["Authorize Time"] = authorizeTime,
                        ["Validate Time"] = validateTime
                    });
            } catch { /* application insights not in DI, so ignore */ }
        }

        private async Task ValidateRequest(TRequest request) {
            var validators = _serviceProvider.GetServices<RequestValidator<TRequest>>();

            foreach (var validator in validators)
                await validator.ValidateAndThrowAsync(request);
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next) {

            // start a stopwatch
            var stopwatch = Stopwatch.StartNew();
            var authorizeComplete = default(double);
            var validateComplete = default(double);

            // if we got this far, authorization and validation succeeded
            var success = true;
            try {

                // if authorization is required, do it
                if (_options.UseAuthorization || _options.RequireAuthorization) await AuthorizeRequest(request);

                authorizeComplete = stopwatch.ElapsedMilliseconds;

                // if validation is required, do it
                if (_options.UseValidation) await ValidateRequest(request);

                validateComplete = stopwatch.ElapsedMilliseconds;

                return await next();

            } catch {
                success = false;
                throw;
            } finally {

                var nextComplete = stopwatch.ElapsedMilliseconds;

                // log the request
                LogRequest(
                    request,
                    success,
                    nextComplete,
                    nextComplete - validateComplete,
                    authorizeComplete,
                    validateComplete - authorizeComplete
                    );

            }



        }
    }
}