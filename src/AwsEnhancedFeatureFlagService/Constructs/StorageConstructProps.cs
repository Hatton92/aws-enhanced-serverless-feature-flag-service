using Amazon.CDK.AWS.Kinesis;

namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal struct StorageConstructProps
    {
        internal IStream KinesisStream { get; private set; }

        internal bool KinesisFirehoseEnabled { get; private set; }

        internal StorageConstructProps(IStream kinesisStream, bool kinesisFirehoseEnabled)
        {
            KinesisStream = kinesisStream;
            KinesisFirehoseEnabled = kinesisFirehoseEnabled;
        }
    }
}
