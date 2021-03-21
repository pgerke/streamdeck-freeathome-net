using BarRaider.SdTools;

namespace PhilipGerke.StreamDeckFreeAtHome
{
    /// <summary>
    ///     A stream deck action that verifies the connection to a system access point.
    /// </summary>
    [PluginActionId("com.philipgerke.freeathome.test")]
    public sealed class FreeAtHomeConnectionTestPlugin : PluginBase
    {
        /// <summary>
        ///     Constructs a new <see cref="FreeAtHomeConnectionTestPlugin"/> instance.
        /// </summary>
        /// <param name="connection">The Stream Deck connection.</param>
        /// <param name="payload">The initial payload.</param>
        public FreeAtHomeConnectionTestPlugin(ISDConnection connection, InitialPayload payload)
            : base(connection, payload)
        {
            
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
        }

        /// <inheritdoc/>
        public override void KeyPressed(KeyPayload payload)
        {
        }

        /// <inheritdoc/>
        public override void KeyReleased(KeyPayload payload)
        {
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
            //Tools.AutoPopulateSettings(settings, payload.Settings);
        }
    }
}
