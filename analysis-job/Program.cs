using System;
using System.Threading.Tasks;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

namespace analysis_job {
	class Program {
		const string ServiceRoleArn = "<your-IAM-service-role-for-Comprehend>";
		const string TestFile = "s3://<your-s3-bucket>/test-data.csv";
		const string OutputLocation = "s3://<your-s3-bucket>/comprehend-output/";
		const string CustomClassifierArn = "<arn-custom-classifier>";
		static void Main(string[] args) {
			var awsOptions = BuildAwsOptions();
			var service = new ComprehendService(awsOptions.CreateServiceClient<IAmazonComprehend>());
			var jobName = Guid.NewGuid().ToString();
			Console.WriteLine(jobName);

			// run a batch job on unlabeled data using the classifier you just created
			Console.WriteLine("Staring a new job with job name: [{0}], service role: [{1}], classifer: [{2}], test data: [{3}], and output location: [{4}]", jobName, ServiceRoleArn, CustomClassifierArn, TestFile, OutputLocation);
			var jobId = service.StartJob(jobName, ServiceRoleArn, CustomClassifierArn, TestFile, OutputLocation);
			Console.WriteLine(jobId);
			service.WaitForCompletion(jobId);
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

		public string StartJob(string jobName, string roleArn, string customClassifierArn, string inputS3Uri, string outputS3Uri) {
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

		public bool IsComplete(string jobId) {
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

		public void WaitForCompletion(string jobId, int delay = 5000) {
			while(!IsComplete(jobId)) {
				this.Wait(delay);
			}
		}

		private void Wait(int delay = 5000) {
			Task.Delay(delay).Wait();
			Console.Write(".");
		}
	}
}
