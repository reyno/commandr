using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Reyno.AspNetCore.CommandR {

    public class AuthorizeResult {

        public bool Authorized { get; set; }

        public string Message { get; set; }
    }

    public abstract class RequestAuthorizer<TRequest> {

        public abstract Task<AuthorizeResult> Authorize(TRequest request, HttpContext context);

        public AuthorizeResult Forbid(string message = null) => new AuthorizeResult {
            Message = message,
            Authorized = false
        };

        public AuthorizeResult Succeed() => new AuthorizeResult {
            Authorized = true
        };
    }
}