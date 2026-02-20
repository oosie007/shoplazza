using ShoplazzaAddonApp.Data.Entities;
using ShoplazzaAddonApp.Models.Configuration;
using ShoplazzaAddonApp.Models.Api;

namespace ShoplazzaAddonApp.Tests.Utilities;

/// <summary>
/// Factory for creating test data used in unit and integration tests
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a test merchant entity
    /// </summary>
    public static Merchant CreateTestMerchant(string shop = "test-shop.myshoplaza.com")
    {
        return new Merchant
        {
            Id = 1,
            Shop = shop,
            StoreName = "Test Store",
            StoreEmail = "test@example.com",
            AccessToken = "test-access-token",
            Scopes = "read_products,write_products,read_orders,write_orders",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test function configuration entity
    /// </summary>
    public static FunctionConfiguration CreateTestFunctionConfiguration(int merchantId = 1)
    {
        return new FunctionConfiguration
        {
            Id = 1,
            MerchantId = merchantId,
            FunctionId = "test-function-id-123",
            FunctionName = "cart-transform-addon-test-shop",
            FunctionType = "cart-transform",
            Status = FunctionStatus.Active,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ActivatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
    }

    /// <summary>
    /// Creates a test function registration request
    /// </summary>
    public static FunctionRegistrationRequest CreateTestFunctionRegistrationRequest()
    {
        return new FunctionRegistrationRequest
        {
            Name = "cart-transform-addon-test",
            Type = "cart-transform",
            Description = "Test cart-transform function",
            WasmBase64 = Convert.ToBase64String(new byte[] { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 }), // Valid WASM header
            SourceCode = "// Test Rust source code\n#[no_mangle]\npub extern \"C\" fn processCart(_input_ptr: *mut u8, _input_len: usize) -> *mut u8 {\n    std::ptr::null_mut()\n}",
            Triggers = new List<string> { "cart.add", "cart.update" },
            Settings = new FunctionSettings
            {
                Timeout = 5000,
                MemoryLimit = "128MB",
                AutoEnable = false
            }
        };
    }

    /// <summary>
    /// Creates a test product add-on entity
    /// </summary>
    public static ProductAddOn CreateTestProductAddOn(int merchantId = 1)
    {
        return new ProductAddOn
        {
            Id = 1,
            MerchantId = merchantId,
            ProductId = "test-product-123",
            ProductTitle = "Test Product",
            ProductHandle = "test-product",
            IsEnabled = true,
            AddOnTitle = "Test Add-On",
            AddOnDescription = "Test add-on description",
            AddOnPriceCents = 999,
            Currency = "USD",
            DisplayText = "Add Test Add-On",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a list of test merchants
    /// </summary>
    public static List<Merchant> CreateTestMerchants(int count = 3)
    {
        var merchants = new List<Merchant>();
        for (int i = 1; i <= count; i++)
        {
            merchants.Add(CreateTestMerchant($"test-shop-{i}.myshoplazza.com"));
        }
        return merchants;
    }

    /// <summary>
    /// Creates test WASM bytes (valid WASM header)
    /// </summary>
    public static byte[] CreateTestWasmBytes()
    {
        // Create a minimal valid WASM file that meets size requirements (at least 1000 bytes)
        var wasmBytes = new byte[1024]; // 1KB to be safe
        
        // WASM magic number and version (must be exactly these bytes in this order)
        wasmBytes[0] = 0x00; // WASM magic number
        wasmBytes[1] = 0x61; // 'a'
        wasmBytes[2] = 0x73; // 's'
        wasmBytes[3] = 0x6D; // 'm'
        wasmBytes[4] = 0x01; // Version 1
        wasmBytes[5] = 0x00; // Version 1
        wasmBytes[6] = 0x00; // Version 1
        wasmBytes[7] = 0x00; // Version 1
        
        // Fill rest with test data to meet size requirements
        for (int i = 8; i < wasmBytes.Length; i++)
        {
            wasmBytes[i] = (byte)(i % 256);
        }
        
        return wasmBytes;
    }
}
