using Microsoft.Extensions.Configuration;
using Serilog;
using Synaptix.Setup;
using Synaptix.Setup.Bootstrap;
using Synaptix.Setup.Commands;
using Synaptix.Setup.Secrets;
using Synaptix.Setup.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
var remainingArgs = args.Skip(1).ToArray();

try
{
    var config = BuildConfiguration(remainingArgs);

    var exitCode = command switch
    {
        "init-local"              => await InitLocalCommand.RunAsync(config, remainingArgs),
        "validate"                => await ValidateCommand.RunAsync(config, remainingArgs),
        "provision-services"      => await ProvisionServicesCommand.RunAsync(config, remainingArgs),
        "provision-minio"         => await ProvisionMinioCommand.RunAsync(config, remainingArgs),
        "upload-seeds"            => await UploadSeedsCommand.RunAsync(config, remainingArgs),
        "validate-seeds"          => await ValidateSeedsCommand.RunAsync(config, remainingArgs),
        "create-super-admin"      => await CreateSuperAdminCommand.RunAsync(config, remainingArgs),
        "rotate-super-admin-password" => await RotateSuperAdminPasswordCommand.RunAsync(config, remainingArgs),
        "status"                  => await StatusCommand.RunAsync(config, remainingArgs),
        "help" or "--help" or "-h" => PrintHelp(),
        _ => PrintUnknown(command),
    };

    return exitCode;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Setup command '{Command}' failed with an unhandled exception.", command);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static IConfiguration BuildConfiguration(string[] args)
{
    var envFile = args.SkipWhile(a => a != "--env").Skip(1).FirstOrDefault()
                  ?? Path.Combine(FindRepoRoot(), "docker", ".env");

    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(Path.Combine(FindRepoRoot(), "config", "bootstrap", "bootstrap.local.json"), optional: true)
        .AddEnvironmentVariables()
        .AddDotNetEnv(envFile)
        .Build();
}

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir, "TycoonTycoon_Backend.slnx")) ||
            File.Exists(Path.Combine(dir, "TycoonTycoon_Backend.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    return AppContext.BaseDirectory;
}

static int PrintHelp()
{
    Console.WriteLine("""
        Synaptix.Setup — Bootstrap and provision the Synaptix backend environment.

        Usage:
          dotnet run --project Synaptix.Setup -- <command> [options]

        Commands:
          init-local                Generate docker/.env with strong random secrets for local dev.
          validate                  Validate the current .env file for weak/placeholder secrets.
          provision-services        Provision all infrastructure services (MinIO, RabbitMQ, etc).
          provision-minio           Create the MinIO bucket and ensure required prefixes exist.
          upload-seeds              Upload bundled seed JSON files to MinIO.
          validate-seeds            Verify all required seed files exist in MinIO.
          create-super-admin        Create the super admin account in the database.
          rotate-super-admin-password  Update the super admin password.
          status                    Show bootstrap status report.
          help                      Show this help.

        Options:
          --env <path>              Path to the .env file (default: docker/.env).
          --local                   Force local-safe mode (allows generated secrets).
          --strict                  Fail on any warning (for CI/staging use).

        Examples:
          dotnet run --project Synaptix.Setup -- init-local
          dotnet run --project Synaptix.Setup -- provision-services --strict
          dotnet run --project Synaptix.Setup -- validate --environment staging --strict
        """);
    return 0;
}

static int PrintUnknown(string cmd)
{
    Log.Error("Unknown command: '{Command}'. Run 'help' for usage.", cmd);
    return 1;
}
