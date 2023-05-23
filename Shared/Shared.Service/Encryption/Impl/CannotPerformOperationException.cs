using System;
using System.Runtime.Serialization;

namespace Auth.Service.Encryption.Impl
{
    [Serializable]
    public class CannotPerformOperationException : Exception
    {
        public CannotPerformOperationException() { }
        public CannotPerformOperationException(string message)
            : base(message)
        { }
        public CannotPerformOperationException(string message, Exception inner)
            : base(message, inner)
        { }
        protected CannotPerformOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

}
