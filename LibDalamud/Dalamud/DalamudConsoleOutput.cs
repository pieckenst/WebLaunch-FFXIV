using Newtonsoft.Json;

namespace LibDalamud.Common.Dalamud
{
    public sealed class DalamudConsoleOutput
    {
        [JsonProperty("pid")]
        public int Pid { get; set; }

        [JsonProperty("handle")]
        public long Handle { get; set; }
    }
}