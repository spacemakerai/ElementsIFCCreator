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
    public APIGatewayProxyResponse GetIFCS3Url(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Getting the proposal tree");
        var projectId = request.PathParameters["projectId"];
        var proposalUrn = request.PathParameters["proposalUrn"];
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
        CommonClient.ClientOptions clientOptions = new CommonClient.ClientOptions(
            jsonConverter: new NewtonsoftJsonImpl.JsonNetConverter(),
            mock: new MockSupportChild(new Dictionary<string, string>()),
            reporter: new ErrorReporter(LogErrorMethod: context.Logger.LogError, LogMethod: context.Logger.LogInformation),
            client: new RestClientImpl(new AccessTokenLambda()),
            dataRegion: dataRegion);

        ProposalTreeRetriever proposalTreeRetriever = new ProposalTreeRetriever(options: clientOptions,
            progress: new ProgressIndicator(),
            allowConcurrency: true,
            disableBatchElementFetch: false);

        ProposalTree proposalTree = proposalTreeRetriever.GetProposalTree(projectId, proposalUrn, true);

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
            return "eyJhbGciOiJSUzI1NiIsImtpZCI6ImI4YjJkMzNhLTFlOTYtNDYwNS1iMWE4LTgwYjRhNWE4YjNlNyIsInBpLmF0bSI6ImFzc2MifQ.eyJjbGFpbXMiOnsic3BhY2VtYWtlcl9saWNlbnNlX3R5cGUiOiJob2JieWlzdCJ9LCJjbGllbnRfaWQiOiJLanNsWTZ2R3duY0Y5QUtWR3BvdmMzRE12ZEQzSnVzVyIsInVzZXJpZCI6IlVIOUpLSEJHTTJMUFNFNVgiLCJzY29wZSI6WyJvcGVuaWQiLCJ1c2VyLXByb2ZpbGU6cmVhZCIsImRhdGE6cmVhZCIsImRhdGE6Y3JlYXRlIiwiZGF0YTp3cml0ZSIsImFjY291bnQ6cmVhZCJdLCJpc3MiOiJodHRwczovL2RldmVsb3Blci5hcGkuYXV0b2Rlc2suY29tIiwiYXVkIjpbImh0dHBzOi8vYXV0b2Rlc2suY29tIl0sImV4cCI6MTcyMTkzNjE2OCwianRpIjoiNzFmZWY1MGUtN2VlYy00Yzc2LTg2MTUtMDA1NjZhYjM0MWYxIn0.UK2Ck1RrCTuQytMCN34UcdWlGjJWHNZu8Xob2h9nqX-no68zA7p-IOO8skaUhEzg5NSAsxpmw9XAXK6QYLTO4ASYQj3G4i-gQAWq55ukCT_-ucnukWoJ9-7MWg9w6P95qtuFtVs8bc8umk-jcyP2qKpy4j5ClxzLlJKn2of14o5rCZD83b_hkK66Fg4PBNdmEB7ErpM6TPmDM1Oo9v7skr6_SyIcopoaHwy-BcVqR5j1VEZhoypNzwYLfexBVC8gZ23btNqTjoeTZAWAR5Pe1r1uumAdsy7SjhaOuxvg2flLalf3TU5Sv4Mn8xkgLroQgp8_3-VRD_cR_0Kc-vKoyg";
        }
    }

    public class MockSupportChild : MockSupport
    {
        public MockSupportChild(Dictionary<string, string> mockData) : base(mockData)
        {
        }

        public override string GetMockFileFolder()
        {
            return null;
        }

        public override string GetMockName()
        {
            return "fileName";
        }

        public override bool IsPlayingRecordedMock()
        {
            return false;
        }

        public override bool IsRecordingMock()
        {
            return false;
        }
    }

    public class ProgressIndicator : IProgressIndicator
    {
        public void Increment(int amount = 1)
        {
            return;
        }

        public void IncrementNumSteps()
        {
            return;
        }

        public void ReportProgress(string message)
        {
            return;
        }
    }
}
