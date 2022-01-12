using System.Text;
using System.Threading.Tasks;
using Emitter.Messages;
using Emitter.Utility;
using MQTTnet.Extensions.ManagedClient;

namespace Emitter
{
    public partial class Connection
    {
        /// <summary>
        ///     Creates a short 2-character link for the channel. Uses the default key that should be specified in the constructor.
        /// </summary>
        /// <param name="channel">The channel to link to.</param>
        /// <param name="name">The name of the link to create.</param>
        /// <param name="subscribe">Automatically subscribe to this link.</param>
        /// <param name="options">Channel options to associate with the link (ex: ttl)</param>
        public Task Link(string channel, string name, bool subscribe, params string[] options)
        {
            if (DefaultKey == null)
                throw EmitterException.NoDefaultKey;

            return Link(DefaultKey, channel, name, subscribe, options);
        }

        /// <summary>
        ///     Creates a short 2-character link for the channel. Uses the default key that should be specified in the constructor.
        /// </summary>
        /// <param name="key">The channel key.</param>
        /// <param name="channel">The channel to link to.</param>
        /// <param name="name">The name of the link to create.</param>
        /// <param name="subscribe">Automatically subscribe to this link.</param>
        /// <param name="options">Channel options to associate with the link (ex: ttl)</param>
        public async Task Link(string key, string channel, string name, bool subscribe, params string[] options)
        {
            var request = new LinkRequest();
            request.Key = key;
            request.Channel = FormatChannelLink(channel, options);
            request.Name = name;
            request.Subscribe = subscribe;

            await Publish("emitter/", "link", Encoding.UTF8.GetBytes(request.ToJson()), true, Options.QoS1);
        }

        /// <summary>
        ///     Publishes to a link.
        /// </summary>
        /// <param name="name">The name of the link.</param>
        /// <param name="message">The message to publish.</param>
        /// <returns></returns>
        public async Task<ushort?> PublishWithLink(string name, byte[] message)
        {
            var ret = await Client.PublishAsync(name, Encoding.UTF8.GetString(message));
            return ret.PacketIdentifier;
        }


        /// <summary>
        ///     Publishes to a link.
        /// </summary>
        /// <param name="name">The name of the link.</param>
        /// <param name="message">The message to publish.</param>
        /// <returns></returns>
        public async Task<ushort?> PublishWithLink(string name, string message)
        {
            var ret = await Client.PublishAsync(name, message);
            return ret.PacketIdentifier;
        }
    }
}