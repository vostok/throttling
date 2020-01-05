using JetBrains.Annotations;

namespace Vostok.Throttling
{
    [PublicAPI]
    public static class WellKnownThrottlingProperties
    {
        /// <summary>
        /// Name of the client application that sent the request.
        /// </summary>
        public const string Consumer = "consumer";

        /// <summary>
        /// Priority of the request. Typical values are <c>Critical</c>, <c>Ordinary</c> and <c>Sheddable</c>.
        /// </summary>
        public const string Priority = "priority";

        /// <summary>
        /// HTTP request method.
        /// </summary>
        public const string Method = "method";
        
        /// <summary>
        /// HTTP request url.
        /// </summary>
        public const string Url = "url";
    }
}
