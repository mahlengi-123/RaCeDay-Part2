namespace RaCeDay_Part_2.Services
{
    public class BlobUploadResult
    {
        private string blobName;
        private string v;

        public BlobUploadResult(string blobName, string v)
        {
            this.blobName = blobName;
            this.v = v;
        }

        public string? ImageUrl { get; internal set; }
        public string? BlobName { get; internal set; }
    }
}