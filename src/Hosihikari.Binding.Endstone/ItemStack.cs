namespace Hosihikari.Binding.Endstone;

/// <summary>An item stack borrowed from another object.</summary>
/// <remarks>
/// The borrowed half of the item-stack pair. A stack handed out by a getter lives as long as its
/// owner; to keep one, copy it with <see cref="OwnedItemStack.CopyOf"/>. Reading through this type
/// never releases the stack.
/// </remarks>
public sealed class ItemStack : IInteropBridge<InteropItemStack>
{
    private InteropItemStack? _interop;

    private ItemStack(InteropItemStack interop) => _interop = interop;

    /// <summary>Bridges an Interop wrapper.</summary>
    public static ItemStack From(InteropItemStack interop)
    {
        ArgumentNullException.ThrowIfNull(interop);
        return new ItemStack(interop);
    }

    /// <inheritdoc/>
    public InteropItemStack Interop => _interop ?? throw new ObjectDisposedException(nameof(ItemStack));

    /// <summary>The native handle, or zero once the instance was invalidated.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The stack amount.</summary>
    public int Amount => Interop.getAmount();

    /// <summary>The stack data value.</summary>
    public int Data => Interop.getData();

    /// <summary>The stack's item type.</summary>
    public ItemType Type => Interop.getType();

    /// <summary>The maximum stack size for this item.</summary>
    public int MaxStackSize => Interop.getMaxStackSize();

    /// <summary>The stack's translation key.</summary>
    public string TranslationKey => Interop.getTranslationKey();

    /// <summary>The stack's item meta, when it has any.</summary>
    public ItemMeta? ItemMeta => Interop.getItemMeta();

    /// <summary>Whether the stack carries item meta.</summary>
    public bool HasItemMeta => Interop.hasItemMeta();

    /// <summary>The stack's NBT data.</summary>
    public CompoundTag Nbt => Interop.getNbt();

    /// <summary>Whether another stack is similar to this one.</summary>
    public bool IsSimilar(ItemStack other) => Interop.isSimilar(other.Interop);

    /// <summary>Whether two stacks are equal.</summary>
    public static bool operator ==(ItemStack? left, ItemStack? right) =>
        left is null ? right is null : right is not null && left.Interop.operator_equal(right.Interop);

    /// <summary>Whether two stacks differ.</summary>
    public static bool operator !=(ItemStack? left, ItemStack? right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ItemStack other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Handle);

    /// <summary>Invalidates this instance. The native stack is untouched.</summary>
    public void Dispose() => _interop = null;
}
