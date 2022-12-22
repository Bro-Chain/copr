using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Data.Model;

public enum EndpointType
{
    Rest,
    Rpc,
    Grpc
}

public class Endpoint
{
    [Key] 
    public Guid Id { get; set; }
    public Chain Chain { get; set; }
    public string Provider { get; set; }
    public string Url { get; set; }
    public EndpointType Type { get; set; }
}
