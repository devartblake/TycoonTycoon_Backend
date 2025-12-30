namespace Tycoon.Shared.Abstractions.Web
{
    public interface IProblemDetailMapper
    {
        int GetMappedStatusCodes(Exception exception);
    }
}
