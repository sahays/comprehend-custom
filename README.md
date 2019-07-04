# About
.NET Core C# code samples for [Amazon Comprehend](https://aws.amazon.com/comprehend/) Custom Classification. You can use Amazon Comprehend to build your own models for custom classification, assigning a document to a class or a category.

Amazon Comprehend uses natural language processing (NLP) to extract insights about the content of documents. Amazon Comprehend processes any text file in UTF-8 format. It develops insights by recognizing the entities, key phrases, language, sentiments, and other common elements in a document. Use Amazon Comprehend to create new products based on understanding the structure of documents. For example, using Amazon Comprehend you can search social networking feeds for mentions of products or scan an entire document repository for key phrases.

# Overview
Custom classification is a two step process. First you train a custom classifier to recognize the categories that are of interest to you. To train the classifier, you send Amazon Comprehend a group of labeled documents. After Amazon Comprehend builds the classifier, you send documents to be classified. The custom classifier examines each document and returns the label that best represents the content of the document.

This sample has two .NET Core projects:
- The project `custom-classification` uses Amazon Comprehend to create a [Custom Classifier](https://docs.aws.amazon.com/comprehend/latest/dg/how-document-classification-training.html)
- The project `analysis-job` uses Amazon Comprehend custom classifier to categorize unlabeled documents in a test file (each line is a document) by starting a [classification job](https://docs.aws.amazon.com/comprehend/latest/dg/how-class-run.html) that helps you Analyze the content of documents stored in Amazon S3 to find insights like entities, phrases, primary language or sentiment


# Variables
You need to set the following variables in Program.cs file inside `custom-classification` and `analysis-job` folders before following the steps to execute the program
| Variable  | Purpose
|---|---|---|
| ServiceRoleArn |  IAM Service Role for Amazon Comprehend that needs to read/write from S3 buckets. You need to create this role in your AWS account.
| TrainingFile | This file has labeled data that is used by Comprehend to train the custom classifier. You can use your own file or upload the training-data.csv to your S3 bucket provided with this sample
| InputFile  | This file has test data that is used as an input for the Comprehend classification batch job. You can use your own file or upload the training-data.csv to your S3 bucket provided with this sample
| OutputLocation | This is the S3 bucket where the Comprehend classification batch job output will be emitted. You can see a sample output file `output.jsonl` in the `analysis-job` folder

# Prerequisites
- [Dotnet Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2)
- [AWS CLI](https://docs.aws.amazon.com/polly/latest/dg/setup-aws-cli.html) for
  running AWS CLI commands after configuring a
  [default or named profile](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-configure.html)

# Steps to execute
- Download the code
- [create a new S3 bucket](https://docs.aws.amazon.com/cli/latest/reference/s3api/create-bucket.html) for training and unlabeled data 
- [create an IAM role](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_create_for-service.html) using the policy document described below
- go to Program.cs in each project, find all variables with `<your->` and replace them with actual values in your own test account
- From a command line, go to `custom-classification` project first in the downloaded folder and then execute `dotnet run` this will download all dependencies, build, and run the program. Follow the same for `analysis-job` project

```ServiceRoleArn``` uses the following policy document to grant privileges to Amazon Comprehend to access the S3 bucket where training data is stored
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "s3:*Bucket"
            ],
            "Resource": [
                "arn:aws:s3:::<your-bucket-name>"
            ]
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:*Object"
            ],
            "Resource": [
                "arn:aws:s3:::<your-bucket-name>/*"
            ]
        }
    ]
}
```

At the completion of the training you'll see a response similar to the following:
```
Status: [TRAINED], Message: []
Started at: [7/3/19 9:52:14 PM], completed at: [7/3/19 9:52:14 PM]
Accuracy: [0.9149], F1Score: [0.8674], Precision: [0.8901], Recall: [0.8489]
custom classifier created
```

Job completion response output print
```
Job Id: [8df6e23b534a9c7aa2831e58cbef04ac], Name: [06df74c8-c5ba-4325-a8e1-9ba5c54eeea5], Status: [COMPLETED], Message: []
Started at: [7/3/19 9:33:33 PM], completed at: [7/3/19 9:40:13 PM]
Output located at: [s3://<your-bucket-name>/<some-object-key>/<your-account-id>-CLN-8df6e23b534a9c7aa2831e58cbef04ac/output/output.tar.gz]
```
# Dependencies
The following dependencies are defined in the .csproj file that are downloaded when you first execute `dotnet run`
```
<ItemGroup>
    <PackageReference Include="AWSSDK.Comprehend" Version="3.3.101" />
    <PackageReference Include="AWSSDK.Extensions.NETCore.Setup" Version="3.3.100.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="2.2.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="2.2.0" />
</ItemGroup>
```

# Troubleshooting
In case you encounter a Classification failure error like the following, please ensure that the S3 bucket is in the same region as Comprehend
```
INPUT_BUCKET_NOT_IN_SERVICE_REGION: The provided input S3 bucket is not in the service region.
```
If you get the following error, then please note that each classification can have up to a maximum of 1000 unique labels. The sample training file that I have used `jeopardy-filtered-labeled.csv` has only 3 unique labels each having more than 1000 documents (each line is a document). Read [Training a Custom Classifier](https://docs.aws.amazon.com/comprehend/latest/dg/how-document-classification-training.html) for more information
```
Error: [Found 27983 unique labels. The maximum allowed number of unique labels is 1000.]
```

# Reference
Source of the file [training-data.csv](https://drive.google.com/file/d/0BwT5wj_P7BKXb2hfM3d2RHU1ckE/view?usp=sharing) from [this website](https://blog.cambridgespark.com/50-free-machine-learning-datasets-natural-language-processing-d88fb9c5c8da) 