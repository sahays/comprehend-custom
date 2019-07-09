// Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

using System;
using System.Threading.Tasks;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

namespace custom_classification {
	class Program {
		const string ServiceRoleArn = "<your-IAM-service-role-for-Comprehend>";
		const string TrainingFile = "s3://<your-s3-bucket>/training-data.csv";
		static void Main(string[] args) {
			var awsOptions = BuildAwsOptions();
			var service = new ComprehendService(awsOptions.CreateServiceClient<IAmazonComprehend>());

			// create a custom classifier using training data
			var jobName = Guid.NewGuid().ToString();
			Console.WriteLine(jobName);
			Console.WriteLine("Creating a Custom Classifier with job name: [{0}], service role: [{1}], and training file: [{2}]", jobName, ServiceRoleArn, TrainingFile);
			var newCustomClassifierArn = service.CreateCustomClassifier(jobName, "en", ServiceRoleArn, TrainingFile);
			Console.WriteLine(newCustomClassifierArn);
			service.WaitForCompletion(newCustomClassifierArn);
			Console.WriteLine("Done!");
		}

		private static AWSOptions BuildAwsOptions() {
			var builder = new ConfigurationBuilder()
				.SetBasePath(Environment.CurrentDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();
			return builder.GetAWSOptions();
		}
	}

	public class ComprehendService {
		private IAmazonComprehend comprehend { get; }
		public ComprehendService(IAmazonComprehend comprehend) {
			this.comprehend = comprehend;
		}

		public string CreateCustomClassifier(string jobName, string languageCode, string roleArn, string inputS3Uri) {
			using(var task = this.comprehend.CreateDocumentClassifierAsync(new CreateDocumentClassifierRequest {
				DocumentClassifierName = jobName,
				LanguageCode = languageCode,
				DataAccessRoleArn = roleArn,
				InputDataConfig = new DocumentClassifierInputDataConfig {
					S3Uri = inputS3Uri
				}
			})) {
				task.Wait();
				return task.Result.DocumentClassifierArn;
			}
		}

		public bool IsComplete(string jobArn) {
			var task = this.comprehend.DescribeDocumentClassifierAsync(new DescribeDocumentClassifierRequest {
				DocumentClassifierArn = jobArn
			});
			task.Wait();
			var result = task.Result;
			var status = result.DocumentClassifierProperties.Status.Value;
			Print(status, result);
			return status == "IN_ERROR" || status == "TRAINED";
		}

		private void Print(string status, DescribeDocumentClassifierResponse result) {
			if(status == "IN_ERROR") {
				Console.WriteLine("Error: [{0}]", result.DocumentClassifierProperties.Message);
			} else if(status == "TRAINED") {
				var props = result.DocumentClassifierProperties;
				var metrics = result.DocumentClassifierProperties.ClassifierMetadata.EvaluationMetrics;
				var meta = result.DocumentClassifierProperties.ClassifierMetadata;
				Console.WriteLine("Custom Classsification Arn (use this Arn with the Analysis job): [{0}]", props.DocumentClassifierArn);
				Console.WriteLine("Status: [{0}], Message: [{1}]", props.Status, props.Message);
				Console.WriteLine("Started at: [{0}], completed at: [{1}]", props.TrainingStartTime, props.TrainingStartTime);
				Console.WriteLine("NumberOfLabels: [{0}], NumberOfTestDocuments: [{1}], NumberOfTrainedDocuments: [{2}]", meta.NumberOfLabels, meta.NumberOfTestDocuments, meta.NumberOfTrainedDocuments);
				Console.WriteLine("Accuracy: [{0}], F1Score: [{1}], Precision: [{2}], Recall: [{3}]", metrics.Accuracy, metrics.F1Score, metrics.Precision, metrics.Recall);
			}
		}

		public void WaitForCompletion(string jobArn, int delay = 5000) {
			while(!IsComplete(jobArn)) {
				this.Wait(delay);
			}
		}

		private void Wait(int delay = 5000) {
			Task.Delay(delay).Wait();
			Console.Write(".");
		}
	}
}
