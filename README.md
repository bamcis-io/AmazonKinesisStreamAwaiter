# BAMCIS Amazon Kinesis Stream Awaiter Serverless Application

Provides a custom resource for CloudFormation that waits for an Amazon Kinesis Stream to be ACTIVE before completing creation. This is essentially like a `WaitCondition` and `WaitConditionHandle` for Amazon Kinesis Streams. This allows you to create a dependency on the `Awaiter` for other resources that need to use the Kinesis Stream but may throw an error if the stream isn't ACTIVE yet.

## Table of Contents
- [Usage](#usage)
- [Revision History](#revision-history)

## Usage

Deploy this serverless application in the account and region you want to use the custom resource in CloudFormation. Once it is deployed, note the `Output` value of `FunctionArn`. You'll need to provide that Arn to your other CloudFormation templates.

The following is an example of a CloudFormation template that uses the custom resource.

    {
        ...
        "Parameters" : {
            ...
            "AwaiterArn" : {
                "Type" : "String",
                "Description" : "The Arn of the Kinesis Awaiter function.",
                "Default" : "arn:aws:lambda:us-east-1:123456789012:function:KinesisStreamAwaiter",
                "AllowedPattern" : "^arn:aws(?:-us-gov|-cn)?:lambda:.*?:[0-9]{12}:function:.*$",
                "ConstraintDescription" : "Member must satisfy regular expression pattern: ^arn:aws(?:-us-gov|-cn)?:lambda:.*?:[0-9]{12}:function:. $"
            }
            ...
        },
        
        "Resources" : {
            "KinesisStreamAwaiter" : {
                "Type" : "Custom::KinesisStreamAwaiter",
                "Properties" : {
                    "ServiceToken" : {
                        "Ref" : "AwaiterArn"
                    },
                    "StreamName" : {
                        "Ref" : "KinesisStream"
                    }
                }
            },
         
            "KinesisStream" : {
                "Type" : "AWS::Kinesis::Stream",
                "Properties" : {
                    "Name" : {
                        "Ref" : "StreamName"
                    },
                    "ShardCount" : {
                        "Ref" : "ShardCount"
                    }
                }
            },
         
            "FirehoseDeliveryStream"             : {
                "Type" : "AWS::KinesisFirehose::DeliveryStream",
                "Properties" : {
                    "DeliveryStreamName" : "DeliveryStream",
                    "DeliveryStreamType" : "KinesisStreamAsSource",
                    "KinesisStreamSourceConfiguration" : {
                        "KinesisStreamARN" : {
                            "Fn::GetAtt" : [
                                "KinesisStream",
                                "Arn"
                            ]
                        },
                        "RoleARN"          : {
                            "Fn::GetAtt" : [
                                "FirehoseRole",
                                "Arn"
                            ]
                        }
                    },
                    ...
                },
                "DependsOn" : [
        	         "KinesisStreamAwaiter",
                    "FirehoseKinesisPolicy"
                ]
            },
        
            ...
        }
    }

In the example, the Kinesis Firehose Delivery Stream has a source of the Kinesis Stream, but its creation may fail if it tries to create before the Kinesis Stream becomes active, it's essentially a race condition. Most of the time the Kinesis Stream enters ACTIVE quickly, but it may not and you could get this error:

    Kinesis stream must be in ACTIVE or UPDATING state in order to create a delivery stream. (Service: AmazonKinesisFirehose; Status Code: 400; Error Code: InvalidArgumentException; Request ID: e266784a-c270-40de-994e-c7a7d443f665)

The awaiter ensures the Stream is ACTIVE before the Delivery Stream attempts creation.

## Revision History

### 1.0.0
Initial release of the application.
