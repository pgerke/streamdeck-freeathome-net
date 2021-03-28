using BarRaider.SdTools;
using PhilipGerke.StreamDeckFreeAtHome.Settings;
using System;
using System.Net.WebSockets;
using System.Text;
using Websocket.Client;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A base plugin that implements the abstract methods from the <see cref="PluginBase"/>.
    /// </summary>
    public abstract class WebSocketPlugin : BasePlugin<DeviceSettings>
    {
        private IDisposable disconnectionHappenedSubscription;
        private IDisposable messageReceivedSubscription;
        private WebsocketClient webSocketClient = null;

        /// <summary>
        ///     Constructs a new <see cref="WebSocketPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public WebSocketPlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            CreateNewWebSocket();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            disconnectionHappenedSubscription?.Dispose();
            messageReceivedSubscription?.Dispose();
            webSocketClient?.Stop(WebSocketCloseStatus.NormalClosure, "Planned closure of web socket.");
            webSocketClient?.Dispose();
        }

        /// <summary>
        ///     Processes the specified message.
        /// </summary>
        /// <param name="message">The <see cref="ResponseMessage"/> to be processed.</param>
        protected abstract void ProcessMessage(ResponseMessage message);

        /// <inheritdoc/>
        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            base.ReceivedSettings(payload);
            CreateNewWebSocket();
        }

        private void CreateNewWebSocket()
        {
            // Dispose the old web socket 
            disconnectionHappenedSubscription?.Dispose();
            messageReceivedSubscription?.Dispose();
            webSocketClient?.Stop(WebSocketCloseStatus.NormalClosure, "Planned closure of web socket.");
            webSocketClient?.Dispose();

            if (string.IsNullOrWhiteSpace(Settings.SysApIpAddress)
                || string.IsNullOrWhiteSpace(Settings.SysApUserID)
                || string.IsNullOrWhiteSpace(Settings.SysApPassword))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot create web socket as mandatory settings are missing.");
                return;
            }

            webSocketClient = new WebsocketClient(new Uri($"ws://{Settings.SysApIpAddress}/fhapi/v1/api/ws"), () =>
            {
                byte[] bytes = Encoding.ASCII.GetBytes($"{Settings.SysApUserID}:{Settings.SysApPassword}");
                ClientWebSocket webSocket = new();
                webSocket.Options.SetRequestHeader("Authorization", $"Basic {Convert.ToBase64String(bytes)}");
                return webSocket;
            });
            disconnectionHappenedSubscription = webSocketClient.DisconnectionHappened.Subscribe(
                info => Logger.Instance.LogMessage(TracingLevel.ERROR,
                    $"Websocket disconnected with type '{info.Type}' and status '{info.CloseStatus}': {info.CloseStatusDescription}"));
            messageReceivedSubscription = webSocketClient.MessageReceived.Subscribe(ProcessMessage);
            webSocketClient.Start().ConfigureAwait(false);
        }
    }
}
