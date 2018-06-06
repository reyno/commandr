using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Reyno.AspNetCore.CommandR {

    public class CommandRMiddleware {
        private readonly RequestDelegate _next;

        public CommandRMiddleware(RequestDelegate next) {
            _next = next;
        }

        private Type GetReturnType(Type requestType) {
            var i = requestType.GetTypeInfo().ImplementedInterfaces.First();
            return i.GenericTypeArguments.FirstOrDefault();
        }

        private async Task HandleCommandRRequest(HttpContext context) {
            // get stuff from DI
            var mediator = context.RequestServices.GetService<IMediator>();
            var jsonOptions = context.RequestServices.GetService<IOptions<CommandRJsonOptions>>().Value;

            // pull out the command name
            var command = GetCommandName(context);

            // use the resolver registered in DI to get the request type for this command
            var requestResolver = context.RequestServices.GetService<IRequestResolver>();
            var requestType = requestResolver.ResolveType(command);

            // read the request
            IBaseRequest request;
            var serializer = new JsonSerializer();
            using (var reader = new StreamReader(context.Request.Body))
            using (var jsonReader = new JsonTextReader(reader))
                request = (IBaseRequest)serializer.Deserialize(jsonReader, requestType);

            // get the return type from the request type
            var returnType = GetReturnType(requestType) ?? typeof(Unit);

            if (returnType == default) {
                await mediator.Send(request as IRequest);

                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            } else {
                var sendMethod = mediator.GetType().GetMethods().SingleOrDefault(x
                    => x.Name == "Send"
                    && x.IsGenericMethod
                    );

                var genericSendMethod = sendMethod.MakeGenericMethod(returnType);

                var task = (Task)genericSendMethod.Invoke(mediator, new object[] { request, default(CancellationToken) });

                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                var result = resultProperty.GetValue(task);

                await WriteResponse(context, HttpStatusCode.OK, result);
            }
        }

        private async Task ReturnError(HttpResponse response, Exception e, HttpStatusCode status = HttpStatusCode.InternalServerError) {
            response.StatusCode = (int)status;
            await response.WriteAsync(JsonConvert.SerializeObject(new {
                message = e.Message,
                Stack = e.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            }));
        }

        private string SerializeResult<T>(T result, JsonSerializerSettings settings) {
            return JsonConvert.SerializeObject(result, settings);
        }

        private async Task WriteResponse<T>(HttpContext context, HttpStatusCode status, T content = default) where T : class {

            var jsonOptions = context.RequestServices.GetService<IOptions<CommandRJsonOptions>>().Value;

            context.Response.StatusCode = (int)status;

            if (content != default(T)) {
                var json = JsonConvert.SerializeObject(content, jsonOptions.SerializerSettings);
                await context.Response.WriteAsync(json);
            }

        }

        public string GetCommandName(HttpContext context) {
            var path = context.Request.Path.Value;
            var queryIndex = path.IndexOf("?");
            var pathWithoutQuery = queryIndex == -1 ? path : path.Substring(0, queryIndex);

            var pathParts = pathWithoutQuery.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length < 2) throw new ArgumentException("Command name not found in path");

            return pathParts[1];
        }

        public async Task InvokeAsync(
            HttpContext context,
            ILogger<CommandRMiddleware> logger
            ) {
            // check to see if this is a commandr request
            var isCommandRRequest = IsCommandRRequest(context);

            if (isCommandRRequest) {
                try {
                    // is a CommandR request, so handle it
                    await HandleCommandRRequest(context);
                } catch (ForbiddenException forbiddenException) {
                    await WriteResponse(context, HttpStatusCode.Forbidden, forbiddenException.Messages);
                } catch (FluentValidation.ValidationException validationException) {
                    await WriteResponse(context, HttpStatusCode.BadRequest, validationException.Errors.Select(x => new {
                        x.ErrorMessage,
                        x.PropertyName,
                        x.AttemptedValue
                    }));
                } catch (Exception e) {
                    logger.LogError(e, "CommandR Request: {path}", context.Request.Path.Value);
                    await WriteResponse(context, HttpStatusCode.InternalServerError, e);
                }
            } else {
                // not a CommandR request, pass to the next middleware
                await _next(context);
            }
        }

        public bool IsCommandRRequest(HttpContext context) {
            var options = context.RequestServices.GetService<IOptions<CommandROptions>>().Value;
            var path = options.Path;

            return context.Request.Path.StartsWithSegments($"/{path}");
        }
    }
}