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
using System.Text;
using System.IO;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

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
            const string blobContent1 = "Blob created with an ad-hoc SAS granting write permissions on the container.";

            const string blobName2 = "sasBlob2.txt";
            const string blobContent2 = "Blob created with a SAS based on a stored access policy granting write permissions on the container.";

            const string blobName3 = "sasBlob3.txt";
            const string blobContent3 = "Blob created with an ad-hoc SAS granting create/write permissions to the blob.";

            const string blobName4 = "sasBlob4.txt";
            const string blobContent4 = "Blob created with a SAS based on a stored access policy granting create/write permissions to the blob."; ;

            string containerName = containerPrefix + DateTime.Now.Ticks.ToString();
            string sharedAccessPolicyName = policyPrefix + DateTime.Now.Ticks.ToString();

            //Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //Create the blob client object.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Get a reference to a container to use for the sample code, and create it if it does not exist.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            try 
            { 
                container.CreateIfNotExists();
            }
            catch (StorageException)
            {
                // Ensure that the storage emulator is running if using emulator connection string.
                Console.WriteLine("If you are running with the default connection string, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                Console.ReadLine();
                throw;
            }

            //Create a new access policy on the container, which may be optionally used to provide constraints for
            //shared access signatures on the container and the blob.
            //The access policy provides create, write, read, list, and delete permissions.
            CreateSharedAccessPolicy(container, sharedAccessPolicyName);

            //Generate an ad-hoc SAS URI for the container. The ad-hoc SAS has write and list permissions.
            string adHocContainerSAS = GetContainerSasUri(container);
            Console.WriteLine("1. SAS for blob container (ad hoc): " + adHocContainerSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The write and list operations should succeed, and the read and delete operations should fail.
            TestContainerSAS(adHocContainerSAS, blobName1, blobContent1);
            Console.WriteLine();

            //Generate a SAS URI for the container, using the stored access policy to set constraints on the SAS.
            string sharedPolicyContainerSAS = GetContainerSasUri(container, sharedAccessPolicyName);
            Console.WriteLine("2. SAS for blob container (stored access policy): " + sharedPolicyContainerSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The write, read, list, and delete operations should all succeed.
            TestContainerSAS(sharedPolicyContainerSAS, blobName2, blobContent2);
            Console.WriteLine();

            //Generate an ad-hoc SAS URI for a blob within the container. The ad-hoc SAS has create, write, and read permissions.
            string adHocBlobSAS = GetBlobSasUri(container, blobName3, null);
            Console.WriteLine("3. SAS for blob (ad hoc): " + adHocBlobSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The create, write, and read operations should succeed, and the delete operation should fail.
            TestBlobSAS(adHocBlobSAS, blobContent3);
            Console.WriteLine();

            //Generate a SAS URI for a blob within the container, using the stored access policy to set constraints on the SAS.
            string sharedPolicyBlobSAS = GetBlobSasUri(container, blobName4, sharedAccessPolicyName);
            Console.WriteLine("4. SAS for blob (stored access policy): " + sharedPolicyBlobSAS);
            Console.WriteLine();

            //Test the SAS to ensure it works as expected.
            //The create, write, read, and delete operations should all succeed.
            TestBlobSAS(sharedPolicyBlobSAS, blobContent4);
            Console.WriteLine();

            //Delete the container to clean up.
            container.DeleteIfExists();

            Console.ReadLine();
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob container.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="storedPolicyName">A string containing the name of the stored access policy. If null, an ad-hoc SAS is created.</param>
        /// <returns>A string containing the URI for the container, with the SAS token appended.</returns>
        static string GetContainerSasUri(CloudBlobContainer container, string storedPolicyName = null)
        {
            string sasContainerToken;

            // If no stored policy is specified, create a new access policy and define its constraints.
            if (storedPolicyName == null)
            {
                // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
                // to construct a shared access policy that is saved to the container's shared access policies. 
                SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
                {
                    // Set start time to five minutes before now to avoid clock skew.
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List
                };

                //Generate the shared access signature on the container, setting the constraints directly on the signature.
                sasContainerToken = container.GetSharedAccessSignature(adHocPolicy, null);
            }
            else
            {
                //Generate the shared access signature on the container. In this case, all of the constraints for the
                //shared access signature are specified on the stored access policy, which is provided by name.
                //It is also possible to specify some constraints on an ad-hoc SAS and others on the stored access policy.
                //However, a constraint must be specified on one or the other; it cannot be specified on both.
                sasContainerToken = container.GetSharedAccessSignature(null, storedPolicyName);
            }

            //Return the URI string for the container, including the SAS token.
            return container.Uri + sasContainerToken;
        }

        /// <summary>
        /// Returns a URI containing a SAS for the blob.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="policyName">A string containing the name of the stored access policy. If null, an ad-hoc SAS is created.</param>
        /// <returns>A string containing the URI for the blob, with the SAS token appended.</returns>
        static string GetBlobSasUri(CloudBlobContainer container, string blobName, string policyName = null)
        {
            string sasBlobToken;

            //Get a reference to a blob within the container.
            //Note that the blob may not exist yet, but a SAS can still be created for it.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            if (policyName == null)
            {
                // Create a new access policy and define its constraints.
                // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
                // to construct a shared access policy that is saved to the container's shared access policies. 
                SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
                {
                    // Set start time to five minutes before now to avoid clock skew.
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
                };

                //Generate the shared access signature on the blob, setting the constraints directly on the signature.
                sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);
            }
            else
            {
                //Generate the shared access signature on the blob. In this case, all of the constraints for the
                //shared access signature are specified on the container's stored access policy.
                sasBlobToken = blob.GetSharedAccessSignature(null, policyName);
            }

            //Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken;
        }

        /// <summary>
        /// Creates a shared access policy on the container.
        /// </summary>
        /// <param name="container">A reference to the container.</param>
        /// <param name="policyName">The name of the stored access policy.</param>
        static void CreateSharedAccessPolicy(CloudBlobContainer container,
            string policyName)
        {
            //Create a new shared access policy and define its constraints.
            SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List |
                    SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Delete
            };

            //Get the container's existing permissions.
            BlobContainerPermissions permissions = container.GetPermissions();

            //Add the new policy to the container's permissions, and set the container's permissions.
            permissions.SharedAccessPolicies.Add(policyName, sharedPolicy);
            container.SetPermissions(permissions);
        }

        /// <summary>
        /// Tests a container SAS to determine which operations it allows.
        /// </summary>
        /// <param name="sasUri">A string containing a URI with a SAS appended.</param>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <param name="blobContent">A string content content to write to the blob.</param>
        static void TestContainerSAS(string sasUri, string blobName, string blobContent)
        {
            //Try performing container operations with the SAS provided.
            //Note that the storage account credentials are not required here; the SAS provides the necessary
            //authentication information on the URI.

            //Return a reference to the container using the SAS URI.
            CloudBlobContainer container = new CloudBlobContainer(new Uri(sasUri));

            //Return a reference to a blob to be created in the container.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            //Write operation: Upload a new blob to the container.
            try
            {
                MemoryStream msWrite = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
                msWrite.Position = 0;
                using (msWrite)
                {
                    blob.UploadFromStream(msWrite);
                }
                Console.WriteLine("Write operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine("Write operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //List operation: List the blobs in the container.
            try
            {
                foreach (ICloudBlob blobItem in container.ListBlobs())
                {
                    Console.WriteLine(blobItem.Uri);
                }
                Console.WriteLine("List operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine("List operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //Read operation: Read the contents of the blob we created above.
            try
            {
                MemoryStream msRead = new MemoryStream();
                msRead.Position = 0;
                using (msRead)
                {
                    blob.DownloadToStream(msRead);
                    Console.WriteLine(msRead.Length);
                }
                Console.WriteLine();
                Console.WriteLine("Read operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
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
            catch (StorageException e)
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
        static void TestBlobSAS(string sasUri, string blobContent)
        {
            //Try performing blob operations using the SAS provided.

            //Return a reference to the blob using the SAS URI.
            CloudBlockBlob blob = new CloudBlockBlob(new Uri(sasUri));

            //Create operation: Upload a blob with the specified name to the container.
            //If the blob does not exist, it will be created. If it does exist, it will be overwritten.
            try
            {
                //string blobContent = "This blob was created with a shared access signature granting write permissions to the blob. ";
                MemoryStream msWrite = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
                msWrite.Position = 0;
                using (msWrite)
                {
                    blob.UploadFromStream(msWrite);
                }
                Console.WriteLine("Create operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine("Create operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            // Write operation: Add metadata to the blob
            try
            {
                blob.FetchAttributes();
                string rnd = new Random().Next().ToString();
                string metadataName = "name";
                string metadataValue = "value";
                blob.Metadata.Add(metadataName, metadataValue);
                blob.SetMetadata();

                Console.WriteLine("Write operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
            {
                Console.WriteLine("Write operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }

            //Read operation: Read the contents of the blob.
            try
            {
                MemoryStream msRead = new MemoryStream();
                using (msRead)
                {
                    blob.DownloadToStream(msRead);
                    msRead.Position = 0;
                    using (StreamReader reader = new StreamReader(msRead, true))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Console.WriteLine(line);
                        }
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Read operation succeeded for SAS " + sasUri);
                Console.WriteLine();
            }
            catch (StorageException e)
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
            catch (StorageException e)
            {
                Console.WriteLine("Delete operation failed for SAS " + sasUri);
                Console.WriteLine("Additional error information: " + e.Message);
                Console.WriteLine();
            }
        }
    }
}
