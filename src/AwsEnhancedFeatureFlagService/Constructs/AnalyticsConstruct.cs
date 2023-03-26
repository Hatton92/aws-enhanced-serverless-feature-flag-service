using Amazon.CDK;
using Amazon.CDK.AWS.Kinesis;
using Amazon.CDK.AWS.KinesisFirehose.Alpha;
using Amazon.CDK.AWS.KinesisFirehose.Destinations.Alpha;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Constructs;
using System.Collections.Generic;

namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal class AnalyticsConstruct : Construct
    {
        public IStream KinesisStream { get; private set; }

        public AnalyticsConstruct(Construct scope, string id, AnalyticsConstructProps props) : base(scope, id)
        {
            var bucketProps = new BucketProps
            {
                AutoDeleteObjects = true,
                RemovalPolicy = RemovalPolicy.DESTROY,
                Versioned = false,
                BucketName = "feature-flag-firehose"
            };

            var firehoseBucket = new Bucket(this, "firehose-s3", bucketProps);

            var lambda = CreateLambda("lambda-processor", "Processor");

            var lambdaProcessor = new LambdaFunctionProcessor(lambda, new DataProcessorProps
            {
                Retries = 5,
            });

            var dynamoDBChangesPrefix = "ddb-changes";

            var s3BucketProps = new S3BucketProps
            {
                BufferingInterval = Duration.Seconds(60),
                Processor = lambdaProcessor,
                DataOutputPrefix = $"{dynamoDBChangesPrefix}/"
            };

            var s3Destination = new S3Bucket(firehoseBucket, s3BucketProps);

            if (props.KinesisFirehoseEnabled)
            {
                KinesisStream = new Stream(this, "Stream");
                var deliveryStreamProps = new DeliveryStreamProps
                {
                    SourceStream = KinesisStream,
                    Destinations = new[] { s3Destination }
                };

                new DeliveryStream(this, "Delivery Stream", deliveryStreamProps);
            }
        }

        private Function CreateLambda(string id, string functionClass)
        {
            var lambda = new Function(this, id, new FunctionProps
            {
                Runtime = Runtime.DOTNET_6,
                Architecture = Architecture.ARM_64,
                Code = Code.FromAsset("./src/Lambdas/AwsEnhancedFeatureFlagService.Lambda.Analytics/bin/Release/net6.0/linux-arm64/publish"),
                Handler = $"AwsEnhancedFeatureFlagService.Lambda.Analytics::AwsEnhancedFeatureFlagService.Lambda.Analytics.{functionClass}::FunctionHandler",
                Environment = new Dictionary<string, string>
                { },
                Timeout = Duration.Minutes(2)
            });

            return lambda;
        }
    }
}