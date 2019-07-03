# comprehend-custom
.NET Core C# code samples for Amazon Comprehend Custom Classification

IAM Role ARN that is used as ```DataAccessRoleArn``` for ```CreateDocumentClassifier```
```
arn:aws:iam::<account-id>:role/comprehend-s3
```

```DataAccessRoleArn``` uses the following policy document to grant privileges to Amazon Comprehend to access the S3 bucket where training data is stored
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
# Errors
Classification failure error
```
INPUT_BUCKET_NOT_IN_SERVICE_REGION: The provided input S3 bucket is not in the service region.
```
Another error
```
Error: [Found 27983 unique labels. The maximum allowed number of unique labels is 1000.]
```