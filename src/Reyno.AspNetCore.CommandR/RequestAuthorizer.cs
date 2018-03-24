using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Reyno.AspNetCore.CommandR {
    public abstract class RequestAuthorizer<TRequest>
    {
        public abstract Task Authorize(TRequest request, HttpContext context);
    }
}
