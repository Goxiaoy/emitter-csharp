using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Emitter.Messages;
using Emitter.Utility;

namespace Emitter
{
    public partial class Connection
    {
        #region KeyGen Members

        /// <summary>
        ///     Hashtable used for processing keygen responses.
        /// </summary>
        private readonly Hashtable KeygenHandlers = new Hashtable();

        /// <summary>
        ///     Asynchronously sends a key generation request to the emitter.io service.
        /// </summary>
        /// <param name="secretKey">The secret key for this request.</param>
        /// <param name="channel">The target channel for the requested key.</param>
        /// <param name="securityAccess">The security access of the requested key.</param>
        public async Task GenerateKey(string secretKey, string channel, SecurityAccess securityAccess,
            KeygenHandler handler)
        {
            await GenerateKey(secretKey, channel, securityAccess, 0, handler);
        }

        /// <summary>
        ///     Asynchronously sends a key generation request to the emitter.io service.
        /// </summary>
        /// <param name="secretKey">The secret key for this request.</param>
        /// <param name="channel">The target channel for the requested key.</param>
        /// <param name="securityAccess">The security access of the requested key.</param>
        /// <param name="ttl">The number of seconds for which this key will be usable.</param>
        /// <param name="handler"></param>
        public async Task GenerateKey(string secretKey, string channel, SecurityAccess securityAccess, int ttl,
            KeygenHandler handler)
        {
            // Prepare the request
            var request = new KeygenRequest();
            request.Key = secretKey;
            request.Channel = channel;
            request.Type = securityAccess;
            request.Ttl = ttl;

            //this.Client.Subscribe(new string[] { "emitter/keygen/" }, new byte[] { 0 });

            // Serialize and publish the request
            var id = await Publish("emitter/", "keygen/", Encoding.UTF8.GetBytes(request.ToJson()), false,
                Options.QoS1);
            // Register the handler
            if (id != null) KeygenHandlers[id] = handler;
        }

        #endregion KeyGen Members
    }
}