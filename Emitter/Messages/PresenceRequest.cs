using System.Collections;

namespace Emitter.Messages
{
    /// <summary>
    ///     Represents a Presence request.
    /// </summary>
    public class PresenceRequest
    {
        /// <summary>
        ///     Get or sets whether we want to receive change events.
        /// </summary>
        public bool? Changes;

        /// <summary>
        ///     Gets or sets the target channel for the requested key.
        /// </summary>
        public string Channel;

        /// <summary>
        ///     Gets or sets the secret key for this request.
        /// </summary>
        public string Key;

        /// <summary>
        ///     Gets or sets whether we want to receive a full status.
        /// </summary>
        public bool Status;

        /// <summary>
        ///     Converts the request to JSON format.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonSerializer.SerializeObject(new Hashtable
            {
                { "key", Key },
                { "channel", Channel },
                { "status", Status },
                { "changes", Changes }
            });
        }
    }
}