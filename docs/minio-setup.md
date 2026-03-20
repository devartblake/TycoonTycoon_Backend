# MinIO – Bucket Setup and Usage Guide

MinIO is the S3-compatible object storage service included in the TycoonTycoon Docker stack. It provides a local, self-hosted alternative to AWS S3 for storing files, assets, and binary data during development and production.

---

## 1. Accessing MinIO

### Web Console

Once the stack is running (`make -f docker/MakeFile up`), the MinIO web console is available at:

```
http://localhost:9001
```

**Default credentials (development only):**

| Field    | Value                      |
| -------- | -------------------------- |
| Username | `tycoon_minio_user`        |
| Password | `tycoon_minio_password_123`|

> **Production:** Credentials are set via `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` environment variables. Never use the development defaults in production.

### S3 API Endpoint

Applications connect to MinIO via its S3-compatible API:

```
http://localhost:9000
```

---

## 2. Creating Buckets

### Option A: Web Console (Recommended for Development)

1. Open [http://localhost:9001](http://localhost:9001) and log in.
2. Click **Buckets** in the left sidebar.
3. Click **Create Bucket**.
4. Enter a bucket name (lowercase, hyphens allowed, no spaces) — e.g. `tycoon-assets`.
5. Configure optional settings:
   - **Versioning** – keeps a history of every object version.
   - **Object Locking** – prevents objects from being deleted or overwritten.
6. Click **Create Bucket**.

### Option B: MinIO Client (`mc`) via Docker

You can run `mc` commands directly inside the MinIO container:

```bash
# Open a shell in the MinIO container
make -f docker/MakeFile shell-minio

# Inside the container – configure the local alias
mc alias set local http://localhost:9000 tycoon_minio_user tycoon_minio_password_123

# Create a bucket
mc mb local/tycoon-assets

# List all buckets
mc ls local
```

Or as a one-liner from the host:

```bash
docker compose -f docker/compose.yml exec minio \
  mc mb local/tycoon-assets --insecure
```

### Option C: AWS CLI (if installed on host)

```bash
aws --endpoint-url http://localhost:9000 \
    --no-sign-request \
    s3 mb s3://tycoon-assets
```

---

## 3. Bucket Naming Conventions

Use consistent bucket names to keep the stack organised:

| Bucket            | Purpose                                 |
| ----------------- | --------------------------------------- |
| `tycoon-assets`   | Public-facing static assets (images, icons) |
| `tycoon-uploads`  | User-uploaded files (avatars, documents)    |
| `tycoon-exports`  | Generated exports (reports, CSV files)      |
| `tycoon-backups`  | Database or data backups                    |

---

## 4. Bucket Policies (Access Control)

By default, new buckets are **private**. Set a policy to allow public read access where needed (e.g. for static assets).

### Make a bucket publicly readable (via `mc`)

```bash
# Inside the container
mc policy set download local/tycoon-assets
```

### Custom policy (JSON)

To apply a fine-grained policy, create a `policy.json` file and apply it:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": "*",
      "Action": ["s3:GetObject"],
      "Resource": ["arn:aws:s3:::tycoon-assets/*"]
    }
  ]
}
```

```bash
mc anonymous set-json policy.json local/tycoon-assets
```

---

## 5. Uploading and Downloading Objects

### Via Web Console

1. Navigate to **Buckets** → select your bucket.
2. Click **Upload** to upload files or folders.
3. Click any object to download it or copy its URL.

### Via `mc`

```bash
# Upload a single file
mc cp ./logo.png local/tycoon-assets/logo.png

# Upload a directory recursively
mc cp --recursive ./dist/ local/tycoon-assets/

# Download a file
mc cp local/tycoon-assets/logo.png ./logo-download.png

# List objects in a bucket
mc ls local/tycoon-assets

# Remove an object
mc rm local/tycoon-assets/logo.png
```

---

## 6. Presigned URLs

Presigned URLs grant temporary access to a private object without exposing credentials.

### Generate a presigned URL via `mc` (valid for 1 hour)

```bash
mc share download --expire 1h local/tycoon-uploads/report.pdf
```

### Generate a presigned URL in .NET (AWSSDK.S3 / Minio SDK)

#### Using the official Minio .NET SDK

```csharp
var minioClient = new MinioClient()
    .WithEndpoint("localhost", 9000)
    .WithCredentials("tycoon_minio_user", "tycoon_minio_password_123")
    .WithSSL(false)
    .Build();

var args = new PresignedGetObjectArgs()
    .WithBucket("tycoon-uploads")
    .WithObject("report.pdf")
    .WithExpiry(3600); // seconds

string url = await minioClient.PresignedGetObjectAsync(args);
```

#### Using AWSSDK.S3 (S3-compatible mode)

```csharp
var config = new AmazonS3Config
{
    ServiceURL = "http://localhost:9000",
    ForcePathStyle = true, // required for MinIO
    UseHttp = true
};

var credentials = new BasicAWSCredentials("tycoon_minio_user", "tycoon_minio_password_123");
var client = new AmazonS3Client(credentials, config);

var request = new GetPreSignedUrlRequest
{
    BucketName = "tycoon-uploads",
    Key = "report.pdf",
    Expires = DateTime.UtcNow.AddHours(1)
};

string url = client.GetPreSignedURL(request);
```

---

## 7. Connection Settings for the .NET Application

Add the following to `appsettings.json` or inject via environment variables:

```json
{
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "tycoon_minio_user",
    "SecretKey": "tycoon_minio_password_123",
    "UseSSL": false,
    "DefaultBucket": "tycoon-assets"
  }
}
```

When running inside Docker (e.g. `backend-api` container), use the service name instead of `localhost`:

```json
{
  "MinIO": {
    "Endpoint": "minio:9000",
    "AccessKey": "tycoon_minio_user",
    "SecretKey": "tycoon_minio_password_123",
    "UseSSL": false
  }
}
```

Or as Docker environment variables in `compose.yml`:

```yaml
MinIO__Endpoint: "minio:9000"
MinIO__AccessKey: "${MINIO_ROOT_USER:-tycoon_minio_user}"
MinIO__SecretKey: "${MINIO_ROOT_PASSWORD:-tycoon_minio_password_123}"
MinIO__UseSSL: "false"
```

---

## 8. Health Check

Verify MinIO is running:

```bash
curl -f http://localhost:9000/minio/health/live
```

Expected response: HTTP `200 OK`.

Or via the Makefile:

```bash
make -f docker/MakeFile health
```

---

## 9. Useful References

- [MinIO Documentation](https://min.io/docs/minio/linux/index.html)
- [MinIO .NET SDK](https://github.com/minio/minio-dotnet)
- [MinIO `mc` CLI Reference](https://min.io/docs/minio/linux/reference/minio-mc.html)
- [S3-compatible API (AWS SDK for .NET)](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/s3-apis-intro.html)
