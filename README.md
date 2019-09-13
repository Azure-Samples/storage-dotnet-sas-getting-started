---
page_type: sample
languages:
- csharp
products:
- azure
description: "This sample shows how to generate and use shared access signatures."
urlFragment: storage-dotnet-sas-getting-started
---

# Getting Started with Shared Access Signatures (SAS)

This sample shows how to generate and use shared access signatures. With a shared access signature, you can delegate access to resources in your storage account, without sharing your account key. The sample demonstrates how to create both an ad-hoc SAS and a SAS associated with a stored access policy.

If you don't already have a Microsoft Azure subscription, get started with a FREE <a href="http://go.microsoft.com/fwlink/?LinkId=330212">trial account</a>.

## Running this sample

By default, this sample is configured to run against the storage emulator. You can also modify it to run against your Azure Storage account.

To run the sample using the storage emulator (default option):

1. Start the Azure storage emulator (once only) by pressing the Start button or the Windows key and searching for it
by typing "Azure storage emulator". Select it from the list of applications to start it.
2. Set breakpoints and run the project using F10. 

To run the sample using a storage account

1. Open the app.config file and comment out the connection string for the emulator (UseDevelopmentStorage=True) and
uncomment the connection string for the storage service (AccountName=[]...)
2. Create a storage account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information
3. Set breakpoints and run the project using F10. 

## More information
- [Shared Access Signatures: Understanding the SAS Model](https://azure.microsoft.com/documentation/articles/storage-dotnet-shared-access-signature-part-1/)
- [Create and use a SAS with Blob storage](https://azure.microsoft.com/documentation/articles/storage-dotnet-shared-access-signature-part-2/)
- [Delegating Access with a Shared Access Signature](https://msdn.microsoft.com/library/ee395415.aspx)
- [Constructing an Account SAS](https://msdn.microsoft.com/library/mt584140.aspx)
- [Constructing a Service SAS](https://msdn.microsoft.com/library/dn140255.aspx)
- [Establishing a Stored Access Policy](https://msdn.microsoft.com/library/dn140257.aspx)
- [Use the Azure storage emulator for Development and Testing](https://azure.microsoft.com/documentation/articles/storage-use-emulator/)
