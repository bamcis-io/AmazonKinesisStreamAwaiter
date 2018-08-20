using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Amazon.S3.Model;
using BAMCIS.AWSLambda.Common.CustomResources;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BAMCIS.LambdaFunctions.KinesisStreamAwaiter.Tests
{
    public class FunctionTest
    {
        public FunctionTest()
        {
        }

        [Fact]
        public async Task TestCreate()
        {
            // ARRANGE
            AWSConfigs.AWSProfilesLocation = $"{Environment.GetEnvironmentVariable("UserProfile")}\\.aws\\credentials";

            string StreamName = "test-stream";
            string PresignedUrlBucket = "pre-sign-url-bucket";
            string AccountNumber = "123456789012";
            string Region = "us-east-1";

            IAmazonS3 S3Client = new AmazonS3Client();

            GetPreSignedUrlRequest Req = new GetPreSignedUrlRequest()
            {
                BucketName = PresignedUrlBucket,
                Key = "result.txt",
                Expires = DateTime.Now.AddMinutes(2),
                Protocol = Protocol.HTTPS,
                Verb = HttpVerb.PUT
            };

            string PreSignedUrl = S3Client.GetPreSignedURL(Req);

            string Json = $@"
{{
""requestType"":""create"",
""responseUrl"":""{PreSignedUrl}"",
""stackId"":""arn:aws:cloudformation:{Region}:{AccountNumber}:stack/stack-name/{Guid.NewGuid().ToString()}"",
""requestId"":""12345678"",
""resourceType"":""Custom::KinesisStreamAwaiter"",
""logicalResourceId"":""KinesisStreamAwaiter"",
""resourceProperties"":{{
""StreamName"":""{StreamName}""
}}
}}";

           
            CustomResourceRequest Request = JsonConvert.DeserializeObject<CustomResourceRequest>(Json);

            TestLambdaLogger TestLogger = new TestLambdaLogger();
            TestClientContext ClientContext = new TestClientContext();

            TestLambdaContext Context = new TestLambdaContext()
            {
                FunctionName = "KinesisStreamAwaiter",
                FunctionVersion = "1",
                Logger = TestLogger,
                ClientContext = ClientContext,
                LogGroupName = "aws/lambda/KinesisStreamAwaiter",
                LogStreamName = Guid.NewGuid().ToString(),
                RemainingTime = TimeSpan.FromSeconds(300)
            };


            Entrypoint Entrypoint = new Entrypoint();

            // ACT
            IAmazonKinesis KinesisClient = new AmazonKinesisClient();
            CreateStreamRequest CreateReq = new CreateStreamRequest()
            {
                ShardCount = 1,
                StreamName = StreamName
            };


            CreateStreamResponse CreateResponse = await KinesisClient.CreateStreamAsync(CreateReq);

            try
            {
                CustomResourceResult Response = await Entrypoint.ExecuteAsync(Request, Context);

                // ASSERT

                Assert.True(Response.IsSuccess);
            }
            finally
            {
                DeleteStreamRequest DeleteReq = new DeleteStreamRequest()
                {
                    StreamName = StreamName
                };

                await KinesisClient.DeleteStreamAsync(DeleteReq);
            }
        }
    }
}
