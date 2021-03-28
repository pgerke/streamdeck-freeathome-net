using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhilipGerke.StreamDeckFreeAtHome.Settings;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A stream deck action that shows the state of a binary switch and allows to toggle it.
    /// </summary>
    [PluginActionId("com.philipgerke.freeathome.binaryswitch")]
    public sealed class BinarySwitchPlugin : WebSocketPlugin
    {
        private const string PathDatapoints = "00000000-0000-0000-0000-000000000000.datapoints";
        private const string PathHttpResponse = "00000000-0000-0000-0000-000000000000.values[0]";                

        /// <summary>
        ///     Constructs a new <see cref="BinarySwitchPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public BinarySwitchPlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {            
            GetInitialData().ConfigureAwait(true);            
        }        

        /// <inheritdoc/>
        public override async void KeyPressed(KeyPayload payload)
        {
            base.KeyPressed(payload);
            async Task SetSwitchState(bool enabled)
            {
                HttpContent httpContent = new StringContent(enabled ? "1" : "0", null, "text/plain");
                HttpResponseMessage response = await httpClient.PutAsync(
                    $"http://{Settings.SysApIpAddress}/fhapi/v1/api/rest/datapoint/{Guid.Empty}/{Settings.DeviceId}.{Settings.Channel}.idp0000", httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to set switch state: " + await response.Content.ReadAsStringAsync());
                    await Connection.ShowAlert();
                }
            }

            switch (payload.State)
            {
                case 1:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Switching on");
                    await SetSwitchState(true);
                    return;
                case 2:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Switching on");
                    await SetSwitchState(false);
                    return;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "State is unknown!");
                    await Connection.ShowAlert();
                    await GetInitialData();
                    return;
            }
        }

        /// <inheritdoc/>
        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            base.ReceivedSettings(payload);
            GetInitialData().ConfigureAwait(true);
        }        

        private async Task GetInitialData()
        {
            // Nothing to do if host name, device ID or channel are not set.
            if (string.IsNullOrWhiteSpace(Settings.SysApIpAddress)
                || string.IsNullOrWhiteSpace(Settings.DeviceId) 
                || string.IsNullOrWhiteSpace(Settings.Channel))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot process message as mandatory device and channel data is missing.");
                return;
            }

            HttpResponseMessage response = await httpClient.GetAsync(
                $"http://{Settings.SysApIpAddress}/fhapi/v1/api/rest/datapoint/{Guid.Empty}/{Settings.DeviceId}.{Settings.Channel}.odp0000");
            if (!response.IsSuccessStatusCode)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Connection test failed: " + await response.Content.ReadAsStringAsync());
                await Connection.ShowAlert();
                return;
            }

            // Deserialize message
            using StreamReader streamReader = new(await response.Content.ReadAsStreamAsync());
            using JsonReader jsonReader = new JsonTextReader(streamReader);
            JToken token = await JToken.ReadFromAsync(jsonReader);

            // Extract and process value
            if(token.SelectToken(PathHttpResponse) is not JValue value || value == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot process response to HTTP request.");
                return;
            }

            await ProcessValue(value.Value<int>());
        }

        /// <inheritdoc/>
        protected override async void ProcessMessage(ResponseMessage message)
        {
            // Nothing to do if device ID or channel are not set.
            if (string.IsNullOrWhiteSpace(Settings.DeviceId) || string.IsNullOrWhiteSpace(Settings.Channel))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot process message as mandatory device and channel data is missing.");
                return;
            }

            // Ignore non-text messages
            if (message.MessageType != WebSocketMessageType.Text)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Ignored non-text message from web socket.");
                return;
            }

            // Deserialize message
            JObject obj = JObject.Parse(message.Text);

            // Ignore message without datapoints
            JToken datapoints = obj.SelectToken(PathDatapoints);
            if (datapoints == null || !datapoints.HasValues)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "The message contains no data points.");
                return;
            }

            // Check if relevant datapoints are included
            JProperty datapoint = datapoints.Children().FirstOrDefault(e => e.Type == JTokenType.Property &&
                    (e as JProperty).Name.Equals($"{Settings.DeviceId}/{Settings.Channel}/odp0000", StringComparison.InvariantCultureIgnoreCase)) as JProperty;
            if (datapoint == null)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "The message contains no relevant data points.");
                return;
            }

            await ProcessValue(datapoint.First.Value<int>());
        }

        private async Task ProcessValue(int value)
        {
            switch (value)
            {
                case 0:
                    await Connection.SetStateAsync(1);
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{Settings.DeviceId} {Settings.Channel} is now off.");
                    break;
                case 1:
                    await Connection.SetStateAsync(2);
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{Settings.DeviceId} {Settings.Channel} is now on.");
                    break;
                default:
                    await Connection.SetStateAsync(0);
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Settings.DeviceId} {Settings.Channel} has an unknown value.");
                    break;
            }
        }        
    }
}
