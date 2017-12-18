public class AwsS3FileRequest
{
    public string AccessKeyId { get; set; }

    public string SecretAccessKey { get; set; }

    public string SessionToken { get; set; }

    public string BucketName { get; set; }

    public string ObjectKey { get; set; }

    public string Uri { get; set; }
}