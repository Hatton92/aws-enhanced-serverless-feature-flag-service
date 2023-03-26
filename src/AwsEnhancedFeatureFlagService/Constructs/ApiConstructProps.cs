using Amazon.CDK.AWS.DynamoDB;

namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal struct ApiConstructProps
    {
        internal ITable FlagTable { get; private set; }

        internal ApiConstructProps(ITable flagTable)
        {
            FlagTable = flagTable;
        }
    }
}
