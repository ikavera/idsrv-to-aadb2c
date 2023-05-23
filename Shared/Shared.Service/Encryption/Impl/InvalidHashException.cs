using System;
using System.Runtime.Serialization;

namespace Auth.Service.Encryption.Impl
{
    [Serializable]
    public class InvalidHashException : Exception
    {
        public InvalidHashException() { }
        public InvalidHashException(string message)
            : base(message)
        { }
        public InvalidHashException(string message, Exception inner)
            : base(message, inner)
        { }
        protected InvalidHashException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
