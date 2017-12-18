public class AzureBlobFileRequest
{
    public string StorageAccountName { get; set; }

    public string StorageAccountKey { get; set; }

    public string ContainerName { get; set; }

    public string ObjectKey { get; set; }

    public string Uri { get; set; }
}