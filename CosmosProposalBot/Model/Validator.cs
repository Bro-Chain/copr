using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class Validator
{
    [JsonProperty("operator_address")]
    public string OperatorAddress { get; set; }

    [JsonProperty("description")]
    public ValidatorDescription Description { get; set; }
    
    [JsonProperty("consensus_pubkey")]
    public ConsensusPublickey PublicKey { get; set; }

    [JsonProperty("jailed")]
    public bool IsJailed { get; set; }
}
