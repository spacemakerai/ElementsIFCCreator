using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ElementsIFCCreator;

public class Functions
{
    /// <summary>
    /// Default constructor that Lambda will invoke.
    /// </summary>
    public Functions()
    {
    }


    /// <summary>
    /// This function will be triggered by an http request.
    /// It will get the urn for the proposal element tree, convert it to IFC file format, upload the file to S3 and return the S3 url.
    /// </summary>
    /// <param name="context">Information about the invocation, function, and execution environment</param>
    /// <param name="request">The incoming request</param>
    /// <returns>The response indicating success of failure with the appropriate HTTP status code<see cref="APIGatewayProxyResponse"/>APIGatewayProxyResponse with the status code and if successful, the url of the S3 location for the IFC file</returns>
    public APIGatewayProxyResponse Get(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Handling the 'Get' Request");

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = "Hello AWS Serverless",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }
}
