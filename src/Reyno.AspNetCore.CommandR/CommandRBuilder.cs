using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Reyno.AspNetCore.CommandR {
    public class CommandRBuilder {

        private IServiceCollection _services;

        public CommandRBuilder(IServiceCollection services) {
            _services = services;
        }

        public CommandRBuilder AddValidation() {

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

            AddGenericTypes(assemblies, typeof(RequestValidator<>));

            _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestValidatorPipelineBehavior<,>));

            return this;

        }

        public CommandRBuilder AddAuthorization(Action<RequestAuthorizerOptions> setupAction = null) {

            _services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

            AddGenericTypes(assemblies, typeof(RequestAuthorizer<>));

            _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestAuthorizerPipelineBehavior<,>));

            if (setupAction != null)
                _services.Configure(setupAction);

            return this;

        }

        public CommandRBuilder UseRequestResolver<TType>() where TType : class, IRequestResolver {

            // replace the request resolver in DI
            _services.RemoveAll<IRequestResolver>();
            _services.AddSingleton<IRequestResolver, TType>();

            return this;

        }

        public CommandRBuilder AddPipelineBehavior<TType, TRequest, TResponse>() where TType: IPipelineBehavior<TRequest, TResponse> {

            _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TType));

            return this;

        }


        private void AddGenericTypes(IEnumerable<Assembly> assemblies, Type type) {

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
                _services.AddScoped(implementationType.BaseType, implementationType);


        }
    }
}
