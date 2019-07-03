using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

namespace comprehend_custom {
	class Program {
		const string DataAccessRoleArn = "arn:aws:iam::865118636886:role/comprehend-s3";
		const string TrainingFile = "s3://s6jackjack/JEOPARDY_CSV-labeled.csv";
		static void Main(string[] args) {
			var awsOptions = BuildAwsOptions();
			var service = new ComprehendService(awsOptions.CreateServiceClient<IAmazonComprehend>());

			// create a custom classifier using training data
			var jobName = Guid.NewGuid().ToString();
			Console.WriteLine(jobName);
			var task = service.CreateCustomClassifier(jobName, "en", DataAccessRoleArn, TrainingFile);
			task.Wait();
			var taskArn = task.Result.DocumentClassifierArn;
			Console.WriteLine(taskArn);
			service.WaitForCreationCompletion(taskArn);
			Console.WriteLine("custom classifier created");

			// run a batch job on unlabeled data using the classifier you just created

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

		public async Task<StartDocumentClassificationJobResponse> StartBatchJob(string jobName, string inputS3Uri, string outputS3Uri) {
			return await this.comprehend.StartDocumentClassificationJobAsync(new StartDocumentClassificationJobRequest {
				JobName = jobName,
				InputDataConfig = new InputDataConfig {
					InputFormat = InputFormat.ONE_DOC_PER_LINE,
					S3Uri = inputS3Uri
				},
				OutputDataConfig = new OutputDataConfig {
					S3Uri = outputS3Uri
				}
			});
		}

		public async Task<CreateDocumentClassifierResponse> CreateCustomClassifier(string jobName, string languageCode, string roleArn, string inputS3Uri) {
			return await this.comprehend.CreateDocumentClassifierAsync(new CreateDocumentClassifierRequest {
				DocumentClassifierName = jobName,
				LanguageCode = languageCode,
				DataAccessRoleArn = roleArn,
				InputDataConfig = new DocumentClassifierInputDataConfig {
					S3Uri = inputS3Uri
				}
			});
		}

		public bool IsCreationComplete(string jobArn) {
			var task = this.comprehend.DescribeDocumentClassifierAsync(new DescribeDocumentClassifierRequest {
				DocumentClassifierArn = jobArn
			});
			task.Wait();
			var status = task.Result.DocumentClassifierProperties.Status.Value;
			if(status == "IN_ERROR") {
				Console.WriteLine("Error: [{0}]", task.Result.DocumentClassifierProperties.Message);
			}
			return status == "IN_ERROR" || status == "TRAINED";
		}

		public bool IsBatchJobComplete(string jobId) {
			var task = this.comprehend.DescribeDocumentClassificationJobAsync(new DescribeDocumentClassificationJobRequest {
				JobId = jobId
			});
			task.Wait();
			var status = task.Result.DocumentClassificationJobProperties.JobStatus;
			return status == "FAILED" || status == "COMPLETED";
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
