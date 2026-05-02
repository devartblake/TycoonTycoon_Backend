using System.Text.Json.Serialization;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Infrastructure.Redis;

[JsonSerializable(typeof(SecureSession))]
internal sealed partial class SessionSerializerContext : JsonSerializerContext;
