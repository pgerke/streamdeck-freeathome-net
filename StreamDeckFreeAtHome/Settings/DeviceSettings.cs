using Newtonsoft.Json;
using System;

namespace PhilipGerke.StreamDeckFreeAtHome.Settings
{
    /// <summary>
    ///     The class defining the connection settings for a free@home device.
    /// </summary>
    [Serializable]
    public class DeviceSettings : ConnectionSettings
    {
        /// <summary>
        ///     Gets or sets the device identifier.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        ///     Gets or sets device channel.
        /// </summary>
        [JsonProperty("channel")]
        public string Channel { get; set; }
    }
}
