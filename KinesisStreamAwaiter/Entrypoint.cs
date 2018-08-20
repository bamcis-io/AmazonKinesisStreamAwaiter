using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Lambda.Core;
using BAMCIS.AWSLambda.Common;
using BAMCIS.AWSLambda.Common.CustomResources;
using System;
using System.Threading;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace BAMCIS.LambdaFunctions.KinesisStreamAwaiter
{
    /// <summary>
    /// The Lambda function entrypoint
    /// </summary>
    public class Entrypoint : CustomResourceHandler
    {
        #region Private Fields

        private IAmazonKinesis _KinesisClient;

        private int _WaitTimeInMillis;

        private static readonly int _DefaultWaitTime = 500;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Entrypoint()
        {
            this._KinesisClient = new AmazonKinesisClient();

            if (!int.TryParse(Environment.GetEnvironmentVariable("WAIT_TIME"), out _WaitTimeInMillis))
            {
                _WaitTimeInMillis = _DefaultWaitTime;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called when the Kinesis Stream Awaiter is created in the CF script, it will wait on the specified stream to enter 
        /// ACTIVE status
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<CustomResourceResponse> CreateAsync(CustomResourceRequest request, ILambdaContext context)
        {
            if (request.ResourceProperties.ContainsKey("StreamName"))
            {
                context.LogInfo($"Beginning await for Kinesis stream {request.ResourceProperties["StreamName"]}.");

                DescribeStreamRequest Request = new DescribeStreamRequest()
                {
                    StreamName = request.ResourceProperties["StreamName"].ToString()
                };

                while (true)
                {
                    if (context.RemainingTime.TotalMilliseconds < 1500)
                    {
                        return new CustomResourceResponse(CustomResourceResponse.RequestStatus.FAILED, "Timeout waiting for stream to become active.", request);
                    }

                    DescribeStreamResponse Response = await this._KinesisClient.DescribeStreamAsync(Request);

                    if ((int)Response.HttpStatusCode < 300)
                    {
                        if (Response.StreamDescription.StreamStatus == StreamStatus.ACTIVE)
                        {
                            break;
                        }
                    }
                    else
                    {
                        context.LogWarning($"Received an unsuccessful response to the describe stream request: {(int)Response.HttpStatusCode}.");
                    }

                    Thread.Sleep(_WaitTimeInMillis);
                }

                context.LogInfo($"Successfully created Kinesis stream {Request.StreamName}.");

                return new CustomResourceResponse(CustomResourceResponse.RequestStatus.SUCCESS, "Created", Request.StreamName, request.StackId, request.RequestId, request.LogicalResourceId);
            }
            else
            {
                return new CustomResourceResponse(CustomResourceResponse.RequestStatus.FAILED, "The StreamName property was not provided.", "stream", request.StackId, request.RequestId, request.LogicalResourceId);
            }
        }

        /// <summary>
        /// Called when the Kinesis Stream Awaiter is deleted in the CF script, no action is taken
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<CustomResourceResponse> DeleteAsync(CustomResourceRequest request, ILambdaContext context)
        {
            context.LogInfo("Delete called on KinesisStreamAwaiter");
            return new CustomResourceResponse(CustomResourceResponse.RequestStatus.SUCCESS, "Deleted", request);
        }

        /// <summary>
        /// Called when the Kinesis Stream Awaiter is updated, it will wait for the specified Kinesis Stream to be in the ACTIVE
        /// state
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<CustomResourceResponse> UpdateAsync(CustomResourceRequest request, ILambdaContext context)
        {
            if (request.ResourceProperties.ContainsKey("StreamName"))
            {
                DescribeStreamRequest Request = new DescribeStreamRequest()
                {
                    StreamName = request.ResourceProperties["StreamName"].ToString()                     
                };

                while (true)
                {
                    if (context.RemainingTime.TotalMilliseconds < 1500)
                    {
                        return new CustomResourceResponse(CustomResourceResponse.RequestStatus.FAILED, "Timeout waiting for stream to become active.", request);
                    }

                    DescribeStreamResponse Response = await this._KinesisClient.DescribeStreamAsync(Request);

                    if ((int)Response.HttpStatusCode < 300)
                    {
                        if (Response.StreamDescription.StreamStatus == StreamStatus.ACTIVE)
                        {
                            break;
                        }
                    }
                    else
                    {
                        context.LogWarning($"Received an unsuccessful response to the describe stream request: {(int)Response.HttpStatusCode}.");
                    }

                    Thread.Sleep(_WaitTimeInMillis);
                }

                context.LogInfo($"Successfully updated Kinesis stream {Request.StreamName}.");

                return new CustomResourceResponse(CustomResourceResponse.RequestStatus.SUCCESS, "Updated", request);
            }
            else
            {
                return new CustomResourceResponse(CustomResourceResponse.RequestStatus.FAILED, "The StreamName property was not provided.", request);
            }         
        }

        #endregion
    }
}
