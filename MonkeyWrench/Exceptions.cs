
using System;

namespace MonkeyWrench {
	/**
	 * Thrown when a user does not have access to a resource.
	 */
	public class UnauthorizedException : Exception {
		public UnauthorizedException () {}
		public UnauthorizedException (string message) : base (message) {}
		public UnauthorizedException (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (info, context) {}
		public UnauthorizedException (string message, Exception innerException) : base (message, innerException) {}
	}
}