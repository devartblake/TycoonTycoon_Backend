# IMMEDIATE FIX: Swagger Duplicate Route Error

## Problem
```
SwaggerGeneratorException: Conflicting method/path combination "GET users/me"
for actions - HTTP: GET /users/me, HTTP: GET /users/me => RetrieveCurrentUser
```

You have the same endpoint registered twice in your API.

---

## Solution: Find and Delete One

### Step 1: Search for Duplicates

In your `Tycoon.Backend.Api` project, search for all instances of `users/me`:

**Using terminal:**
```bash
cd Tycoon.Backend.Api
grep -rn "users/me" . --include="*.cs"
```

**Using Visual Studio:**
- Press **Ctrl+Shift+F**
- Search for: `"users/me"`
- Scope: Current Project

### Step 2: Identify the Duplicates

You'll find something like this across **one or two files**:

**Example 1: Program.cs (Minimal API style)**
```csharp
// Line 85
app.MapGet("/users/me", async (HttpContext ctx) => 
{
    var userId = ctx.User.FindFirst("sub")?.Value;
    // ... returns user details
});

// Line 142 (DUPLICATE - same route!)
app.MapGet("/users/me", RetrieveCurrentUser);
```

**Example 2: UsersEndpoints.cs + Program.cs**
```csharp
// File: UsersEndpoints.cs
public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/me", async (HttpContext ctx) => { ... });
    }
}

// File: Program.cs
app.MapUsersEndpoints(); // ← Registers /users/me
app.MapGet("/users/me", RetrieveCurrentUser); // ← DUPLICATE!
```

### Step 3: Delete ONE of Them

**If both are identical:**
- Delete the second occurrence

**If different:**
- Compare the two implementations
- Keep the better one (likely the named handler `RetrieveCurrentUser`)
- Delete the other

### Step 4: Restart and Test

```bash
docker compose restart backend-api
```

Then visit: `http://localhost:5000/swagger`

It should load without errors now.

---

## Common Scenarios

### Scenario A: Extension Method + Inline Registration

**Problem:**
```csharp
// In UsersEndpoints.cs
app.MapGet("/users/me", GetCurrentUser);

// In Program.cs
app.MapUsersEndpoints();
app.MapGet("/users/me", RetrieveCurrentUser); // ← DUPLICATE
```

**Fix:** Delete the line in `Program.cs`

---

### Scenario B: Multiple Extension Method Calls

**Problem:**
```csharp
// In Program.cs
app.MapUsersEndpoints(); // Registers /users/me
app.MapAuthEndpoints();  // Also registers /users/me (oops!)
```

**Fix:** Check both `UsersEndpoints.cs` and `AuthEndpoints.cs`. Delete one.

---

### Scenario C: Base Path Collision

**Problem:**
```csharp
var users = app.MapGroup("/users");
users.MapGet("/me", GetCurrentUser); // → /users/me

app.MapGet("/users/me", RetrieveCurrentUser); // ← DUPLICATE
```

**Fix:** Delete the standalone `app.MapGet`

---

## After Fixing Swagger

You still need to:

1. **Add the missing `/auth/signup` endpoint** (see `backend_signup_endpoint.cs`)
2. **Consolidate your frontend auth systems** (see `frontend_backend_auth_analysis.md`)

But at least Swagger will work so you can test your API!
