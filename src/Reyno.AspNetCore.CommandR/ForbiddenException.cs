using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Reyno.AspNetCore.CommandR {

    [Serializable]
    public class ForbiddenException : Exception {

        public IEnumerable<string> Messages { get; private set; }

        public ForbiddenException() {
        }

        public ForbiddenException(IEnumerable<string> messages) {
            Messages = messages;
        }

        public ForbiddenException(string message) : base(message) {
        }

        public ForbiddenException(string message, Exception innerException) : base(message, innerException) {
        }

        protected ForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}