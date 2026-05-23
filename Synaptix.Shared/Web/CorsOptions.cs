namespace Synaptix.Shared.Web;

public class CorsOptions
{
    public IEnumerable<string> AllowedUrls { get; set; } = System.Array.Empty<string>();
}
