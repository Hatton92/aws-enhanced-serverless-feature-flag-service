using AwsEnhancedFeatureFlagService.Constructs;
using Constructs;

namespace AwsEnhancedFeatureFlagService
{
    public class AwsEnhancedFeatureFlagService : Amazon.CDK.Stack
    {
        private const bool KinesisFirehoseEnabled = false; //By default set to false to avoid accidentally incurring costs, switch to true to enable firehose to s3.

        internal AwsEnhancedFeatureFlagService(Construct scope, string id, Amazon.CDK.IStackProps props = null) : base(scope, id, props)
        {
            var analytics = new AnalyticsConstruct(this, "analytics", new AnalyticsConstructProps(KinesisFirehoseEnabled));
            var storage = new StorageConstruct(this, "storage", new StorageConstructProps(analytics.KinesisStream, KinesisFirehoseEnabled));
            _ = new ConfigConstruct(this, "config");
            _ = new ApiConstruct(this, "api", new ApiConstructProps(storage.FlagTable));
        }
    }
}
