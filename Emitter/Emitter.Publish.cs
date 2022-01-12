using System.Text;
using System.Threading.Tasks;
using Emitter.Utility;
using MQTTnet.Client.Publishing;
using MQTTnet.Extensions.ManagedClient;

namespace Emitter
{
    public partial class Connection
    {
        #region Publish Members

        /// <summary>
        ///     Asynchonously publishes a message to the emitter.io service. Uses the default
        ///     key that should be specified in the constructor.
        /// </summary>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <returns>The message identifier for this operation.</returns>
        public async Task<ushort?> Publish(string channel, byte[] message)
        {
            return await Publish(channel, Encoding.UTF8.GetString(message));
        }

        /// <summary>
        ///     Asynchonously publishes a message to the emitter.io service. Uses the default
        ///     key that should be specified in the constructor.
        /// </summary>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <returns>The message identifier for this operation.</returns>
        public async Task<ushort?> Publish(string channel, string message)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;
            return await Publish(DefaultKey, channel, message);
        }

        /// <summary>
        ///     Publishes a message to the emitter.io service asynchronously.
        /// </summary>
        /// <param name="key">The key to use for this publish request.</param>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <returns>The message identifier.</returns>
        public async Task<ushort?> Publish(string key, string channel, string message)
        {
            var ret = await Client.PublishAsync(FormatChannel(key, channel), message);
            return ret.PacketIdentifier;
        }

        /// <summary>
        ///     Publishes a message to the emitter.io service asynchronously.
        /// </summary>
        /// <param name="key">The key to use for this publish request.</param>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <returns>The message identifier.</returns>
        public async Task<ushort?> Publish(string key, string channel, byte[] message)
        {
            return await Publish(key, channel, Encoding.UTF8.GetString(message));
        }

        /// <summary>
        ///     Publishes a message to the emitter.io service asynchronously.
        /// </summary>
        /// <param name="key">The key to use for this publish request.</param>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <param name="ttl">The time to live of the message.</param>
        /// <returns>The message identifier.</returns>
        public async Task<ushort?> Publish(string key, string channel, string message, int ttl, bool queue = true)
        {
            MqttClientPublishResult ret = null;
            if (queue)
                ret = await Client.PublishAsync(FormatChannel(key, channel, Options.WithTTL(ttl)));
            else
                ret = await Client.InternalClient.PublishAsync(FormatChannel(key, channel, Options.WithTTL(ttl)));
            return ret.PacketIdentifier;
        }

        /// <summary>
        ///     Publishes a message to the emitter.io service asynchronously.
        /// </summary>
        /// <param name="key">The key to use for this publish request.</param>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <param name="ttl">The time to live of the message.</param>
        /// <returns>The message identifier.</returns>
        public async Task<ushort?> Publish(string key, string channel, byte[] message, int ttl, bool queue = true)
        {
            return await Publish(key, channel, Encoding.UTF8.GetString(message), ttl, queue);
        }

        /// <summary>
        ///     Publishes a message to the emitter.io service asynchronously.
        /// </summary>
        /// <param name="key">The key to use for this publish request.</param>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <param name="options">The options associated with the message. Ex: Options.WithLast(5).</param>
        /// <returns>The message identifier.</returns>
        public async Task<ushort?> Publish(string key, string channel, string message, bool queue = true,
            params string[] options)
        {
            GetHeader(options, out var retain, out var qos);
            MqttClientPublishResult ret = null;
            if (queue)
                ret = await Client.PublishAsync(FormatChannel(key, channel, options), message, qos, retain);
            else
                ret = await Client.InternalClient.PublishAsync(FormatChannel(key, channel, options), message, qos,
                    retain);
            return ret.PacketIdentifier;
        }

        /// <summary>
        ///     Publishes a message to the emitter.io service asynchronously.
        /// </summary>
        /// <param name="key">The key to use for this publish request.</param>
        /// <param name="channel">The channel to publish to.</param>
        /// <param name="message">The message body to send.</param>
        /// <param name="queue"></param>
        /// <param name="options">The options associated with the message. Ex: Options.WithoutEcho().</param>
        /// <returns>The message identifier.</returns>
        public async Task<ushort?> Publish(string key, string channel, byte[] message, bool queue = true,
            params string[] options)
        {
            return await Publish(key, channel, Encoding.UTF8.GetString(message), queue, options);
        }

        #endregion Publish Members
    }
}