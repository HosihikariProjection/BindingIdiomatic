namespace Hosihikari.Binding.Bedrock;

/// <summary>Handles the null a failed shim factory returns.</summary>
/// <remarks>
/// The generated factory returns <see langword="null"/> when the shim could not resolve the
/// symbol it needs, and records the reason with the shim's last-error slot. A constructor
/// cannot return null, so an owned type turns that absence into an exception instead; a
/// property can hand the null straight through.
/// </remarks>
internal static class Factories
{
    /// <summary>Returns the created wrapper, or throws with the shim's reason.</summary>
    public static T Require<T>(T? created, string what) where T : class =>
        created ?? throw new InvalidOperationException(
            $"Could not create {what}: {NativeMethods.GetLastError() ?? "the shim returned no object."}");
}
