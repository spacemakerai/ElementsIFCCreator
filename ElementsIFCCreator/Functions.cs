using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using FormaAPI;
using FormaRestClientImpl;
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
    public async Task<APIGatewayProxyResponse> GetIFCS3Url(APIGatewayProxyRequest request, ILambdaContext context)
    {
        IRestClient restClient = new RestClientImpl(new AccessTokenLambda());
        context.Logger.LogInformation("Getting the proposal tree");
        var projectId = request.PathParameters["projectId"];
        var proposalUrn = request.PathParameters["proposalId"];
        string dataRegion = request.QueryStringParameters["dataRegion"]; // Place holder

        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(proposalUrn))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = "projectId and proposalId are required",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
        }


        // Get proposal tree
        CommonClient.ClientOptions clientOptions = new CommonClient.ClientOptions(jsonConverter: new JsonConverter(),
            mock: null,
            reporter: new ErrorReporter(LogErrorMethod: context.Logger.LogError, LogMethod: context.Logger.LogInformation),
            client: null,
            dataRegion: dataRegion);

        ProposalTreeRetriever proposalTreeRetriever = new ProposalTreeRetriever(options: clientOptions,
            progress: null,
            allowConcurrency: true,
            disableBatchElementFetch: false);

        var proposalTree = proposalTreeRetriever.GetProposalTree(projectId, proposalUrn, true);

        if (proposalTree == null)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = "Proposal tree not found",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
        }

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = "Hello AWS Serverless",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }

    public class AccessTokenLambda : IAccessToken
    {
        public string GetAccessToken(TokenAccessType accessType)
        {
            return "Bearer " + "eyJhbGciOiJSUzI1NiIsImtpZCI6ImI4YjJkMzNhLTFlOTYtNDYwNS1iMWE4LTgwYjRhNWE4YjNlNyIsInBpLmF0bSI6ImFzc2MifQ.eyJjbGFpbXMiOnsic3BhY2VtYWtlcl9saWNlbnNlX3R5cGUiOiJob2JieWlzdCJ9LCJjbGllbnRfaWQiOiJLanNsWTZ2R3duY0Y5QUtWR3BvdmMzRE12ZEQzSnVzVyIsInVzZXJpZCI6IlVIOUpLSEJHTTJMUFNFNVgiLCJzY29wZSI6WyJvcGVuaWQiLCJ1c2VyLXByb2ZpbGU6cmVhZCIsImRhdGE6cmVhZCIsImRhdGE6Y3JlYXRlIiwiZGF0YTp3cml0ZSIsImFjY291bnQ6cmVhZCJdLCJpc3MiOiJodHRwczovL2RldmVsb3Blci5hcGkuYXV0b2Rlc2suY29tIiwiYXVkIjpbImh0dHBzOi8vYXV0b2Rlc2suY29tIl0sImV4cCI6MTcyMTE2NTE4MSwianRpIjoiMjJjODg5YWItNGMwNS00NTg1LTk4MTEtY2U2NWI3ZGJiMTdjIn0.bH25lgfjWaZehtHvTYv-XZ-fTERQaDXjNKIyOH_ESO-TyhHpgnwtEMX6jzMm9h5gHlirO1Z7QRHtwK60QRbQ3x02k22U2NLtIyAdVjZdVQ5eYRsAnbMmoU5gNuoSLKLNVvurU3CAlgWhxNUfhwIKpdaBIAvzV1Ris8bnnOxQ3AKORtp0B6PfVLm7fsdTTobsZsw5RuxpApf_PgyTeFrOY0ykaoyZBj6UXMbAOO6pcbK_P2SwdK1Zaqd_mdGdmXBBj11tLMZkr3tE1_-T8Unz3vHl2UctxFc0jhWGqO6SXX-d-YOHkoXnZwxNz-P0sHV2nk9z8xV4ngTQTEUzyG5tHA";
        }
    }
}
