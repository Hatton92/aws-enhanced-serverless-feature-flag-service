using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Constructs;
using System.Collections.Generic;

namespace AwsEnhancedFeatureFlagService.Constructs
{
    internal class ApiConstruct : Construct
    {
        public ApiConstruct(Construct scope, string id, ApiConstructProps props) : base(scope, id)
        {
            var api = new RestApi(this, "enhanced-feature-flag-api", new RestApiProps
            {
                BinaryMediaTypes = new[] { "*/*" },
                RestApiName = "enhanced-feature-flag-api"
            });

            var rootPath = api.Root.AddResource("feature-flag");
            var flagPath = rootPath.AddResource("{flagName}");
            var lambdaAlarmSNS = new Topic(this, "Lambda Alarm", new TopicProps() { TopicName = "Lambda-Errors" });

            var schemaProperties = new Dictionary<string, IJsonSchema> {
                { "flagName", new JsonSchema { Type = JsonSchemaType.STRING } },
                { "currentVariation", new JsonSchema { Type = JsonSchemaType.BOOLEAN } }
            };

            var jsonSchema = new JsonSchema()
            {
                Type = JsonSchemaType.OBJECT,
                Required = new string[] { "flagName", "currentVariation" },
                Properties = schemaProperties,
            };

            var requestModel = new Model(this, "requestModel", new ModelProps
            {
                RestApi = api,
                ContentType = "application/json",
                Description = "validate request",
                ModelName = "requestModel",
                Schema = jsonSchema
            });

            var createUpdateValidator = new RequestValidator(this, "create-update-validator", new RequestValidatorProps
            {
                RestApi = api,
                RequestValidatorName = "create-update-validator",
                ValidateRequestBody = true,
                ValidateRequestParameters = true
            });

            var getDeleteValidator = new RequestValidator(this, "get-delete-validator", new RequestValidatorProps
            {
                RestApi = api,
                RequestValidatorName = "get-delete-validator",
                ValidateRequestParameters = true
            });

            AddGetFlagEndpoint(flagPath, props, getDeleteValidator, lambdaAlarmSNS);
            AddUpdateFlagEndpoint(flagPath, props, requestModel, createUpdateValidator);
            AddDeleteFlagEndpoint(flagPath, props, getDeleteValidator);
            AddCreateFlagEndpoint(rootPath, props, requestModel, createUpdateValidator);
        }

        private void AddGetFlagEndpoint(Resource category, ApiConstructProps props, RequestValidator requestValidator, Topic lambdaAlarmSNS)
        {
            var lambda = CreateLambda("get-flag-lambda", "GetFlagFunction", props);
            CreateAlarm(lambdaAlarmSNS, lambda);

            var integration = new LambdaIntegration(lambda);

            category.AddMethod("GET", integration, new MethodOptions
            {
                RequestParameters = new Dictionary<string, bool>
                {
                    { "method.request.path.flagName", true },
                },
                MethodResponses = new MethodResponse[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200",
                        ResponseParameters = new Dictionary<string, bool>
                        {
                            { "method.response.header.Content-Type", true }
                        }
                    }
                },
                RequestValidator = requestValidator
            });
        }

        private void CreateAlarm(Topic lambdaAlarmSNS, Function lambda)
        {
            var pattern = FilterPattern.AnyTerm(new string[] { "ERROR", "Error", "error" });
            var metric = new Metric(new MetricProps { MetricName = "Get Function Errors", Namespace = "lambdaErrors", Statistic = "sum" });
            metric.With(new MetricOptions { Statistic = "sum", Period = Amazon.CDK.Duration.Minutes(5) });

            var metricFilter = new MetricFilter(this, "get-function-errors-metric",
                new MetricFilterProps
                {
                    MetricNamespace = "lambdaErrors",
                    FilterPattern = pattern,
                    MetricName = "Get Function Errors",
                    LogGroup = lambda.LogGroup
                });

            metricFilter.ApplyRemovalPolicy(Amazon.CDK.RemovalPolicy.DESTROY);

            var alarm = new Alarm(this, "get-function-errors-alarm", new AlarmProps
            {
                ActionsEnabled = true,
                Metric = metric,
                AlarmName = "Get Function Errors",
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                Threshold = 0,
                EvaluationPeriods = 1
            });

            alarm.AddAlarmAction(new SnsAction(lambdaAlarmSNS));
        }

        private void AddUpdateFlagEndpoint(Resource category, ApiConstructProps props, Model requestModel, RequestValidator requestValidator)
        {
            var lambda = CreateLambda("update-flag-lambda", "UpdateFlagFunction", props);

            var integration = new LambdaIntegration(lambda);

            category.AddMethod("PUT", integration, new MethodOptions
            {
                RequestParameters = new Dictionary<string, bool>
                {
                    { "method.request.path.flagName", true },
                },
                MethodResponses = new MethodResponse[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200",
                        ResponseParameters = new Dictionary<string, bool>
                        {
                            { "method.response.header.Content-Type", true }
                        }
                    }
                },
                RequestModels = new Dictionary<string, IModel>
                {
                    { "application/json", requestModel }
                },
                RequestValidator = requestValidator
            });
        }

        private void AddDeleteFlagEndpoint(Resource category, ApiConstructProps props, RequestValidator requestValidator)
        {
            var lambda = CreateLambda("delete-flag-lambda", "DeleteFlagFunction", props);

            var integration = new LambdaIntegration(lambda);

            category.AddMethod("DELETE", integration, new MethodOptions
            {
                RequestParameters = new Dictionary<string, bool>
                {
                    { "method.request.path.flagName", true },
                },
                MethodResponses = new MethodResponse[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200",
                        ResponseParameters = new Dictionary<string, bool>
                        {
                            { "method.response.header.Content-Type", true }
                        }
                    }
                },
                RequestValidator = requestValidator,
            });
        }

        private void AddCreateFlagEndpoint(Resource categories, ApiConstructProps props, Model requestModel, RequestValidator requestValidator)
        {
            var lambda = CreateLambda("create-flag-lambda", "CreateFlagFunction", props);

            var integration = new LambdaIntegration(lambda);

            categories.AddMethod("POST", integration, new MethodOptions
            {
                MethodResponses = new MethodResponse[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200",
                        ResponseParameters = new Dictionary<string, bool>
                        {
                            { "method.response.header.Content-Type", true }
                        }
                    }
                },
                RequestModels = new Dictionary<string, IModel>
                {
                    { "application/json", requestModel }
                },
                RequestValidator = requestValidator
            });
        }

        private Function CreateLambda(string id, string functionClass, ApiConstructProps props)
        {
            var lambda = new Function(this, id, new FunctionProps
            {
                Runtime = Runtime.DOTNET_6,
                Architecture = Architecture.ARM_64,
                Code = Code.FromAsset("./src/Lambdas/AwsEnhancedFeatureFlagService.Lambda.Flag/bin/Release/net6.0/linux-arm64/publish"),
                Handler = $"AwsEnhancedFeatureFlagService.Lambda.Flag::AwsEnhancedFeatureFlagService.Lambda.Flag.{functionClass}::FunctionHandler",
                Environment = new Dictionary<string, string>
                {
                    { "FlagTableName", props.FlagTable.TableName },
                    { "region", Amazon.CDK.Stack.Of(this).Region}
                },
                Timeout = Amazon.CDK.Duration.Seconds(10)
            });

            props.FlagTable.GrantReadWriteData(lambda);
            lambda.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Resources = new[] { $"arn:aws:ssm:eu-west-2:*" },
                Actions = new[] { "ssm:GetParameter" }
            }));
            return lambda;
        }
    }
}
