using Tycoon.Hosting.Elasticsearch;
using Tycoon.Hosting.Minio;

var builder = DistributedApplication.CreateBuilder(args);

// Parameters (keep in appsettings.json -> Parameters)
var postgresAdmin = builder.AddParameter("postgres-admin");
var admin = builder.AddParameter("admin");
var password = builder.AddParameter("admin-password", secret: true);
var minioUser = builder.AddParameter("minio-user");
var minioPassword = builder.AddParameter("minio-password", secret: true);

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

// MinIO = S3-compatible object storage for media assets
var minio = builder
    .AddMinio("minio", rootUser: minioUser, rootPassword: minioPassword, apiPort: 9000, consolePort: 9001)
    .WithDataVolume();

// Your API project
var api = builder
    .AddProject("tycoon-api", "../Tycoon.Backend.Api/Tycoon.Backend.Api.csproj")
    .WithReference(tycoonDb)
    .WithReference(analyticsDb)
    .WithReference(search)
    .WithReference(redis)
    .WithReference(messaging)
    .WithMinioConnection(minio);

// Optional: migrator/indexer project
var migrator = builder
    .AddProject("tycoon-migrator", "../Tycoon.MigrationService/Tycoon.MigrationService.csproj")
    .WithReference(tycoonDb)
    .WithReference(analyticsDb)
    .WithReference(search)
    .WithReference(messaging);

api.WaitFor(migrator);

// Operator dashboard moved to Tycoon.OperatorDashboard.Web (Next.js).
// Run separately: cd Tycoon.OperatorDashboard.Web && pnpm dev
var dashboard = builder
    .AddProject("tycoon-dashboard", "../Tycoon.OperatorDashboard/Tycoon.OperatorDashboard.csproj")
    .WithReference(api)
    .WithExternalHttpEndpoints();
dashboard.WaitFor(api);

// FastAPI sidecar — ML inference, analytics pipelines, webhooks, utilities
var sidecar = builder
    .AddExecutable("tycoon-sidecar", "uvicorn", "../Tycoon.Sidecar",
        "app.main:app", "--host", "0.0.0.0", "--port", "8100")
    .WithHttpEndpoint(port: 8100, name: "http")
    .WithReference(analyticsDb)
    .WithReference(search);

// FIX: ExecutableResource does not implement IResourceWithConnectionString, so
// WithReference(sidecar) fails with CS1503. Pass the named endpoint instead —
// Aspire injects it as an environment variable (services__tycoon-sidecar__http__0).
api.WithReference(sidecar.GetEndpoint("http"));

builder.Build().Run();