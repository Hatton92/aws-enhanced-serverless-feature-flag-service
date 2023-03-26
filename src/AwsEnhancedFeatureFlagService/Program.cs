using Amazon.CDK;

namespace AwsEnhancedFeatureFlagService
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            new AwsEnhancedFeatureFlagService(app, "AwsEnhancedFeatureFlagService");
            app.Synth();
        }
    }
}
