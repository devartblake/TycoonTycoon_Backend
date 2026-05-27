using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminStore;

public sealed class AdminStoreEndpointsContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _anon;

    public AdminStoreEndpointsContractTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _anon = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // Auth gating
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("GET",  "/admin/store/catalog")]
    [InlineData("POST", "/admin/store/catalog")]
    [InlineData("GET",  "/admin/store/system/status")]
    public async Task AdminStore_Requires_Admin_Ops_Key(string method, string path)
    {
        var req = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            req.Content = JsonContent.Create(new { });

        var resp = await _anon.SendAsync(req);

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    // -------------------------------------------------------------------------
    // Catalog CRUD
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateItem_HappyPath_Returns_201_With_Id_And_Sku()
    {
        var sku = UniqueSku();
        var resp = await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(sku));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
        body.GetProperty("sku").GetString().Should().Be(sku);
    }

    [Fact]
    public async Task CreateItem_DuplicateSku_Returns_409_Conflict()
    {
        var sku = UniqueSku();
        await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(sku));

        var dup = await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(sku));

        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await dup.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("SKU_CONFLICT");
    }

    [Theory]
    [InlineData("", "Valid Name", 100, 0, 1)]    // missing sku
    [InlineData("valid-sku", "", 100, 0, 1)]      // missing name
    [InlineData("valid-sku2", "Name", -1, 0, 1)] // negative coins
    [InlineData("valid-sku3", "Name", 0, -1, 1)] // negative diamonds
    [InlineData("valid-sku4", "Name", 0, 0, 0)]  // grantQuantity = 0
    public async Task CreateItem_InvalidInput_Returns_400(string sku, string name, int coins, int diamonds, int grantQty)
    {
        var resp = await _admin.PostAsJsonAsync("/admin/store/catalog", new
        {
            sku, name, priceCoins = coins, priceDiamonds = diamonds, grantQuantity = grantQty,
            maxPerPlayer = 0, sortOrder = 0,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListCatalog_Returns_Paginated_Shape()
    {
        await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(UniqueSku()));

        var resp = await _admin.GetAsync("/admin/store/catalog?page=1&pageSize=5");

        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        body.TryGetProperty("total", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateItem_HappyPath_Returns_200_With_UpdatedAt()
    {
        var sku = UniqueSku();
        var created = await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(sku));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await _admin.PatchAsJsonAsync($"/admin/store/catalog/{id}", new
        {
            name = "Updated Name",
            priceCoins = 500,
        });

        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(id);
        body.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateItem_NonExistent_Returns_404()
    {
        var resp = await _admin.PatchAsJsonAsync($"/admin/store/catalog/{Guid.NewGuid()}", new { name = "X" });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteItem_HappyPath_Returns_204()
    {
        var sku = UniqueSku();
        var created = await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(sku));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await _admin.DeleteAsync($"/admin/store/catalog/{id}");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteItem_AlreadyDeleted_Returns_409()
    {
        var sku = UniqueSku();
        var created = await _admin.PostAsJsonAsync("/admin/store/catalog", NewItemPayload(sku));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await _admin.DeleteAsync($"/admin/store/catalog/{id}");

        var second = await _admin.DeleteAsync($"/admin/store/catalog/{id}");

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("ALREADY_INACTIVE");
    }

    // -------------------------------------------------------------------------
    // System status
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SystemStatus_Get_Returns_Flags()
    {
        var resp = await _admin.GetAsync("/admin/store/system/status");

        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("storePurchasesEnabled", out _).Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string UniqueSku() => $"test-sku-{Guid.NewGuid():N}"[..30];

    private static object NewItemPayload(string sku) => new
    {
        sku,
        name = $"Test Item {sku}",
        description = "Test",
        itemType = "misc",
        priceCoins = 100,
        priceDiamonds = 0,
        grantQuantity = 1,
        maxPerPlayer = 0,
        sortOrder = 0,
    };
}
