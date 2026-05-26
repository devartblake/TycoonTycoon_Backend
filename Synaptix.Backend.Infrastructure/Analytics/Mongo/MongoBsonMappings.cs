using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Infrastructure.Analytics.Mongo;

internal static class MongoBsonMappings
{
    private static readonly GuidSerializer StandardGuidSerializer = new(GuidRepresentation.Standard);

    public static void Register()
    {
        RegisterQuestionAnsweredAnalyticsEvent();
        RegisterQuestionAnsweredPlayerDailyRollup();
    }

    private static void RegisterQuestionAnsweredAnalyticsEvent()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(QuestionAnsweredAnalyticsEvent)))
            return;

        BsonClassMap.RegisterClassMap<QuestionAnsweredAnalyticsEvent>(map =>
        {
            map.AutoMap();
            map.MapMember(x => x.MatchId).SetSerializer(StandardGuidSerializer);
            map.MapMember(x => x.PlayerId).SetSerializer(StandardGuidSerializer);
        });
    }

    private static void RegisterQuestionAnsweredPlayerDailyRollup()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(QuestionAnsweredPlayerDailyRollup)))
            return;

        BsonClassMap.RegisterClassMap<QuestionAnsweredPlayerDailyRollup>(map =>
        {
            map.AutoMap();
            map.MapMember(x => x.PlayerId).SetSerializer(StandardGuidSerializer);
        });
    }
}
