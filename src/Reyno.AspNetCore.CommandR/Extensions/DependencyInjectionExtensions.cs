using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reyno.AspNetCore.CommandR;

namespace Microsoft.Extensions.DependencyInjection {

    public static class DependencyInjectionExtensions {

        private static CommandRBuilder AddCommandRCore(this IServiceCollection services) {
            services.AddMediatR();

            // add the default request resolver
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRequestResolver, DefaultRequestResolver>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandRPipelineBehavior<,>));

            // find all the assemblies in this domain
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

            // add all the request authorizers and validators for this domain
            AddGenericTypes(services, assemblies, typeof(RequestAuthorizer<>));
            AddGenericTypes(services, assemblies, typeof(RequestValidator<>));

            return new CommandRBuilder(services);
        }

        private static void AddGenericTypes(IServiceCollection services, IEnumerable<Assembly> assemblies, Type type) {
            // find all types that derive from the generic type supplied
            var types =
                from a in assemblies
                from t in a.GetTypes()
                where t.IsClass
                 && t.BaseType != null
                 && t.BaseType.IsGenericType
                 && t.BaseType.GetGenericTypeDefinition() == type
                select t
                ;

            foreach (var implementationType in types)
                services.AddScoped(implementationType.BaseType, implementationType);
        }

        public static CommandRBuilder AddCommandR(this IServiceCollection services) {
            var builder = services.AddCommandRCore();

            return builder;
        }

        public static CommandRBuilder AddCommandR(this IServiceCollection services, Action<CommandROptions> setupAction) {
            services.Configure(setupAction);

            return services.AddCommandR();
        }
    }
}