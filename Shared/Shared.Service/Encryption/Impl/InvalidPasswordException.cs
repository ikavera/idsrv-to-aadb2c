﻿using System;
using System.Runtime.Serialization;

namespace Auth.Service.Encryption.Impl
{
    [Serializable]
    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException() { }
        public InvalidPasswordException(string message)
            : base(message)
        { }
        public InvalidPasswordException(string message, Exception inner)
            : base(message, inner)
        { }
        protected InvalidPasswordException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
