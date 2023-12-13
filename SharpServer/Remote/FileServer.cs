using Amazon.S3;
using Amazon.S3.Model;

namespace SharpServer.Remote;

public class FileServer
{
    private static readonly FileServer Instance = new();
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    private FileServer()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = Environment.GetEnvironmentVariable("S3_URL")
        };
        _client = new AmazonS3Client(
            Environment.GetEnvironmentVariable("S3_ACCESS_KEY_ID"),
            Environment.GetEnvironmentVariable("S3_SECRET_ACCESS_KEY"),
            s3Config
        );
        _bucketName =
            Environment.GetEnvironmentVariable("S3_BUCKET_NAME")
            ?? throw new InvalidOperationException("S3_BUCKET_NAME is not set");
    }

    public static FileServer GetFileServer()
    {
        return Instance;
    }

    public async Task<bool> UploadFileAsync(string objectName, string filePath, string fileType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectName,
            FilePath = filePath,
            DisablePayloadSigning = true,
            ContentType = fileType
        };

        var response = await _client.PutObjectAsync(request);
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine($"Successfully uploaded {objectName} to {_bucketName}.");
            return true;
        }

        Console.WriteLine($"Could not upload {objectName} to {_bucketName}.");
        return false;
    }

    public async Task<bool> DownloadFileAsync(string objectName, string filePath)
    {
        var request = new GetObjectRequest { BucketName = _bucketName, Key = objectName, };

        using GetObjectResponse response = await _client.GetObjectAsync(request);
        try
        {
            await response.WriteResponseStreamToFileAsync(
                $"{filePath}\\{objectName}",
                true,
                CancellationToken.None
            );
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error saving {objectName}: {ex.Message}");
            return false;
        }
        finally
        {
            response.Dispose();
        }
    }
}
