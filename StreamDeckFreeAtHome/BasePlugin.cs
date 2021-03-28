using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using PhilipGerke.StreamDeckFreeAtHome.Settings;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A base plugin that implements the abstract methods from the <see cref="PluginBase"/>.
    /// </summary>
    public abstract class BasePlugin : PluginBase
    {
        /// <summary>
        ///     The singleton <see cref="httpClient"/> instance to be used.
        /// </summary>
        protected static readonly HttpClient httpClient = new();

        /// <summary>
        ///     Constructs a new <see cref="WebSocketPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public BasePlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
        }

        /// <inheritdoc/>
        public override void Dispose() => Logger.Instance.LogMessage(TracingLevel.DEBUG,
            "The Dispose method has been called but is not overridden in the specified plugin.");

        /// <inheritdoc/>
        public override void KeyPressed(KeyPayload payload) => Logger.Instance.LogMessage(TracingLevel.DEBUG,
            "The KeyPressed method has been called but is not overridden in the specified plugin.");

        /// <inheritdoc/>
        public override void KeyReleased(KeyPayload payload) => Logger.Instance.LogMessage(TracingLevel.DEBUG,
            "The KeyReleased method has been called but is not overridden in the specified plugin.");

        /// <inheritdoc/>
        public override void OnTick() => Logger.Instance.LogMessage(TracingLevel.DEBUG,
            "The OnTick method has been called but is not overridden in the specified plugin.");

        /// <inheritdoc/>
        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) => Logger.Instance.LogMessage(TracingLevel.DEBUG,
            "The ReceivedGlobalSettings method has been called but is not overridden in the specified plugin.");

        /// <inheritdoc/>
        public override void ReceivedSettings(ReceivedSettingsPayload payload) => Logger.Instance.LogMessage(TracingLevel.DEBUG,
            "The ReceivedSettings method has been called but is not overridden in the specified plugin.");
    }

    /// <summary>
    ///     A base plugin that implements the abstract methods from the <see cref="PluginBase"/>.
    /// </summary>
    public abstract class BasePlugin<TSettings> : BasePlugin
        where TSettings : ConnectionSettings, new()
    {
        /// <summary>
        ///     Gets or sets the plugin settings.
        /// </summary>
        protected TSettings Settings { get; set; }

        /// <summary>
        ///     Constructs a new <see cref="WebSocketPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public BasePlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            // Deserialize settings or create a new instance.
            Settings = (payload.Settings == null || payload.Settings.Count == 0)
                ? new TSettings()
                : payload.Settings.ToObject<TSettings>();
            SetAuthenticationHeader();
        }

        /// <inheritdoc/>
        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Received Settings");
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            Connection.SetSettingsAsync(JObject.FromObject(Settings));
            SetAuthenticationHeader();
        }

        /// <summary>
        ///     Sets the authentication header for the HTTP client.
        /// </summary>
        protected void SetAuthenticationHeader()
        {
            if (string.IsNullOrWhiteSpace(Settings.SysApUserID) || string.IsNullOrWhiteSpace(Settings.SysApPassword))
            {
                return;
            }

            byte[] bytes = Encoding.ASCII.GetBytes($"{Settings.SysApUserID}:{Settings.SysApPassword}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }
    }
}
