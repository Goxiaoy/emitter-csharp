using System.Collections;

namespace Emitter.Messages
{
    public class LinkRequest
    {
        /// <summary>
        ///     Gets or sets the target channel for the requested key.
        /// </summary>
        public string Channel;

        /// <summary>
        ///     Gets or sets the secret key for this request.
        /// </summary>
        public string Key;

        /// <summary>
        ///     Gets or sets the name of the link.
        /// </summary>
        public string Name;

        /// <summary>
        ///     Gets or sets whether to subscribe automatically upon the creation of the link.
        /// </summary>
        public bool Subscribe;

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
                { "name", Name },
                { "subscribe", Subscribe }
            });
        }
    }
}