using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;

namespace SimpleAzureBatchBoy.Test
{
    public class TaskUploader
    {
        readonly CloudStorageAccount _storageAccount;

        public TaskUploader(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;
        }

        /// <summary>
        /// Uploads to the blob container <paramref name="containerName"/> the files specified by <paramref name="filePatterns"/>
        /// from the directory of <code>SimpleAzureBatchBoy.Task\bin\Debug</code>
        /// </summary>
        public async Task UploadFiles(string containerName, string[] filePatterns)
        {
            var client = _storageAccount.CreateCloudBlobClient();

            var container = client.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync();

            var files = filePatterns.SelectMany(GetFiles).ToList();

            Console.WriteLine($"Uploading to container '{containerName}'");

            var tasks = files.Select(async file =>
            {
                var blob = container.GetBlockBlobReference(Path.GetFileName(file));

                Console.WriteLine($@"    OK {blob.Name}
       {file}");

                await blob.UploadFromFileAsync(file, FileMode.Open);
            });

            await Task.WhenAll(tasks);

            Console.WriteLine();
        }

        static IEnumerable<string> GetFiles(string filePattern)
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "SimpleAzureBatchBoy.Task", "bin", "Debug");

            return Directory.GetFiles(directory, filePattern)
                .Select(Path.GetFullPath);
        }

    }
}