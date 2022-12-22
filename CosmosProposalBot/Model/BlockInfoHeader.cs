using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class BlockInfoHeader
{
    [JsonProperty("chain_id")]
    public string ChainId { get; set; }
    public string Height { get; set; }
    public DateTime Time { get; set; }
}
