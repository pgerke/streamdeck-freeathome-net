using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Websocket.Client;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A stream deck action that shows the state of a dimmer and allows to toggle it.
    /// </summary>
    [PluginActionId("com.philipgerke.freeathome.dimmerstatus")]
    public sealed class DimmerStatusPlugin : WebSocketPlugin
    {
        private const string PathDatapoints = "00000000-0000-0000-0000-000000000000.datapoints";
        private const string PathHttpResponse = "00000000-0000-0000-0000-000000000000.values[0]";
        private uint currentState;
        private float currentBrightness = 0f;

        /// <summary>
        ///     Constructs a new <see cref="DimmerStatusPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public DimmerStatusPlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            currentState = payload.State;
            GetInitialData().ConfigureAwait(true);
        }

        /// <inheritdoc/>
        public override async void KeyPressed(KeyPayload payload)
        {
            base.KeyPressed(payload);
            currentState = (currentState + 1) % 3;
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
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Switching on");
                    await SetSwitchState(true);
                    return;
                case 2:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Switching on");
                    await SetSwitchState(false);
                    return;
                default:
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "State is unknown!");
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

        private async Task GetInitialData() => await Task.WhenAll(GetInitialDataBrightness(), GetInitialDataPowerState());

        private async Task GetInitialDataBrightness()
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
                $"http://{Settings.SysApIpAddress}/fhapi/v1/api/rest/datapoint/{Guid.Empty}/{Settings.DeviceId}.{Settings.Channel}.odp0001");
            if (!response.IsSuccessStatusCode)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Getting initial power state data failed: " + await response.Content.ReadAsStringAsync());
                await Connection.ShowAlert();
                return;
            }

            // Deserialize message
            using StreamReader streamReader = new(await response.Content.ReadAsStreamAsync());
            using JsonReader jsonReader = new JsonTextReader(streamReader);
            JToken token = await JToken.ReadFromAsync(jsonReader);

            // Extract and process value
            if (token.SelectToken(PathHttpResponse) is not JValue value || value == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot process response to HTTP request.");
                return;
            }

            await ProcessBrightness(value.Value<float>());
        }

        private async Task GetInitialDataPowerState()
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
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Getting initial power state data failed: " + await response.Content.ReadAsStringAsync());
                await Connection.ShowAlert();
                return;
            }

            // Deserialize message
            using StreamReader streamReader = new(await response.Content.ReadAsStreamAsync());
            using JsonReader jsonReader = new JsonTextReader(streamReader);
            JToken token = await JToken.ReadFromAsync(jsonReader);

            // Extract and process value
            if (token.SelectToken(PathHttpResponse) is not JValue value || value == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot process response to HTTP request.");
                return;
            }

            await ProcessState(value.Value<int>());
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
                    ((e as JProperty).Name.Equals($"{Settings.DeviceId}/{Settings.Channel}/odp0000", StringComparison.InvariantCultureIgnoreCase)
                    || (e as JProperty).Name.Equals($"{Settings.DeviceId}/{Settings.Channel}/odp0001", StringComparison.InvariantCultureIgnoreCase))) as JProperty;
            if (datapoint == null)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "The message contains no relevant data points.");
                return;
            }

            if (datapoint.Name.EndsWith("/odp0000"))
            {
                await ProcessState(datapoint.First.Value<int>());
            }
            else if (datapoint.Name.EndsWith("/odp0001"))
            {
                await ProcessBrightness(datapoint.First.Value<float>());
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "The data point name is unexpected.");
            }
        }

        private async Task ProcessBrightness(float brightness)
        {
            currentBrightness = brightness;
            if (currentState != 2)
                return;

            await Connection.SetTitleAsync($"{brightness!:0}%");
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{Settings.DeviceId} {Settings.Channel} brightness is now at {brightness:0}%.");
        }

        private async Task ProcessState(int state)
        {
            switch (state)
            {
                case 0:
                    currentState = 1;
                    await Connection.SetStateAsync(1);
                    await Connection.SetTitleAsync("Off");
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{Settings.DeviceId} {Settings.Channel} is now off.");
                    break;
                case 1:
                    currentState = 2;
                    await Connection.SetStateAsync(2);
                    await Connection.SetTitleAsync($"{currentBrightness!:0}%");
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{Settings.DeviceId} {Settings.Channel} is now on.");
                    break;
                default:
                    currentState = 0;
                    await Connection.SetStateAsync(0);
                    await Connection.SetTitleAsync(string.Empty);
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{Settings.DeviceId} {Settings.Channel} has an unknown value.");
                    break;
            }
        }
    }
}
