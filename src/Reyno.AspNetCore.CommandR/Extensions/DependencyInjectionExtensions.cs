using System;
using MediatR;
using Reyno.AspNetCore.CommandR;


namespace Microsoft.Extensions.DependencyInjection {
    public static class DependencyInjectionExtensions {

        public static CommandRBuilder AddCommandR(this IServiceCollection services) {

            var builder = services.AddCommandRCore();

            return builder;

        }

        private static CommandRBuilder AddCommandRCore(this IServiceCollection services) {

            services.AddMediatR();

            // add the default request resolver
            services.AddSingleton<IRequestResolver, DefaultRequestResolver>();

            return new CommandRBuilder(services);

        }

        public static CommandRBuilder AddCommandR(this IServiceCollection services, Action<CommandROptions> setupAction) {

            var builder = services.AddCommandR();

            services.Configure(setupAction);

            return builder;

        }

    }
}
