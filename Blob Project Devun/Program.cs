using System;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace Blob_Project_Devun
{
    class Program
    {
        private static async Task ProcessAsync()
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudblobcontainer = null;
            string sourceFile = "";
            string destinationFile = "";
            string storageConnectionString = Environment.GetEnvironmentVariable("storageconnectionstring");


            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                Console.WriteLine("Successfully Parsed Storage Connection String");
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    cloudblobcontainer = cloudBlobClient.GetContainerReference("testcontainer" + Guid.NewGuid().ToString());
                    await cloudblobcontainer.CreateAsync();
                    Console.WriteLine("Created container '{0}' ", cloudblobcontainer.Name);
                    Console.WriteLine();

                    BlobContainerPermissions permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await cloudblobcontainer.SetPermissionsAsync(permissions);

                    string localPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string localFileName = "BlobFile_ " + Guid.NewGuid().ToString() + ".txt";
                    sourceFile = Path.Combine(localPath, localFileName);
                    File.WriteAllText(sourceFile, "Hello, World");
                    Console.WriteLine("Temp file = {0}", sourceFile);
                    Console.WriteLine("Uploading to Blob Storage as blob '{0}'", localFileName);
                    Console.WriteLine();

                    CloudBlockBlob cloudBlockBlob = cloudblobcontainer.GetBlockBlobReference(localFileName);
                    await cloudBlockBlob.UploadFromFileAsync(sourceFile);

                    Console.WriteLine("Listing blobs in container.");
                    BlobContinuationToken blobcontinuationtoken = null;
                    do
                    {
                        var results = await cloudblobcontainer.ListBlobsSegmentedAsync(null, blobcontinuationtoken);
                        blobcontinuationtoken = results.ContinuationToken;
                        foreach (IListBlobItem item in results.Results)
                        {
                            Console.WriteLine(item.Uri);
                        }
                    }
                    while (blobcontinuationtoken != null);
                    Console.WriteLine();

                    destinationFile = sourceFile.Replace(".txt", "_DOWNLOAD.txt");
                    Console.WriteLine("Downloading blob to {0}", destinationFile);
                    Console.WriteLine();
                    await cloudBlockBlob.DownloadToFileAsync(destinationFile, FileMode.Create);
                }
                catch (StorageException e)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                }
                finally
                {
                    Console.WriteLine("Pres any key and hit enter to delete the sample files and example container.");
                    Console.ReadLine();
                    Console.WriteLine("Deleting the container and any blobs it contains");
                    if (cloudblobcontainer != null)
                    {
                        await cloudblobcontainer.DeleteIfExistsAsync();
                    }
                    Console.WriteLine("Deleting all created local files");
                    Console.WriteLine();

                    File.Delete(sourceFile);

                    File.Delete(destinationFile);
                   
                }

            }
            else
            {
                Console.WriteLine("Failed to Parse Storage Connection String");

            }
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage Excercise");
            Console.WriteLine();
            ProcessAsync().GetAwaiter().GetResult();
            Console.WriteLine("Press any key and hit the enter to exit the sample application.");
            Console.ReadLine();
        }
    }
}
