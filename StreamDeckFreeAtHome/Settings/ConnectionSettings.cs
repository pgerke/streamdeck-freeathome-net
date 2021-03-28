using Newtonsoft.Json;
using System;

namespace PhilipGerke.StreamDeckFreeAtHome.Settings
{
    /// <summary>
    ///     The class defining the connection settings for a free@home system access point.
    /// </summary>
    [Serializable]
    public class ConnectionSettings
    {
        /// <summary>
        ///     Gets or sets the IP address for the free@home system access point (SysAP).
        /// </summary>
        [JsonProperty("sysApIpAddr")]
        public string SysApIpAddress { get; set; }

        /// <summary>
        ///     Gets or sets the user ID to be used to connect to the SysAP.
        /// </summary>
        [JsonProperty("sysApUserId")]
        public string SysApUserID { get; set; }

        /// <summary>
        ///     Gets or sets the user password to be used to connect to the SysAP.
        /// </summary>
        [JsonProperty("sysApPassword")]
        public string SysApPassword { get; set; }
    }
}
