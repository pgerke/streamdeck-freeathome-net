using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using PhilipGerke.StreamDeckFreeAtHome.Settings;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A stream deck action that verifies the connection to a system access point.
    /// </summary>
    [PluginActionId("com.philipgerke.freeathome.test")]
    public sealed class FreeAtHomeConnectionTestPlugin : PluginBase
    {
        private readonly ConnectionTestSettings settings;
        private static readonly HttpClient httpClient = new();

        /// <summary>
        ///     Constructs a new <see cref="FreeAtHomeConnectionTestPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public FreeAtHomeConnectionTestPlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            // Deserialize settings or create a new instance.
            settings = (payload.Settings == null || payload.Settings.Count == 0)
                ? new ConnectionTestSettings() 
                : payload.Settings.ToObject<ConnectionTestSettings>();
            SetAuthenticationHeader();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
        }

        /// <inheritdoc/>
        public override async void KeyPressed(KeyPayload payload)
        {

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Key Pressed: Connection Test");
            HttpResponseMessage response = await httpClient.GetAsync($"http://{settings.SysApIpAddress}/fhapi/v1/api/rest/devicelist");
            if(response.IsSuccessStatusCode)
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

        /// <inheritdoc/>
        public override void KeyReleased(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Key Released: Connection Test");
        }

        /// <inheritdoc/>
        public override void OnTick()
        {
        }

        /// <inheritdoc/>
        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        /// <inheritdoc/>
        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Received Settings: Connection Test");
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
            SetAuthenticationHeader();
        }

        private void SetAuthenticationHeader()
        {
            if(string.IsNullOrWhiteSpace(settings.SysApUserID) || string.IsNullOrWhiteSpace(settings.SysApPassword))
            {
                return;
            }

            byte[] bytes = Encoding.ASCII.GetBytes($"{settings.SysApUserID}:{settings.SysApPassword}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
