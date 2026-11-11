namespace Hosihikari.Binding.Bedrock;

/// <summary>Recreates a wrapper from the state of a borrowed one.</summary>
/// <remarks>
/// The symbol-dispatch surface exposes the corners only as raw addresses, so a copy goes through
/// the empty box and the shim's own assignment instead of reading components.
/// </remarks>
internal static class Snapshots
{
    /// <summary>Copies a borrowed box into a fresh owned one.</summary>
    public static InteropAabbB? Rebuild(InteropAabbB source)
    {
        var empty = InteropAabbB.BOX_AT_ORIGIN_WITH_NO_VOLUME();
        if (empty is null)
        {
            return null;
        }

        // Assignment returns the target; the wrapper it hands back is a fresh, owned one.
        return empty.operator_assign(source);
    }
}
