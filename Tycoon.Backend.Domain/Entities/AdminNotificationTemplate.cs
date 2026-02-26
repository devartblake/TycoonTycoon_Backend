namespace Tycoon.Backend.Domain.Entities;

public sealed class AdminNotificationTemplate
{
    public string TemplateId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string ChannelKey { get; private set; } = string.Empty;
    public string VariablesJson { get; private set; } = "[]";
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private AdminNotificationTemplate() { }

    public AdminNotificationTemplate(string templateId, string name, string title, string body, string channelKey, string variablesJson)
    {
        TemplateId = templateId;
        Update(name, title, body, channelKey, variablesJson);
    }

    public void Update(string name, string title, string body, string channelKey, string variablesJson)
    {
        Name = name;
        Title = title;
        Body = body;
        ChannelKey = channelKey;
        VariablesJson = variablesJson;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
