using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class RpcResponseBase
{
    [JsonProperty("jsonrpc")]
    public string JsonRpcVersion { get; set; }
    public int Id { get; set; }
    public string Error { get; set; }
}

public class RpcBlockResponse : RpcResponseBase
{
    public BlockInfoResult Result { get; set; }
}

public class BlockInfoResult
{
    public BlockInfoBlock Block { get; set; }
}
