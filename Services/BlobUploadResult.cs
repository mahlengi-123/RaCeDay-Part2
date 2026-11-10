namespace RaCeDay_Part_2.Services;

public sealed class BlobUploadResult
{
    public BlobUploadResult(string blobName, string imageUrl)
    {
        BlobName = blobName;
        ImageUrl = imageUrl;
    }

    public string BlobName { get; }
    public string ImageUrl { get; }
}
