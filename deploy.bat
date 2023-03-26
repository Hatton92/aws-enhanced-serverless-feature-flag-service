aws ssm put-parameter --name "allow-flag-deletion" --type "String" --value false --profile=personal --overwrite

dotnet publish src\Lambdas\AwsEnhancedFeatureFlagService.Lambda.Flag\ -c Release -r linux-arm64 --self-contained
dotnet publish src\Lambdas\AwsEnhancedFeatureFlagService.Lambda.Analytics\ -c Release -r linux-arm64 --self-contained
cdk deploy --profile=personal