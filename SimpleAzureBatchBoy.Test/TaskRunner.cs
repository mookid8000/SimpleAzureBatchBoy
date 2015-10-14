using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SimpleAzureBatchBoy.Test
{
    public class TaskRunner
    {
        readonly BatchClient _batchClient;
        readonly CloudStorageAccount _storageAccount;

        public TaskRunner(BatchClient batchClient, CloudStorageAccount storageAccount)
        {
            _batchClient = batchClient;
            _storageAccount = storageAccount;
        }

        public async Task<string> RunAndGetOutput(string containerName)
        {
            Console.WriteLine($"Running job from container '{containerName}'");

            var poolId = "pool1";
            var time = DateTime.Now;
            var jobId = $"job-{containerName}-{time:HHmmss}";

            await EnsurePoolExists(poolId);

            try
            {
                return await RunJob(jobId, poolId, containerName);
            }
            finally
            {
                await CleanUpJob(jobId);
            }
        }

        async Task<string> RunJob(string jobId, string poolId, string containerName)
        {
            Console.WriteLine($"Creating job '{jobId}'");

            var job = _batchClient.JobOperations.CreateJob(jobId, new PoolInformation { PoolId = poolId });

            await job.CommitAsync();

            // "bind"... just because
            job = _batchClient.JobOperations.GetJob(jobId);

            var taskId = "main";

            Console.WriteLine($"Creating task '{taskId}'");

            var task = new CloudTask(taskId, "SimpleAzureBatchBoy.Task.exe")
            {
                ResourceFiles = GetResourceFiles(containerName).ToList()
            };

            await job.AddTaskAsync(task);

            Console.Write("Waiting for task to finish... ");

            var taskStateMonitor = _batchClient.Utilities.CreateTaskStateMonitor();

            // "bind" again... just because
            task = _batchClient.JobOperations.GetTask(jobId, taskId);

            await taskStateMonitor.WaitAllAsync(new[] { task }, TaskState.Completed, TimeSpan.FromMinutes(5));

            await task.RefreshAsync();

            Console.WriteLine("Done!");

            Console.WriteLine("Loading task output");

            var standardOut = await GetString(task, Constants.StandardOutFileName);
            var standardError = await GetString(task, Constants.StandardErrorFileName);

            return $@"Output:

{standardOut}

Error:

{standardError}";
        }

        static async Task<string> GetString(CloudTask task, string fileName)
        {
            var nodeFile = await task.GetNodeFileAsync(fileName);
            var output = await nodeFile.ReadAsStringAsync();
            return output;
        }

        IEnumerable<ResourceFile> GetResourceFiles(string containerName)
        {
            var blobs = _storageAccount.CreateCloudBlobClient();

            var resourceFiles = blobs
                .GetContainerReference(containerName)
                .ListBlobs()
                .Select(blobListItem =>
                {
                    var blobName = blobListItem.Uri.Segments[2];
                    var sharedAccessSignature = blobListItem.Container
                        .GetBlockBlobReference(blobName)
                        .GetSharedAccessSignature(new SharedAccessBlobPolicy
                        {
                            Permissions = SharedAccessBlobPermissions.Read,
                            SharedAccessExpiryTime = DateTimeOffset.Now.AddHours(1)
                        });

                    var blobPath = blobListItem.Uri + sharedAccessSignature;
                    var resourceFile = new ResourceFile(blobPath, blobName);
                    return resourceFile;
                })
                .ToList();

            return resourceFiles;
        }

        async Task CleanUpJob(string jobId)
        {
            try
            {
                var existingJob = _batchClient.JobOperations.GetJob(jobId);

                Console.WriteLine($"Deleting job '{jobId}'");

                await existingJob.DeleteAsync();
            }
            catch (Exception)
            {
            }
        }

        async Task EnsurePoolExists(string poolId)
        {
            try
            {
                Console.WriteLine($"Ensuring that pool '{poolId}' exists");

                var pool = _batchClient.PoolOperations.CreatePool(poolId, "4", "small", 1);

                await pool.CommitAsync();
            }
            catch (BatchException exception)
            {
                if (!exception.Message.Contains("PoolExists")) throw;
            }
        }
    }
}