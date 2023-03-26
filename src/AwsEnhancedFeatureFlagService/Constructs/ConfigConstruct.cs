using Amazon.CDK.AWS.IAM;
using Constructs;

namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal class ConfigConstruct : Construct
    {
        public ConfigConstruct(Construct scope, string id) : base(scope, id)
        {
            var serviceAutomationRole = new Role(this, "service-automation", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ssm.amazonaws.com"),
                RoleName = "AutomationServiceRole"
            });

            serviceAutomationRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonS3FullAccess"));
            serviceAutomationRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMFullAccess"));
            serviceAutomationRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonSSMAutomationRole"));
            serviceAutomationRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Resources = new[] { serviceAutomationRole.RoleArn },
                Actions = new[] { "iam:PassRole" }
            }));

            // Due to a bug with CDK unable to create a remediation configuration currently.
            // Manually created this in aws console but used the policy created
        }
    }
}