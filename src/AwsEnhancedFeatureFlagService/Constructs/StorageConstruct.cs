using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal class StorageConstruct : Construct
    {
        public ITable FlagTable { get; private set; }

        public StorageConstruct(Construct scope, string id, StorageConstructProps props) : base(scope, id)
        {
            var tableProps = new TableProps
            {
                PartitionKey = new Attribute { Name = "name", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = Amazon.CDK.RemovalPolicy.DESTROY,
            };

            if (props.KinesisFirehoseEnabled)
            {
                tableProps.KinesisStream = props.KinesisStream;
            }

            FlagTable = new Table(this, "flags", tableProps);
        }
    }
}
