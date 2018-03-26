using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Routing;
using Reyno.AspNetCore.CommandR;

namespace Microsoft.AspNetCore.Builder {
    public static class DependencyInjectionExtensions {

        public static void UseCommandR(this IApplicationBuilder app) {


            var routeBuilder = new RouteBuilder(app);

            routeBuilder.MapMiddlewarePost($"_commandr/{{command}}", commandrApp => commandrApp.UseMiddleware<CommandRMiddleware>());

            app.UseRouter(routeBuilder.Build());


        }

    }
}
