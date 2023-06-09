using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using System.Text.Json;

namespace AwsEnhancedFeatureFlagService.Lambda.Flag;

public class GetFlagFunction
{
    private readonly DynamoDBContext _context;
    private readonly string _tableName;
    private readonly Dictionary<string, string> _headers = new()
    {
        { "Content-Type", "application/json" },
        { "Access-Control-Allow-Origin", "*" },
    };

    public GetFlagFunction()
    {
        _context = new DynamoDBContext(new AmazonDynamoDBClient());
        var tableName = Environment.GetEnvironmentVariable("FlagTableName");
        if (tableName is null)
        {
            throw new Exception("Missing Table Name Variable");
        }

        _tableName = tableName;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest input)
    {
        var flagName = input.PathParameters["flagName"];
        var operationConfig = new DynamoDBOperationConfig() { OverrideTableName = _tableName };

        try
        {
            var flag = await _context.LoadAsync<Flag>(flagName, operationConfig);

            if (flag == null)
            {
                Console.WriteLine("Error Could not find feature flag Flag: {0}", flagName);
                return CreateResponse(false, "Error Retrieving Flag: Feature Flag Does Not Exsit", HttpStatusCode.BadRequest, null);
            }

            flag.LastAccessed = DateTime.Now;
            await _context.SaveAsync(flag, operationConfig);

            return CreateResponse(true, null, HttpStatusCode.OK, flag);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Retrieving Flag: {0}", ex.Message);
            return CreateResponse(false, "Error Retrieving Flag", HttpStatusCode.InternalServerError, null);
        }
    }

    private APIGatewayProxyResponse CreateResponse(bool success, string? errorMessage, HttpStatusCode httpStatusCode, Flag? flag)
    {
        var flagResponse = new FlagResponse()
        {
            Success = success,
            ErrorMessage = errorMessage,
            CurrentVariation = flag?.Value,
            FlagName = flag?.Name
        };

        return ResponseHelper.Create(JsonSerializer.Serialize(flagResponse), httpStatusCode, _headers);
    }
}
