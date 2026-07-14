using Synaptix.Hosting.Elasticsearch;
using Synaptix.Hosting.Minio;

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
// Resource name becomes ConnectionStrings:synaptix-db (Aspire).
// DI still accepts legacy tycoon-db / tycoon_db keys for older env files.
var synaptixDb = builder
    .AddPostgres("postgres", postgresAdmin, password)
    .WithDataVolume()
    .WithPgAdmin(c => c.WithHostPort(5050))
    .AddDatabase("synaptix-db");

// Mongo = analytics/events
var analyticsDb = builder
    .AddMongoDB("analytics-mongodb")
    .WithDataVolume()
    .WithMongoExpress(c => c.WithHostPort(8081))
    .AddDatabase("synaptix-analytics");

// Elasticsearch = search/aggregations (optional)
var search = builder
    .AddElasticsearch("search", password: password, port: 9200)
    .WithDataVolume();

// MinIO = S3-compatible object storage for media assets
var minio = builder
    .AddMinio("minio", rootUser: minioUser, rootPassword: minioPassword, apiPort: 9000, consolePort: 9001)
    .WithDataVolume();

// API
var api = builder
    .AddProject("synaptix-api", "../Synaptix.Backend.Api/Synaptix.Backend.Api.csproj")
    .WithReference(synaptixDb)
    .WithReference(analyticsDb)
    .WithReference(search)
    .WithReference(redis)
    .WithReference(messaging)
    .WithMinioConnection(minio);

// Migrator / seed
var migrator = builder
    .AddProject("synaptix-migrator", "../Synaptix.MigrationService/Synaptix.MigrationService.csproj")
    .WithReference(synaptixDb)
    .WithReference(analyticsDb)
    .WithReference(search)
    .WithReference(messaging);

api.WaitFor(migrator);

// Operator dashboard: React (Docker Compose primary). Blazor Aspire project removed.
// Local React: docker compose up operator-dashboard-react  or npm run dev in OperatorDashboard.React

// FastAPI sidecar — ML inference, analytics pipelines, webhooks, utilities
var sidecar = builder
    .AddExecutable("synaptix-sidecar", "uvicorn", "../Synaptix.Sidecar",
        "app.main:app", "--host", "0.0.0.0", "--port", "8100")
    .WithHttpEndpoint(port: 8100, name: "http")
    .WithReference(analyticsDb)
    .WithReference(search);

// ExecutableResource does not implement IResourceWithConnectionString, so
// WithReference(sidecar) fails with CS1503. Pass the named endpoint instead —
// Aspire injects services__synaptix-sidecar__http__0.
api.WithReference(sidecar.GetEndpoint("http"));

builder.Build().Run();
