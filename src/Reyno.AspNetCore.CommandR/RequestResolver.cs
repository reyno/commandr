using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.Extensions.Options;

namespace Reyno.AspNetCore.CommandR {

    public interface IRequestResolver {

        Type ResolveType(string command);
    }

    public class DefaultRequestResolver : IRequestResolver {
        private readonly IEnumerable<Type> _requestTypes;
        private CommandROptions _options;

        public DefaultRequestResolver(
            IOptions<CommandROptions> optionsAccessor
            ) {
            _options = optionsAccessor.Value;

            var interfaceType = typeof(IBaseRequest);

            _requestTypes = (
                from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
                from type in assembly.GetTypes()
                where type.IsClass
                let implementedType = type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault()
                where interfaceType.IsAssignableFrom(implementedType)
                select type
                ).ToList();
        }

        public Type ResolveType(string command) {
            // expecting the format: values.get
            // should translate into {namespace}.Values.GetRequest

            var typePath = $"{_options.RequestNamespace}.{command}Request";

            // capitilise
            var normalisedTypePath = typePath.Replace("-", string.Empty); /*string.Join(
                ".",
                typePath
                    .Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => string.Join(
                        string.Empty,
                        part
                            .Split(
                                new[] { "-" },
                                StringSplitOptions.RemoveEmptyEntries
                            )
                            .Select(s => string.Concat(s.Substring(0, 1).ToUpper(), s.Substring(1)))
                    )
                )
            );*/

            var matchingTypes = _requestTypes.Where(type => type.FullName.EndsWith(normalisedTypePath, StringComparison.OrdinalIgnoreCase));

            switch (matchingTypes.Count()) {
                case 0: throw new Exception($"No matching request type found for: {command}{Environment.NewLine}Path used: {normalisedTypePath}");
                case 1: return matchingTypes.Single();
                default: throw new Exception($"Multiple request types found for: {command}{Environment.NewLine}Path used: {normalisedTypePath}");
            }
        }
    }
}