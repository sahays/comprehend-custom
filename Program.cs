using System;
using System.Threading.Tasks;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

namespace comprehend_custom {
	class Program {
		const string ServiceRoleArn = "<your-IAM-service-role-for-Comprehend>";
		const string TrainingFile = "s3://<your-s3-bucket>/jeopardy-filtered-labeled.csv";
		const string InputFile = "s3://<your-s3-bucket>/JEOPARDY_CSV.csv";
		const string OutputLocation = "s3://<your-s3-bucket>/comprehend-output/";
		static void Main(string[] args) {
			var awsOptions = BuildAwsOptions();
			var service = new ComprehendService(awsOptions.CreateServiceClient<IAmazonComprehend>());

			// create a custom classifier using training data
			var jobName = Guid.NewGuid().ToString();
			Console.WriteLine(jobName);
			var newCustomClassifierArn = service.CreateCustomClassifier(jobName, "en", ServiceRoleArn, TrainingFile);
			Console.WriteLine(newCustomClassifierArn);
			service.WaitForCreationCompletion(newCustomClassifierArn);
			Console.WriteLine("custom classifier created");

			// run a batch job on unlabeled data using the classifier you just created
			var jobId = service.StartBatchJob(jobName, ServiceRoleArn, newCustomClassifierArn, InputFile, OutputLocation);
			Console.WriteLine(jobId);
			service.WaitForJobCompletion(jobId);
			Console.WriteLine("job complete");
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

		public string StartBatchJob(string jobName, string roleArn, string customClassifierArn, string inputS3Uri, string outputS3Uri) {
			using(var task = this.comprehend.StartDocumentClassificationJobAsync(new StartDocumentClassificationJobRequest {
				JobName = jobName,
				DataAccessRoleArn = roleArn,
				DocumentClassifierArn = customClassifierArn,
				InputDataConfig = new InputDataConfig {
					InputFormat = InputFormat.ONE_DOC_PER_LINE,
					S3Uri = inputS3Uri
				},
				OutputDataConfig = new OutputDataConfig {
					S3Uri = outputS3Uri
				}
			})) {
				task.Wait();
				return task.Result.JobId;
			}
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

		public bool IsCreationComplete(string jobArn) {
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
				Console.WriteLine("Status: [{0}], Message: [{1}]", props.Status, props.Message);
				Console.WriteLine("Started at: [{0}], completed at: [{1}]", props.TrainingStartTime, props.TrainingStartTime);
				Console.WriteLine("Accuracy: [{0}], F1Score: [{1}], Precision: [{2}], Recall: [{3}]", metrics.Accuracy, metrics.F1Score, metrics.Precision, metrics.Recall);
			}
		}

		public bool IsBatchJobComplete(string jobId) {
			var task = this.comprehend.DescribeDocumentClassificationJobAsync(new DescribeDocumentClassificationJobRequest {
				JobId = jobId
			});
			task.Wait();
			var status = task.Result.DocumentClassificationJobProperties.JobStatus;
			var result = task.Result;
			Print(status, result);
			return status == "FAILED" || status == "COMPLETED";
		}

		private void Print(string status, DescribeDocumentClassificationJobResponse result) {
			if(status == "FAILED") {
				Console.WriteLine("Error: [{0}]", result.DocumentClassificationJobProperties.Message);
			} else if(status == "COMPLETED") {
				var props = result.DocumentClassificationJobProperties;
				Console.WriteLine("Job Id: [{0}], Name: [{1}], Status: [{2}], Message: [{3}]", props.JobId, props.JobName, props.JobStatus, props.Message);
				Console.WriteLine("Started at: [{0}], completed at: [{1}]", props.SubmitTime, props.EndTime);
				Console.WriteLine("Output located at: [{0}]", props.OutputDataConfig.S3Uri);
			}
		}

		public void WaitForCreationCompletion(string jobArn, int delay = 5000) {
			while(!IsCreationComplete(jobArn)) {
				this.Wait(delay);
			}
		}
		public void WaitForJobCompletion(string jobId, int delay = 5000) {
			while(!IsBatchJobComplete(jobId)) {
				this.Wait(delay);
			}
		}

		private void Wait(int delay = 5000) {
			Task.Delay(delay).Wait();
			Console.Write(".");
		}
	}
}
