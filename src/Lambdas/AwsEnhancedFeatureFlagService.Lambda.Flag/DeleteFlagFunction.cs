using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System.Net;
using System.Text.Json;

namespace AwsEnhancedFeatureFlagService.Lambda.Flag;

public class DeleteFlagFunction
{
    private readonly DynamoDBContext _context;
    private readonly string _tableName;
    private readonly RegionEndpoint _region;
    private readonly Dictionary<string, string> _headers = new()
    {
        { "Content-Type", "application/json" },
        { "Access-Control-Allow-Origin", "*" },
    };
    private const string AllowFlagDeletionParamName = "allow-flag-deletion";

    public DeleteFlagFunction()
    {
        _context = new DynamoDBContext(new AmazonDynamoDBClient());
        var tableName = Environment.GetEnvironmentVariable("FlagTableName");
        if (tableName is null)
        {
            throw new Exception("Missing Table Name Variable");
        }

        var region = Environment.GetEnvironmentVariable("region");
        if (region is null)
        {
            throw new Exception("Missing region Variable");
        }

        _tableName = tableName;
        _region = RegionEndpoint.GetBySystemName(region);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input)
    {
        var flagName = input.PathParameters["flagName"];

        if (!await AreDeletesEnabled())
        {
            Console.WriteLine("Error Deleting Flag {0}, Flag deletion is not enabled", flagName);
            return CreateResponse(false, "Deleting Flags is disabled", HttpStatusCode.Forbidden);
        }

        var operationConfig = new DynamoDBOperationConfig() { OverrideTableName = _tableName };
        var existingFlag = await _context.LoadAsync<Flag>(flagName, operationConfig);

        if (existingFlag == null)
        {
            return CreateResponse(false, "Error Deleting Flag: Feature Flag Does Not Exist", HttpStatusCode.BadRequest);
        }

        try
        {
            await _context.DeleteAsync(existingFlag, operationConfig);
            return CreateResponse(true, null, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Deleting Flag: {0}", ex.Message);
            return CreateResponse(false, "Error Deleting Flag", HttpStatusCode.InternalServerError);
        }
    }

    private APIGatewayProxyResponse CreateResponse(bool success, string? errorMessage, HttpStatusCode httpStatusCode)
    {
        var flagResponse = new FlagResponse()
        {
            Success = success,
            ErrorMessage = errorMessage
        };

        return ResponseHelper.Create(JsonSerializer.Serialize(flagResponse), httpStatusCode, _headers);
    }

    private async Task<bool> AreDeletesEnabled()
    {
        var ssmClient = new AmazonSimpleSystemsManagementClient(_region);

        var response = await ssmClient.GetParameterAsync(new GetParameterRequest
        {
            Name = AllowFlagDeletionParamName
        });

        var value = response.Parameter.Value;

        bool.TryParse(value, out var allowFlagDeletion);

        return allowFlagDeletion;
    }
}