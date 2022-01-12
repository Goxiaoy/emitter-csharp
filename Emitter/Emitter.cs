/*
Copyright (c) 2016 Roman Atachiants

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution.

The Eclipse Public License:  http://www.eclipse.org/legal/epl-v10.html
The Eclipse Distribution License: http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Roman Atachiants - integrating with emitter.io
*/

using System;
using System.Threading.Tasks;
using Emitter.Messages;
using Emitter.Utility;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Emitter
{
    /// <summary>
    ///     Represents a message handler callback.
    /// </summary>
    public delegate Task MessageHandler(string channel, byte[] message);

    /// <summary>
    ///     Delegate that defines event handler for client/peer disconnection
    /// </summary>
    public delegate Task DisconnectHandler(object sender, MqttClientDisconnectedEventArgs e);

    /// <summary>
    ///     Delegate that defines event handler for client/peer disconnection
    /// </summary>
    public delegate Task ConnectHandler(object sender, MqttClientConnectedEventArgs e);

    /// <summary>
    ///     Delegate that defines an event handler for an error.
    /// </summary>
    public delegate Task ErrorHandler(object sender, Exception e);

    /// <summary>
    ///     Represents emitter.io MQTT-based client.
    /// </summary>
    public partial class Connection : IDisposable
    {
        #region Static Members

        /// <summary>
        ///     Establishes a new connection by creating the connection instance and connecting to it.
        /// </summary>
        /// <param name="broker">The broker hostname to use.</param>
        /// <param name="brokerPort">The broker port to use.</param>
        /// <param name="secure">Whether its a secure connection.</param>
        /// <param name="defaultKey">The default key to use.</param>
        /// <param name="username"></param>
        /// <returns>The connection state.</returns>
        public static async Task<Connection> Establish(string defaultKey = null, string broker = null,
            int? brokerPort = null, bool secure = true, string username = null)
        {
            // Create the connection
            var conn = new Connection(defaultKey, broker, brokerPort, secure, username: username);

            // Connect
            await conn.Connect();

            // Return it
            return conn;
        }

        #endregion Static Members

        /// <summary>
        ///     Occurs when a message is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs message)
        {
            var e = message.ApplicationMessage;
            try
            {
                if (!e.Topic.StartsWith("emitter"))
                {
                    var handlers = Trie.Match(e.Topic);
                    // Invoke every handler matching the channel
                    foreach (MessageHandler handler in handlers)
                        await handler(e.Topic, e.Payload);

                    if (handlers.Count == 0)
                        if (DefaultMessageHandler != null)
                            await DefaultMessageHandler(e.Topic, e.Payload);

                    return;
                }

                // Did we receive a keygen response?
                if (e.Topic == "emitter/keygen/")
                {
                    // Deserialize the response
                    var response = KeygenResponse.FromBinary(e.Payload);
                    if (response == null || response.Status != 200)
                    {
                        await InvokeError(response.Status);
                        return;
                    }

                    // Call the response handler we have registered previously
                    if (response.RequestId != null && KeygenHandlers.ContainsKey((ushort)response.RequestId))
                        await ((KeygenHandler)KeygenHandlers[(ushort)response.RequestId])(response);
                    return;
                }

                if (e.Topic == "emitter/presence/")
                {
                    var presenceEvent = PresenceEvent.FromBinary(e.Payload);

                    var handlers = PresenceTrie.Match(presenceEvent.Channel);
                    // Invoke every handler matching the channel
                    foreach (PresenceHandler handler in handlers)
                        await handler(presenceEvent);

                    if (handlers.Count == 0)
                        if (DefaultMessageHandler != null)
                            await DefaultPresenceHandler(presenceEvent);

                    return;
                }

                if (e.Topic == "emitter/error/")
                {
                    var errorEvent = ErrorEvent.FromBinary(e.Payload);
                    var emitterException =
                        new EmitterException((EmitterEventCode)errorEvent.Status, errorEvent.Message);

                    await InvokeError(emitterException);
                    return;
                }

                if (e.Topic == "emitter/me/")
                {
                    var meResponse = MeResponse.FromBinary(e.Payload);
                    if (Me != null) await Me(meResponse);
                }
            }
            catch (Exception ex)
            {
                await InvokeError(ex);
            }
        }

        #region Constructors

        private readonly IManagedMqttClient Client;
        private readonly ReverseTrie<MessageHandler> Trie = new ReverseTrie<MessageHandler>(-1);
        private readonly string DefaultKey;

        private readonly ManagedMqttClientOptionsBuilder _optionsBuilder;

        /// <summary>
        ///     Constructs a new emitter.io connection.
        /// </summary>
        public Connection() : this(null, null, 0)
        {
        }

        /// <summary>
        ///     Constructs a new emitter.io connection.
        /// </summary>
        /// <param name="defaultKey">The default key to use.</param>
        public Connection(string defaultKey) : this(defaultKey, null, 0)
        {
        }

        /// <summary>
        ///     Constructs a new emitter.io connection.
        /// </summary>
        /// <param name="defaultKey">The default key to use.</param>
        /// <param name="broker">The address of the broker.</param>
        /// <param name="secure">Whether the connection has to be secure.</param>
        public Connection(string defaultKey, string broker, bool secure = true) : this(defaultKey, broker, 0, secure)
        {
        }

        /// <summary>
        ///     Constructs a new emitter.io connection.
        /// </summary>
        /// <param name="defaultKey">The default key to use.</param>
        /// <param name="broker">The address of the broker.</param>
        /// <param name="brokerPort">The port of the broker to use.</param>
        /// <param name="secure">Whether the connection has to be secure.</param>
        /// <param name="websocket"></param>
        /// <param name="username"></param>
        /// <param name="autoReconnectSeconds">Default to 2 seconds</param>
        /// <param name="logger"></param>
        public Connection(string defaultKey, string broker, int? brokerPort = null, bool secure = true,
            bool websocket = false, string username = null, int autoReconnectSeconds = 2, IMqttNetLogger logger = null)
        {
            if (broker == null)
            {
                broker = "api.emitter.io";
            }

            if (!brokerPort.HasValue || brokerPort.Value <= 0)
            {
                brokerPort = secure ? 443 : 8080;
            }

            DefaultKey = defaultKey;

            var options = new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V311);
            options = !websocket ? options.WithTcpServer(broker, brokerPort) : options.WithWebSocketServer(broker);
            options = username != null ? options.WithCredentials(username, (string)null) : options;
            if (secure) options = options.WithTls();

            _optionsBuilder = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(autoReconnectSeconds))
                .WithClientOptions(options);

            var factory = new MqttFactory();
            if (logger == null)
                Client = factory.CreateManagedMqttClient();
            else
                Client = factory.CreateManagedMqttClient(logger);

            Client.UseDisconnectedHandler(async args => { await OnDisconnect(this, args); });
            Client.UseConnectedHandler(async args => { await OnConnected(this, args); });

            Client.UseApplicationMessageReceivedHandler(OnMessageReceived);
        }

        #endregion Constructors

        #region Error Members

        /// <summary>
        ///     Occurs when an error occurs.
        /// </summary>
        public event ErrorHandler Error;

        /// <summary>
        ///     Invokes the error handler.
        /// </summary>
        /// <param name="ex"></param>
        private async Task InvokeError(Exception ex)
        {
            if (Error != null) await Error(this, ex);
        }

        /// <summary>
        ///     Invokes the error handler.
        /// </summary>
        /// <param name="status"></param>
        private async Task InvokeError(int status)
        {
            if (status == 200)
                return;

            await InvokeError(EmitterException.FromStatus(status));
        }

        #endregion Error Members

        #region Connect / Disconnect Members

        /// <summary>
        ///     Occurs when the client was disconnected.
        /// </summary>
        public event DisconnectHandler Disconnected;

        /// <summary>
        ///     Occurs when the client was disconnected.
        /// </summary>
        public event ConnectHandler Connected;

        /// <summary>
        ///     Gets whether the client is connected or not.
        /// </summary>
        public bool IsConnected => Client.IsConnected;

        /// <summary>
        ///     Occurs when the connection was closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task OnDisconnect(object sender, MqttClientDisconnectedEventArgs e)
        {
            if (Disconnected != null) await Disconnected(sender, e);
        }

        private async Task OnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            if (Connected != null) await Connected(sender, e);
        }

        /// <summary>
        ///     Connects the emitter.io service.
        /// </summary>
        public async Task Connect()
        {
            await Client.StartAsync(_optionsBuilder.Build());
        }

        /// <summary>
        ///     Disconnects from emitter.io service.
        /// </summary>
        public Task Disconnect()
        {
            return Client.StopAsync();
        }

        #endregion Connect / Disconnect Members

        #region Private Members

        private string FormatOptions(string[] options)
        {
            var formatted = "";
            if (options != null && options.Length > 0)
            {
                formatted += "?";
                for (var i = 0; i < options.Length; ++i)
                {
                    if (options[i][0] == '+')
                        continue;

                    formatted += options[i];
                    if (i + 1 < options.Length)
                        formatted += "&";
                }
            }

            return formatted;
        }

        /// <summary>
        ///     Formats the channel.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="channel">The channel name.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        private string FormatChannel(string key, string channel, params string[] options)
        {
            var k = key.Trim('/');
            var c = channel.Trim('/');
            var o = FormatOptions(options);

            var formatted = string.Format("{0}/{1}/{2}", k, c, o);

            return formatted;
        }

        private string FormatChannelLink(string channel, params string[] options)
        {
            var c = channel.Trim('/');
            var o = FormatOptions(options);

            var formatted = string.Format("{0}/{1}", c, o);

            return formatted;
        }

        private string FormatChannelShare(string key, string channel, string shareGroup, params string[] options)
        {
            var k = key.Trim('/');
            var c = channel.Trim('/');
            var s = shareGroup.Trim('/');
            var o = FormatOptions(options);

            var formatted = string.Format("{0}/$share/{1}/{2}/{3}", k, s, c, o);

            return formatted;
        }

        private void GetHeader(string[] options, out bool retain, out MqttQualityOfServiceLevel qos)
        {
            retain = false;
            qos = MqttQualityOfServiceLevel.AtMostOnce;
            foreach (var o in options)
                switch (o)
                {
                    case Options.Retain:
                        retain = true;
                        break;
                    case Options.QoS1:
                        qos = MqttQualityOfServiceLevel.AtLeastOnce;
                        break;
                }
        }

        #endregion Private Members

        #region IDisposable

        /// <summary>
        ///     Disposes the connection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Disposes the connection.
        /// </summary>
        /// <param name="disposing">Whether we are disposing or finalizing.</param>
        protected void Dispose(bool disposing)
        {
            try
            {
                Disconnect();
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Finalizes the connection.
        /// </summary>
        ~Connection()
        {
            Dispose(false);
        }

        #endregion IDisposable
    }
}