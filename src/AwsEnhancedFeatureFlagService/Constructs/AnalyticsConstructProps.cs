namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal struct AnalyticsConstructProps
    {
        internal bool KinesisFirehoseEnabled { get; private set; }

        internal AnalyticsConstructProps(bool kinesisFirehoseEnabled)
        {
            KinesisFirehoseEnabled = kinesisFirehoseEnabled;
        }
    }
}
