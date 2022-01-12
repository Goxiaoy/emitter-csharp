using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Subscribing;

namespace Emitter
{
    public partial class Connection
    {
        #region Subscribe

        public event MessageHandler DefaultMessageHandler;

        /// <summary>
        ///     Asynchronously subscribes to a particular channel of emitter.io service. Uses the default
        ///     key that should be specified in the constructor.
        /// </summary>
        /// <param name="channel">The channel to subscribe to.</param>
        /// <param name="optionalHandler">The callback to be invoked every time the message is received.</param>
        /// <param name="options">The options of the channel. Ex: Options.WithLast(10)</param>
        /// ;
        /// <returns>The message identifier for this operation.</returns>
        public Task Subscribe(string channel, MessageHandler optionalHandler = null, bool recover = false,
            params string[] options)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;
            return Subscribe(DefaultKey, channel, optionalHandler, recover, options);
        }

        /// <summary>
        ///     Asynchronously subscribes to a particular channel of emitter.io service.
        /// </summary>
        /// <param name="key">The key to use for this subscription request.</param>
        /// <param name="channel">The channel to subscribe to.</param>
        /// <param name="optionalHandler">The callback to be invoked every time the message is received.</param>
        /// <param name="options">The options of the channel. Ex: Options.WithLast(10)</param>
        /// ;
        /// <returns>The message identifier for this operation.</returns>
        public async Task Subscribe(string key, string channel,
            MessageHandler optionalHandler = null, bool recover = false, params string[] options)
        {
            // Register the handler
            if (optionalHandler != null)
                Trie.RegisterHandler(channel, optionalHandler);
            if (recover)
                await Client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(FormatChannel(key, channel, options)).Build().TopicFilters);
            else
                await Client.InternalClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(FormatChannel(key, channel, options)).Build(), CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously subscribes to a particular share group for a channel of emitter.io service.
        /// </summary>
        /// <param name="key">The key to use for this subscription request.</param>
        /// <param name="channel">The channel to subscribe to.</param>
        /// <param name="shareGroup">The share group to join.</param>
        /// <param name="optionalHandler">The callback to be invoked every time the message is received.</param>
        /// <param name="options">The options of the channel. Ex: Options.WithLast(10)</param>
        /// ;
        /// <returns>The message identifier for this operation.</returns>
        public async Task SubscribeWithGroup(string key, string channel,
            string shareGroup, MessageHandler optionalHandler = null, bool recover = false, params string[] options)
        {
            // Register the handler
            if (optionalHandler != null)
                Trie.RegisterHandler(channel, optionalHandler);
            if (recover)
                await Client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(FormatChannelShare(key, channel, shareGroup, options)).Build().TopicFilters);
            else
                await Client.InternalClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(FormatChannelShare(key, channel, shareGroup, options)).Build(),
                    CancellationToken.None);
        }

        #endregion Subscribe

        #region Unsubscribe

        /// <summary>
        ///     Asynchonously unsubscribes from a particular channel of emitter.io service. Uses the default
        ///     key that should be specified in the constructor.
        /// </summary>
        /// <param name="channel">The channel to subscribe to.</param>
        /// <returns>The message identifier for this operation.</returns>
        public Task Unsubscribe(string channel)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;
            return Unsubscribe(DefaultKey, channel);
        }

        /// <summary>
        ///     Asynchonously unsubscribes from a particular channel of emitter.io service.
        /// </summary>
        /// <param name="key">The key to use for this unsubscription request.</param>
        /// <param name="channel">The channel to subscribe to.</param>
        /// <returns>The message identifier for this operation.</returns>
        public async Task Unsubscribe(string key, string channel)
        {
            // Unregister the handler
            Trie.UnregisterHandler(key);
            // Unsubscribe
            await Client.UnsubscribeAsync(
                new[] { FormatChannel(key, channel) });
        }

        #endregion Unsubscribe
    }
}