using System.Threading.Tasks;
using Emitter.Messages;
using Emitter.Utility;

namespace Emitter
{
    public partial class Connection
    {
        /// <summary>
        ///     Represents a Me event handler.
        /// </summary>
        /// <param name="meResponse"></param>
        public delegate Task MeHandler(MeResponse meResponse);

        public event MeHandler Me;

        /// <summary>
        ///     Requests information on the current connection.
        /// </summary>
        public Task MeInfo()
        {
            return Publish("emitter/", "me", new byte[] { }, true, Options.QoS1);
        }
    }
}