using Amazon.S3;
using Amazon.S3.Model;

namespace FFMpegWrapper;

public class FileServer
{
    private static FileServer _instance = null;
    private IAmazonS3 client;

    private string bucketName;

    public const string BaseLink = "https://pub-ed3bc5875a1d45f8aacd58cb8f447141.r2.dev/";

    public FileServer()
    {
        AmazonS3Config s3Config = new AmazonS3Config
        {
            ServiceURL = "https://5f1f59614d2c85173d227fcc83ce4971.r2.cloudflarestorage.com"
        };
        client = new AmazonS3Client("8f99e382c9899098e3cdbca4deb0eab5",
            "26f2e54861d14c28c43b57f3d47f34cc8f518f5a9dd0cab83fe38e77bac9c067", s3Config);
        bucketName = "iukprojekt";
    }

    public static FileServer GetFileServer()
    {
        if (_instance == null)
        {
            _instance = new FileServer();
        }

        return _instance;
    }
    
    public async Task<bool> UploadFileAsync( string objectName, string filePath, string fileType)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
            FilePath = filePath,
            DisablePayloadSigning = true,
            ContentType = fileType
        };

        var response = await client.PutObjectAsync(request);
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
            return true;
        }

        Console.WriteLine($"Could not upload {objectName} to {bucketName}.");
        return false;
        
    }
    
    public async Task<bool> DownloadFileAsync( string objectName, string filePath)
    {
        
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
        };
        
        using GetObjectResponse response = await client.GetObjectAsync(request);
        try
        {
            await response.WriteResponseStreamToFileAsync($"{filePath}\\{objectName}", true, CancellationToken.None);
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