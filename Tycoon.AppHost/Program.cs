using Tycoon.Hosting.Elasticsearch;

var builder = DistributedApplication.CreateBuilder(args);

// Parameters (keep in appsettings.json -> Parameters)
var postgresAdmin = builder.AddParameter("postgres-admin");
var admin = builder.AddParameter("admin");
var password = builder.AddParameter("admin-password", secret: true);

// Infra
var redis = builder.AddRedis("cache");

var messaging = builder
    .AddRabbitMQ("messaging", admin, password, port: 5672)
    .WithDataVolume()
    .WithManagementPlugin();

// Postgres = source of truth
var tycoonDb = builder
    .AddPostgres("postgres", postgresAdmin, password)
    .WithDataVolume()
    .WithPgAdmin(c => c.WithHostPort(5050))
    .AddDatabase("tycoon-db");

// Mongo = analytics/events (optional, but aligns with your plan)
var analyticsDb = builder
    .AddMongoDB("analytics-mongodb")
    .WithDataVolume()
    .WithMongoExpress(c => c.WithHostPort(8081))
    .AddDatabase("tycoon-analytics");

// Elasticsearch = search/aggregations (optional)
var search = builder
    .AddElasticsearch("search", password: password, port: 9200)
    .WithDataVolume();

// Your API project
var api = builder
    .AddProject("tycoon-api", "../Tycoon.Backend.Api/Tycoon.Backend.Api.csproj")
    .WithReference(tycoonDb)
    .WithReference(analyticsDb)
    .WithReference(search)
    .WithReference(redis)
    .WithReference(messaging);

// Optional: migrator/indexer project
var migrator = builder
    .AddProject("tycoon-migrator", "../Tycoon.MigrationService/Tycoon.MigrationService.csproj")
    .WithReference(tycoonDb)
    .WithReference(analyticsDb)
    .WithReference(search)
    .WithReference(messaging);

api.WaitFor(migrator);

builder.Build().Run();
