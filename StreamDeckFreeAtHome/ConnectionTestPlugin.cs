using BarRaider.SdTools;
using PhilipGerke.StreamDeckFreeAtHome.Settings;
using System.Net.Http;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A stream deck action that verifies the connection to a system access point.
    /// </summary>
    [PluginActionId("com.philipgerke.freeathome.test")]
    public sealed class ConnectionTestPlugin : BasePlugin<ConnectionSettings>
    {
        /// <summary>
        ///     Constructs a new <see cref="ConnectionTestPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public ConnectionTestPlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
        }

        /// <inheritdoc/>
        public override async void KeyPressed(KeyPayload payload)
        {

            HttpResponseMessage response = await httpClient.GetAsync($"http://{Settings.SysApIpAddress}/fhapi/v1/api/rest/devicelist");
            if (response.IsSuccessStatusCode)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Connection test successful");
                await Connection.ShowOk();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Connection test failed: " + await response.Content.ReadAsStringAsync());
                await Connection.ShowAlert();
            }
        }
    }
}
