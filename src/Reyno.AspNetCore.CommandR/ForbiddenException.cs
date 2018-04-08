using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Reyno.AspNetCore.CommandR {

    [Serializable]
    public class ForbiddenException : Exception {

        protected ForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public ForbiddenException() {
        }

        public ForbiddenException(IEnumerable<string> messages) {
            Messages = messages;
        }

        public ForbiddenException(string message) : base(message) {
        }

        public ForbiddenException(string message, Exception innerException) : base(message, innerException) {
        }

        public IEnumerable<string> Messages { get; private set; }
    }
}