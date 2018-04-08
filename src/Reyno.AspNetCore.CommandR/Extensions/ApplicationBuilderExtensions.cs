using Reyno.AspNetCore.CommandR;

namespace Microsoft.AspNetCore.Builder {

    public static class DependencyInjectionExtensions {

        public static void UseCommandR(this IApplicationBuilder app) {
            app.UseMiddleware<CommandRMiddleware>();
        }
    }
}