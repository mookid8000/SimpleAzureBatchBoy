using System;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.WindowsAzure.Storage;

namespace SimpleAzureBatchBoy.Test
{
    class Program
    {
        const string BatchAccountUrl = "<url>";
        const string BatchAccountName = "<name>";
        const string BatchAccountKey = "<key>";

        const string StorageAccountConnectionString = "<connection-string>";

        static readonly CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(StorageAccountConnectionString);
        static readonly BatchSharedKeyCredentials BatchCreds = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

        static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();
        }

        static async Task Run()
        {
            // upload console app with/without .config file to storage
            await UploadTaskToStorage();

            // run
            await RunTasks();
        }

        static async Task RunTasks()
        {
            using (var batchClient = BatchClient.Open(BatchCreds))
            {
                var runner = new TaskRunner(batchClient, StorageAccount);

                var outputWithConfig = await runner.RunAndGetOutput("with-config");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(outputWithConfig);
                Console.WriteLine();
                Console.ResetColor();

                var outputWithoutConfig = await runner.RunAndGetOutput("without-config");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(outputWithoutConfig);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        static async Task UploadTaskToStorage()
        {
            var uploader = new TaskUploader(StorageAccount);

            // upload task incl. .config file to container 'with-config'
            await uploader.UploadFiles("with-config", new[] { "SimpleAzureBatchBoy.Task.exe", "SimpleAzureBatchBoy.Task.exe.config" });

            // upload task excl. .config file to container 'without-config'
            await uploader.UploadFiles("without-config", new[] { "SimpleAzureBatchBoy.Task.exe" });
        }
    }
}
