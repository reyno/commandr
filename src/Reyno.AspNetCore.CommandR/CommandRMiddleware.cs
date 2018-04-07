using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Serialization;

namespace Reyno.AspNetCore.CommandR {
    public class CommandRMiddleware {

        private readonly RequestDelegate _next;

        public CommandRMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IOptions<CommandROptions> optionsAccessor,
            IRequestResolver requestResolver,
            IMediator mediator
            ) {

            var options = optionsAccessor.Value;

            var command = Convert.ToString(context.GetRouteValue("command"));

            var requestType = requestResolver.ResolveType(command);

            IBaseRequest request;
            var serializer = new JsonSerializer();
            using (var reader = new StreamReader(context.Request.Body))
            using (var jsonReader = new JsonTextReader(reader))
                request = (IBaseRequest)serializer.Deserialize(jsonReader, requestType);


            var returnType = GetReturnType(requestType);


            context.Response.ContentType = "application/json";

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

                try {

                    await task;

                    var resultProperty = task.GetType().GetProperty("Result");
                    var result = resultProperty.GetValue(task);

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(SerializeResult(result));

                } catch (ForbiddenException forbiddenException) {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await context.Response.WriteAsync(SerializeResult(forbiddenException.Messages));
                } catch (FluentValidation.ValidationException validationException) {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync(SerializeResult(validationException.Errors.Select(x => new {
                        x.ErrorMessage,
                        x.PropertyName,
                        x.AttemptedValue
                    })));
                } catch (Exception e) {
                    await ReturnError(context.Response, e);
                }

            }

        }

        private string SerializeResult<T>(T result) {

            return JsonConvert.SerializeObject(result, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 10,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        }

        private async Task ReturnError(HttpResponse response, Exception e, HttpStatusCode status = HttpStatusCode.InternalServerError) {

            response.StatusCode = (int)status;
            await response.WriteAsync(JsonConvert.SerializeObject(new {
                message = e.Message,
                Stack = e.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            }));

        }

        private Type GetReturnType(Type requestType) {
            var i = requestType.GetTypeInfo().ImplementedInterfaces.First();
            return i.GenericTypeArguments.FirstOrDefault();
        }

    }
}
