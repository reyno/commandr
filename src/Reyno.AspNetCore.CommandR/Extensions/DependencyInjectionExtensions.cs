using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Http;
using Reyno.AspNetCore.CommandR;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Microsoft.Extensions.DependencyInjection {
    public static class DependencyInjectionExtensions {

        public static CommandRBuilder AddCommandR(this IServiceCollection services) {

            var builder = services.AddCommandRCore();

            return builder;

        }

        private static CommandRBuilder AddCommandRCore(this IServiceCollection services) {

            services.AddMediatR();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // add the validator and authorizer pipeline behaviors
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizePipelineBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatePipelineBehavior<,>));

            // add the default request resolver
            services.AddSingleton<IRequestResolver, DefaultRequestResolver>();

            // use reflection to add all authorizer and validator implementations
            services.AddAuthorizers(assemblies);
            services.AddValidators(assemblies);


            return new CommandRBuilder(services);

        }

        public static IServiceCollection AddCommandR(this IServiceCollection services, Action<CommandROptions> setupAction) {

            services.AddCommandR();
            services.Configure(setupAction);

            return services;

        }

        private static void AddAuthorizers(this IServiceCollection services, IEnumerable<Assembly> assemblies)
            => services.AddGenericTypes(assemblies, typeof(RequestAuthorizer<>));

        private static void AddValidators(this IServiceCollection services, IEnumerable<Assembly> assemblies)
            => services.AddGenericTypes(assemblies, typeof(RequestValidator<>));


        private static void AddGenericTypes(this IServiceCollection services, IEnumerable<Assembly> assemblies, Type type) {

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

    }
}
