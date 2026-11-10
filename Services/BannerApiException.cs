namespace RaCeDay_Part_2.Services;

public sealed class BannerApiException : Exception
{
    public BannerApiException(string message) : base(message) { }
}
