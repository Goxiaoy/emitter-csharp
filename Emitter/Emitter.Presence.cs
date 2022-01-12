using System.Text;
using System.Threading.Tasks;
using Emitter.Messages;
using Emitter.Utility;

namespace Emitter
{
    public partial class Connection
    {
        #region Presence Members

        /// <summary>
        ///     Represents a Presence handler callback.
        /// </summary>
        public delegate Task PresenceHandler(PresenceEvent presenceResponse);

        /// <summary>
        ///     Represents the default presence handler, called when there is no specific handler for the channel.
        /// </summary>
        public event PresenceHandler DefaultPresenceHandler;

        private readonly ReverseTrie<PresenceHandler> PresenceTrie = new ReverseTrie<PresenceHandler>(-1);

        /// <summary>
        ///     Subscribes to presence events using the default key. Optionaly requests a status of the channel.
        /// </summary>
        /// <param name="channel">The name of the channel</param>
        /// <param name="status">Whether to request a status of the channel.</param>
        /// <param name="optionalHandler">A specific handler to receive presence events for this channel.</param>
        public async Task PresenceSubscribe(string channel, bool status, PresenceHandler optionalHandler = null)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;

            await PresenceSubscribe(DefaultKey, channel, status, optionalHandler);
        }

        /// <summary>
        ///     Subscribes to presence events. Optionaly requests a status of the channel.
        /// </summary>
        /// <param name="key">The key for the channel</param>
        /// <param name="channel">The name of the channel</param>
        /// <param name="status">Whether to request a status of the channel.</param>
        /// <param name="optionalHandler">A specific handler to receive presence events for this channel.</param>
        public async Task PresenceSubscribe(string key, string channel, bool status,
            PresenceHandler optionalHandler = null)
        {
            if (optionalHandler != null)
                PresenceTrie.RegisterHandler(channel, optionalHandler);

            var request = new PresenceRequest();
            request.Key = key;
            request.Channel = channel;
            request.Status = status;
            request.Changes = true;

            await Publish("emitter/", "presence", Encoding.UTF8.GetBytes(request.ToJson()), true, Options.QoS1);
        }

        /// <summary>
        ///     Unsubsctibes to presence events for a channel using the default key.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        public async Task PresenceUnsubscribe(string channel)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;

            await PresenceUnsubscribe(DefaultKey, channel);
        }

        /// <summary>
        ///     Unsubsctibes to presence events for a channel.
        /// </summary>
        /// <param name="key">The key for the channel.</param>
        /// <param name="channel">The name of the channel.</param>
        public async Task PresenceUnsubscribe(string key, string channel)
        {
            PresenceTrie.UnregisterHandler(channel);

            var request = new PresenceRequest();
            request.Key = key;
            request.Channel = channel;
            request.Status = false;
            request.Changes = false;

            await Publish("emitter/", "presence", Encoding.UTF8.GetBytes(request.ToJson()));
        }

        /// <summary>
        ///     Requests a status of the channel using the default key.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="optionalHandler">An optional handler, specific to this channel.</param>
        public async Task PresenceStatus(string channel, PresenceHandler optionalHandler)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;

            await PresenceStatus(DefaultKey, channel, optionalHandler);
        }

        /// <summary>
        ///     Requests a status of the channel using the default key.
        /// </summary>
        /// <param name="key">The key for the channel.</param>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="optionalHandler">An optional handler, specific to this channel.</param>
        public async Task PresenceStatus(string key, string channel, PresenceHandler optionalHandler)
        {
            if (optionalHandler != null)
                PresenceTrie.RegisterHandler(channel, optionalHandler);

            var request = new PresenceRequest();
            request.Key = key;
            request.Channel = channel;
            request.Status = true;
            request.Changes = null;

            await Publish("emitter/", "presence", Encoding.UTF8.GetBytes(request.ToJson()));
        }

        #endregion Presence Members
    }
}