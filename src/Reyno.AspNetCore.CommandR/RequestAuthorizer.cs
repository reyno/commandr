using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Reyno.AspNetCore.CommandR {
    public abstract class RequestAuthorizer<TRequest> {

        public abstract Task<AuthorizeResult> Authorize(TRequest request, HttpContext context);

        public AuthorizeResult Forbid(string message) => new AuthorizeResult {
            Message = message,
            Authorized = false
        };

        public AuthorizeResult Succeed() => new AuthorizeResult {
            Authorized = true
        };

    }

    public class AuthorizeResult {
        public string Message { get; set; }
        public bool Authorized { get; set; }
    }

}
