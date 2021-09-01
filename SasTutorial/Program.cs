//----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace SasTutorial
{
    /// <summary>
    /// Azure Storage Service SAS Sample for Blobs - Demonstrates how to create shared access signatures for use with Blob storage. 
    /// A shared access signature (SAS) provides delegated access to resources in your storage account,
    /// without giving clients the account access key. A SAS provides access to one or more resources in 
    /// your account with the permissions that you specify and over a time interval that you specify.
    /// 
    /// A SAS is a token added to the URL of a resource in your storage account. Anyone who has access to the 
    /// URL can access the resource with the permissions that the SAS grants, so it's important to carefully
    /// control access to a SAS URL. Additionally, using HTTPS with SAS is recommended as a best practice.
    /// 
    /// This sample demonstrates two ways of creating a SAS. You can create an ad-hoc SAS, where all of the SAS
    /// constraints are specified on the URL. Or you can create a SAS that is associated with a stored access policy 
    /// on a blob container. A stored access policy defines permissions and a validity interval, and a SAS that is 
    /// associated with the policy inherits those constraints. The advantage to using the stored access policy is that
    /// you can revoke the SAS if you believe it to be compromised.
    /// 
    /// This sample demonstrates how to create a service SAS, which applies only to a single service, in this case Blob storage. 
    /// You can also create an account SAS, which can apply to more than one storage service, and which enables you
    /// to delegate access to service-level operations.
    /// 
    /// Documentation References: 
    /// - Shared Access Signatures: Understanding the SAS Model: https://azure.microsoft.com/documentation/articles/storage-dotnet-shared-access-signature-part-1/
    /// - Create and use a SAS with Blob storage: https://azure.microsoft.com/documentation/articles/storage-dotnet-shared-access-signature-part-2/
    /// - Delegating Access with a Shared Access Signature: https://msdn.microsoft.com/library/ee395415.aspx
    /// - Constructing an Account SAS: https://msdn.microsoft.com/library/mt584140.aspx
    /// - Constructing a Service SAS: https://msdn.microsoft.com/library/dn140255.aspx
    /// - Establishing a Stored Access Policy: https://msdn.microsoft.com/library/dn140257.aspx
    /// - Use the Azure storage emulator for Development and Testing: https://azure.microsoft.com/documentation/articles/storage-use-emulator/
    /// </summary>
    class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run using either the Azure storage emulator that installs as part of the Azure SDK - or by
        // updating the App.Config file with your AccountName and Key. 
        // 
        // To run the sample using the storage emulator (default option)
        //      1. Start the Azure storage emulator (once only) by pressing the Start button or the Windows key and searching for it
        //         by typing "Azure storage emulator". Select it from the list of applications to start it.
        //      2. Set breakpoints and run the project using F10. 
        // 
        // To run the sample using a storage account
        //      1. Open the app.config file and comment out the connection string for the emulator (UseDevelopmentStorage=True) and
        //         uncomment the connection string for the storage service (AccountName=[]...)
        //      2. Create a storage account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
        //      3. Set breakpoints and run the project using F10. 
        // 
        // *************************************************************************************************************************
        static void Main()
        {
            const string containerPrefix = "sas-container-";
            const string policyPrefix = "tutorial-policy-";

            const string blobName1 = "sasBlob1.txt";
            const string blobContent1 = "Blob created with an container SAS with store access policy granting all permissions on the container.";

            const string blobName2 = "sasBlob2.txt";
            const string blobContent2 = "Blob created with a blob SAS with store access policy granting all permissions to the blob.";

            const string blobName3 = "sasBlob3.txt";
            const string blobContent3 = "Blob created with an container SAS granting all permissions on the container.";

            const string blobName4 = "sasBlob4.txt";
            const string blobContent4 = "Blob created with a blob SAS granting all permissions to the blob.";

            string containerName = containerPrefix + DateTime.Now.Ticks.ToString();
            string storeAccessPolicyName = policyPrefix + DateTime.Now.Ticks.ToString();

            //Parse the connection string and return a reference to the storage account.
            BlobServiceClient blobServiceClient = new BlobServiceClient(ConfigurationManager.AppSettings["StorageConnectionString"]);

            //Get a reference to a container to use for the sample code, and create it if it does not exist.
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

            try
            {
                container.CreateIfNotExists();
            }
            catch (RequestFailedException)
            {
                // Ensure that the storage emulator is running if using emulator connection string.
                Console.WriteLine("If you are running with the default connection string, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            //Create a new access policy on the container, which may be optionally used to provide constraints for
            //shared access signatures on the container and the blob.
            //The access policy provides create, write, read, list, and delete permissions.
            StorageSharedKeyCredential storageSharedKeyCredential = new StorageSharedKeyCredential(blobServiceClient.AccountName,ConfigurationManager.AppSettings["AzureStorageEmulatorAccountKey"]);

            CreateStoreAccessPolicy(container, storeAccessPolicyName);

            //Generate an  SAS URI for the container. The  SAS has all permissions.
            UriBuilder storeContainerSAS = GetContainerSasUri(container, storeAccessPolicyName, storageSharedKeyCredential);
            Console.WriteLine("1. SAS for blob container : " + storeContainerSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The write, read, and delete operations should  succeed, and the list operations should fail.
            TestContainerSAS(storeContainerSAS, blobName1, blobContent1);
            Console.WriteLine();

            //Generate an  SAS URI for the container. The  SAS has all permissions.
            UriBuilder containerSAS = GetContainerSasUri(container, storageSharedKeyCredential);
            Console.WriteLine("1. SAS for blob container : " + containerSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The write, read, and delete operations should  succeed, and the list operations should fail.
            TestContainerSAS(containerSAS, blobName3, blobContent3);
            Console.WriteLine();

            //Generate an  SAS URI for a blob within the container. The  SAS has all permissions.
            UriBuilder storeBlobSAS = GetBlobSasUri(container, blobName2, storageSharedKeyCredential);
            Console.WriteLine("3. SAS for blob : " + storeBlobSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The create, write, read, and delete operations should all succeed.
            TestBlobSAS(storeBlobSAS, blobContent2);
            Console.WriteLine();

            //Generate an  SAS URI for a blob within the container. The  SAS has all permissions.
            UriBuilder blobSAS = GetBlobSasUri(container, blobName4, storageSharedKeyCredential);
            Console.WriteLine("3. SAS for blob : " + blobSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The create, write, read, and delete operations should all succeed.
            TestBlobSAS(blobSAS, blobContent4);
            Console.WriteLine();

            //Delete the container to clean up.
            container.DeleteIfExists();

            Console.ReadLine();
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob container.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="StorageSharedKeyCredential">Storage Shared Key Credential.</param>
        /// <returns>A string containing the URI for the container, with the SAS token appended.</returns>
        static UriBuilder GetContainerSasUri(BlobContainerClient container, StorageSharedKeyCredential storageSharedKeyCredential)
        {
            var policy = new BlobSasBuilder
            {
                Protocol = SasProtocol.HttpsAndHttp,

                BlobContainerName = container.Name,
                Resource = "c",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                IPRange = new SasIPRange(IPAddress.None, IPAddress.None)
            };
            policy.SetPermissions(BlobSasPermissions.All);
            var sas = policy.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            UriBuilder sasUri = new UriBuilder(container.Uri);
            sasUri.Query = sas;
            //Return the URI string for the container, including the SAS token.
            return sasUri;
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob container.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="storedPolicyName">A string containing the name of the stored access policy.</param>
        /// <param name="StorageSharedKeyCredential">Storage Shared Key Credential.</param>
        /// <returns>A string containing the URI for the container, with the SAS token appended.</returns>
        static UriBuilder GetContainerSasUri(BlobContainerClient container, string storeAccessPolicyName, StorageSharedKeyCredential storageSharedKeyCredential)
        {
            var policy = new BlobSasBuilder
            {
                BlobContainerName = container.Name,
                Identifier = storeAccessPolicyName

            };
            var sas = policy.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            UriBuilder sasUri = new UriBuilder(container.Uri);
            sasUri.Query = sas;
            //Return the URI string for the container, including the SAS token.
            return sasUri;
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="StorageSharedKeyCredential">Storage Shared Key Credential.</param>
        /// <returns>A string containing the URI for the blob, with the SAS token appended.</returns>
        static UriBuilder GetBlobSasUri(BlobContainerClient container, string blobName, StorageSharedKeyCredential storageSharedKeyCredential)
        {
            //Get a reference to a blob within the container.
            //Note that the blob may not exist yet, but a SAS can still be created for it.
            BlobClient blob = container.GetBlobClient(blobName);


            var policy = new BlobSasBuilder

            {
                Protocol = SasProtocol.HttpsAndHttp,
                BlobContainerName = container.Name,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                IPRange = new SasIPRange(IPAddress.None, IPAddress.None)
            };
            policy.SetPermissions(BlobSasPermissions.All);
            var sas = policy.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            UriBuilder sasUri = new UriBuilder(blob.Uri);
            sasUri.Query = sas;
            //Return the URI string for the container, including the SAS token.
            return sasUri;
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="policyName">A string containing the name of the stored access policy.</param>
        /// <param name="StorageSharedKeyCredential">Storage Shared Key Credential.</param>
        /// <returns>A string containing the URI for the blob, with the SAS token appended.</returns>
        static UriBuilder GetBlobSasUri(BlobContainerClient container, string blobName, string storeAccessPolicyName, StorageSharedKeyCredential storageSharedKeyCredential)
        {
            //Get a reference to a blob within the container.
            //Note that the blob may not exist yet, but a SAS can still be created for it.
            BlobClient blob = container.GetBlobClient(blobName);


            var policy = new BlobSasBuilder

            {
                BlobContainerName = container.Name,
                BlobName = blobName,
                Identifier = storeAccessPolicyName
            };
            var sas = policy.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            UriBuilder sasUri = new UriBuilder(blob.Uri);
            sasUri.Query = sas;
            //Return the URI string for the container, including the SAS token.
            return sasUri;
        }



        static void CreateStoreAccessPolicy(BlobContainerClient container, string policyName)
        {
            IEnumerable<BlobSignedIdentifier> permissions = new[]
            {
                new BlobSignedIdentifier
                {
                    Id = policyName,
                    AccessPolicy =
                        new BlobAccessPolicy
                        {
                            PolicyStartsOn = DateTimeOffset.UtcNow.AddHours(-1),
                            PolicyExpiresOn =  DateTimeOffset.UtcNow.AddHours(1),
                            Permissions = "racwdl"
                        }
                }
            };

            container.SetAccessPolicy(PublicAccessType.None, permissions);
        }

        /// <summary>
        /// Tests a container SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        static void TestContainerSAS(UriBuilder sasUri, string blobName, string blobContent)
        {
            //Try performing container operations with the SAS provided.
            //Note that the storage account credentials are not required here; the SAS provides the necessary
            //authentication information on the URI.

            //Return a reference to the container using the SAS URI.
            BlobContainerClient container = new BlobContainerClient(sasUri.Uri);

            //Return a reference to a blob to be created in the container.
            BlobClient blob = container.GetBlobClient(blobName);

            //Write operation: Upload a new blob to the container.
            try
            {
                blob.Upload(BinaryData.FromString(blobContent));
                Console.WriteLine("Write operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Write operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //List operation: List the blobs in the container.
            try
            {
                foreach (BlobItem blobItem in container.GetBlobs())
                {
                    Console.WriteLine(blobItem.Name);
                }
                Console.WriteLine("List operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("List operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //Read operation: Read the contents of the blob we created above.
            try
            {

                BlobDownloadInfo download = blob.Download();
                Console.WriteLine(download.ContentLength);
                Console.WriteLine();
                Console.WriteLine("Read operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Read operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
            Console.WriteLine();

            //Delete operation: Delete the blob we created above.
            try
            {
                blob.Delete();
                Console.WriteLine("Delete operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Delete operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Tests a blob SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        static void TestBlobSAS(UriBuilder sasUri, string blobContent)
        {
            //Try performing blob operations using the SAS provided.

            //Return a reference to the blob using the SAS URI.
            BlobClient blob = new BlobClient(sasUri.Uri);

            //Create operation: Upload a blob with the specified name to the container.
            //If the blob does not exist, it will be created. If it does exist, it will be overwritten.
            try
            {
                //string blobContent = "This blob was created with a shared access signature granting write permissions to the blob. ";
                blob.Upload(BinaryData.FromString(blobContent));
                Console.WriteLine("Create operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Create operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            // Write operation: Add metadata to the blob
            try
            {
                IDictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add("name", "value");
                blob.SetMetadata(metadata);
                Console.WriteLine("Write operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Write operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //Read operation: Read the contents of the blob.
            try
            {
                BlobDownloadResult download = blob.DownloadContent();
                string content = download.Content.ToString();
                Console.WriteLine(content);
                Console.WriteLine();

                Console.WriteLine("Read operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Read operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //Delete operation: Delete the blob.
            try
            {
                blob.Delete();
                Console.WriteLine("Delete operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("Delete operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
        }
    }
}
