using System.Runtime.Serialization;

namespace Shared.Domain.Common
{
    [DataContract]
    public static class ErrorCodes
    {
        public const string AccessDenied = "access_denied";
        public const string ServerError = "server_error";
        public const string LimitExceeded = "limit_exceeded";

        public const string KeyExpired = "key_expired";
        public const string KeyInvalid = "key_invalid_issuer";
        public const string KeyMissing = "key_missing";

        public const string NotFound = "not_found";
        public const string AlreadyConfirmed = "already_confirmed";

    }
}
