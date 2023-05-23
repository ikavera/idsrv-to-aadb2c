using System;
using System.Runtime.Serialization;

namespace Shared.Domain.Exceptions
{
    // We use custom exception for generating unique message for UI (throw them in controller and handle in middleware)
    [Serializable]
    public class CustomMessageException : Exception
    {
        public string Status { get; set; }
        public string Action { get; set; }
        public CustomMessageException(string message, Exception inner, string status, string action)
            : base(message, inner)
        {
            Status = status;
            Action = action;
        }
        public CustomMessageException(string message)
            : base(message)
        {

        }
        public CustomMessageException(string message, Exception inner)
        : base(message, inner) { }
        protected CustomMessageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
