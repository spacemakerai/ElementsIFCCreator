using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ElementsIFCCreator.IfcGeometryCreators;
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

        // Get the proposal Tree
        ProposalTree proposalTree = null;
        try
        {
            proposalTree = proposalTreeRetriever.GetProposalTree(projectId, proposalUrn, true);

        }
        catch (Exception e)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = e.Message,
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
        }
        LevelOfDetailClient lodClient = new LevelOfDetailClient(clientOptions);

        IFCCreator ifCreator = new IFCCreator(proposalTree, context.Logger.LogInformation, context.Logger.LogError, lodClient);
        ifCreator.Create();

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
            return "eyJhbGciOiJSUzI1NiIsImtpZCI6ImI4YjJkMzNhLTFlOTYtNDYwNS1iMWE4LTgwYjRhNWE4YjNlNyIsInBpLmF0bSI6ImFzc2MifQ.eyJjbGFpbXMiOnsic3BhY2VtYWtlcl9saWNlbnNlX3R5cGUiOiJob2JieWlzdCJ9LCJjbGllbnRfaWQiOiJLanNsWTZ2R3duY0Y5QUtWR3BvdmMzRE12ZEQzSnVzVyIsInVzZXJpZCI6IlVIOUpLSEJHTTJMUFNFNVgiLCJzY29wZSI6WyJvcGVuaWQiLCJ1c2VyLXByb2ZpbGU6cmVhZCIsImRhdGE6cmVhZCIsImRhdGE6Y3JlYXRlIiwiZGF0YTp3cml0ZSIsImFjY291bnQ6cmVhZCJdLCJpc3MiOiJodHRwczovL2RldmVsb3Blci5hcGkuYXV0b2Rlc2suY29tIiwiYXVkIjpbImh0dHBzOi8vYXV0b2Rlc2suY29tIl0sImV4cCI6MTcyMjg4MDEzNiwianRpIjoiZDMzODcxZjktYzk5Ny00NDAyLWI5OGYtNDRlNTQyMjk2YmVmIn0.VeYwHcPDuC2MxPSP686gbcGCPiqY-tdi8MvRbLwMCRQDUAJYENvLn_jjoVF4-guxLDwUU1sK27wiyKC1Uz32Q_fq4U7XkSec_eExHaZjmlwAGQcdrh81KKslfgwxPuYF5mH-D62btydgmRVXha_MfbzljJgndd0Ro0geNRbIU7ZLkYoLLQ2OZCdugiCrcznKu-pxf0QYnlVdeTQgOHOHufJ55MCKOVALe-6wVLi48Zyjyto1jIA0c3VozGmmNwEqGRAYORebeuMdmsZxrXsbuA3mMa_ut0RhNLz68V-Cd1jP_C2vRKxL0wyROXJ-z1nF-sgchrWyDHd_DJcPl32rWg";
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
