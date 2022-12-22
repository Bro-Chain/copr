using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class SigningInfo
{
    [JsonProperty("address")]
    public string Address { get; set; }
    [JsonProperty("start_height")]
    public string StartHeight { get; set; }
    [JsonProperty("index_offset")]
    public string IndexOffset { get; set; }
    [JsonProperty("jailed_until")]
    public DateTimeOffset JailedUntil { get; set; }
    [JsonProperty("tombstoned")]
    public string Tombstoned { get; set; }
    [JsonProperty("missed_blocks_counter")]
    public string MissedBlocksCounter { get; set; }
}
