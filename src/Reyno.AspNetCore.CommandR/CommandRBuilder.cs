using System;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Reyno.AspNetCore.CommandR {
    public class CommandRBuilder {

        private IServiceCollection _services;

        public CommandRBuilder(IServiceCollection services) {
            _services = services;
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

    }
}
