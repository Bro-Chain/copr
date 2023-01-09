namespace CosmosProposalBot.Model;

public static class Constants
{
    public const string ProposalStatusDepositPeriod = "PROPOSAL_STATUS_DEPOSIT_PERIOD";
    public const string ProposalStatusVotingPeriod = "PROPOSAL_STATUS_VOTING_PERIOD";
    public const string ProposalStatusPassed = "PROPOSAL_STATUS_PASSED";
    public const string ProposalStatusRejected = "PROPOSAL_STATUS_REJECTED";
    
    public const string ProposalTypeText = "/cosmos.gov.v1beta1.TextProposal";
    public const string ProposalTypeSoftwareUpgrade = "/cosmos.upgrade.v1beta1.SoftwareUpgradeProposal";
}
