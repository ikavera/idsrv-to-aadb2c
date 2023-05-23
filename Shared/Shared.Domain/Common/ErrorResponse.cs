using System;
using System.Runtime.Serialization;

namespace Shared.Domain.Common
{
    [DataContract]
    public class ErrorResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponse"/> class.
        /// </summary>        
        /// <param name="errorCode">The PPS error code.</param>
        /// <param name="errorDescription">A description of the error.</param>        
        public ErrorResponse(string errorCode, string errorDescription)
        {
            if (string.IsNullOrEmpty(errorCode))
                throw new ArgumentNullException("errorCode");

            ErrorCode = errorCode;
            ErrorDescription = errorDescription;
        }

        /// <summary>
        /// Gets or sets the PPS error code for the error.
        /// </summary>
        /// <value>The PPS error code.</value>
        [DataMember]
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the description of the error.
        /// </summary>
        /// <value>The description of the error.</value>
        [DataMember]
        public string ErrorDescription { get; set; }


        /// <summary>
        /// Override of ToString() implementation
        /// </summary>
        /// <returns>A formatted string containing both error code and description for error response</returns>
        public override string ToString()
        {
            return $"[ErrorCode='{ErrorCode}', ErrorDescription='{ErrorDescription}']";
        }

    }
}
