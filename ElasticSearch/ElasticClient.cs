namespace Elastic;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Products.Elasticsearch;
using System.Globalization;

public class ElasticClient(ElasticsearchClient client)
{
    private const string PostIndex = "posts";
    private const string LikeIndex = "likes";

    public async Task<IEnumerable<IndexedPost>> SearchPostsAsync(
        PostSearch search,
        CancellationToken cancellationToken = default
    )
    {
        var searchResponse = await client.SearchAsync<IndexedPost>(
            s => s
                .Indices(PostIndex)
                .From(0)
                .Size(10)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            sh => sh.Match(m => m
                                .Field(f => f.Title)
                                .Query(search.Title)
                            ),
                            sh => sh.Match(m => m
                                .Field(f => f.Content)
                                .Query(search.Content)
                            )
                        )
                    )
                ),
            cancellationToken
        );

        EnsureSuccess(searchResponse);

        return searchResponse.Documents;
    }

    public async Task<AnalyticsResponse> GetAnalyticsDataAsync(
        AnalyticsRequest request,
        CancellationToken cancellationToken = default
    )
    {
        const string Key = "user_likes";

        var aggregationResponse = await client.SearchAsync<IndexedLike>(
            s => s
                .Indices(LikeIndex)
                .Size(0)
                .Query(q =>
                {
                    if (request.start.HasValue && request.end.HasValue)
                    {
                        // ✅ FIXED: Convert DateTimeOffset to DateMath
                        q.Range(r => r
                            .DateRange(dr => dr
                                .Field(f => f.CreatedAt)
                                .Gte(request.start.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))
                                .Lte(request.start.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))
                            )
                        );
                    }
                    else
                    {
                        q.MatchAll(new MatchAllQuery());
                    }
                })
                .Aggregations(a => a
                    .Add(Key, agg => agg
                        .Terms(t => t
                            .Field(f => f.AuthorId)
                            .Size(5)
                        )
                    )
                ),
            cancellationToken
        );

        EnsureSuccess(aggregationResponse);

        Dictionary<long, long> leaderboard = [];

        // ✅ FIXED: Separate pattern matching for proper variable scope
        if (aggregationResponse.Aggregations is not null
            && aggregationResponse.Aggregations.TryGetValue(Key, out var likesByUser))
        {
            if (likesByUser is LongTermsAggregate aggregate)
            {
                leaderboard = aggregate.Buckets.ToDictionary(
                    b => b.Key,
                    b => b.DocCount
                );
            }
        }

        return new AnalyticsResponse(leaderboard);
    }

    public record AnalyticsRequest(
        DateTimeOffset? start = default,
        DateTimeOffset? end = default
    );

    public record AnalyticsResponse(Dictionary<long, long> Leaderboard);

    public async Task CreateAsync(
        IndexedPost post,
        CancellationToken cancellationToken = default
    )
    {
        var indexResponse = await client.IndexAsync(
            post,
            PostIndex,
            cancellationToken
        );

        EnsureSuccess(indexResponse);
    }

    public async Task CreateAsync(
        IndexedLike like,
        CancellationToken cancellationToken = default
    )
    {
        var indexResponse = await client.IndexAsync(
            like,
            LikeIndex,
            cancellationToken
        );

        EnsureSuccess(indexResponse);
    }

    public async Task CreateManyAsync(
        IEnumerable<IndexedPost> posts,
        CancellationToken cancellationToken = default
    )
    {
        var bulkResponse = await client.BulkAsync(
            b => b
                .Index(PostIndex)
                .IndexMany(posts),
            cancellationToken
        );

        EnsureSuccess(bulkResponse);
    }

    public async Task CreateManyAsync(
        IEnumerable<IndexedLike> likes,
        CancellationToken cancellationToken = default
    )
    {
        var bulkResponse = await client.BulkAsync(
            b => b
                .Index(LikeIndex)
                .IndexMany(likes),
            cancellationToken
        );

        EnsureSuccess(bulkResponse);
    }

    public async Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var deleteResponse = await client.DeleteAsync(
            PostIndex,
            id,
            cancellationToken
        );

        EnsureSuccess(deleteResponse);
    }

    public async Task<IndexedPost?> GetAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var getResponse = await client.GetAsync<IndexedPost>(
            PostIndex,
            id,
            cancellationToken
        );

        EnsureSuccess(getResponse);

        return getResponse.Source;
    }

    public async Task SetupAsync(CancellationToken cancellationToken = default)
    {
        await EnsureIndex(client, PostIndex, cancellationToken);
        await EnsureIndex(client, LikeIndex, cancellationToken);
    }

    private static async Task EnsureIndex(
        ElasticsearchClient client,
        string postIndex,
        CancellationToken cancellationToken
    )
    {
        var indexExistsResponse = await client.Indices.ExistsAsync(
            postIndex,
            cancellationToken
        );

        if (!indexExistsResponse.Exists)
        {
            await client.Indices.CreateAsync(
                postIndex,
                cancellationToken
            );
        }
    }

    private static void EnsureSuccess<TResponse>(TResponse response)
        where TResponse : ElasticsearchResponse
    {
        if (!response.IsValidResponse)
        {
            throw new ElasticsearchException(
                $"Elasticsearch operation failed: {response.DebugInformation}",
                response.ApiCallDetails.OriginalException
            );
        }
    }
}

public class ElasticsearchException : Exception
{
    public ElasticsearchException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

public class PostSearch
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
}