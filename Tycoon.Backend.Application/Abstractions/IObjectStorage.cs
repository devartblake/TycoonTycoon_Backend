namespace Tycoon.Backend.Application.Abstractions
{
    public interface IObjectStorage
    {
        Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct = default);
        string GetPublicUrl(string key);
    }
}
