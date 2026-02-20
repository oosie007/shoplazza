namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Service for building and managing cart-transform WASM functions
/// </summary>
public interface ICartTransformFunctionService
{
    /// <summary>
    /// Builds the cart-transform WASM file from source
    /// </summary>
    /// <returns>The compiled WASM file as byte array</returns>
    Task<byte[]> BuildWasmAsync();

    /// <summary>
    /// Builds the cart-transform WASM file and returns it as base64 string
    /// </summary>
    /// <returns>The compiled WASM file as base64 encoded string</returns>
    Task<string> BuildWasmBase64Async();

    /// <summary>
    /// Validates that a WASM file is properly formatted
    /// </summary>
    /// <param name="wasmBytes">The WASM file bytes to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateWasmAsync(byte[] wasmBytes);

    /// <summary>
    /// Gets the path to the pre-built WASM file as fallback
    /// </summary>
    /// <returns>The path to the fallback WASM file, or null if not available</returns>
    Task<string?> GetFallbackWasmPathAsync();

    /// <summary>
    /// Checks if the WASM build environment is properly configured
    /// </summary>
    /// <returns>True if ready to build, false otherwise</returns>
    Task<bool> IsBuildEnvironmentReadyAsync();
}
