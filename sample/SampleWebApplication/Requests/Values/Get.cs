using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Reyno.AspNetCore.CommandR;

namespace SampleWebApplication.Requests.Values {

    public class GetRequest : IRequest<IEnumerable<string>> {
        public int Id { get; set; }
    }

    public class GetRequestAuthorizer : RequestAuthorizer<GetRequest> {
        public override Task<AuthorizeResult> Authorize(GetRequest request, HttpContext context) {
            return Task.FromResult(
                Succeed()
                );
        }
    }

    public class GetRequestValidator : RequestValidator<GetRequest> {
        public GetRequestValidator() {
            // Rules
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class GetRequestHandler : AsyncRequestHandler<GetRequest, IEnumerable<string>> {

        protected override Task<IEnumerable<string>> HandleCore(GetRequest request) {

            var results = new[] { "Value1", "Value2", "Value3", "Value4", "Value5" };

            return Task.FromResult(results.AsEnumerable());

        }

    }

}
