namespace Hosihikari.Binding.Endstone;

/// <summary>An item stack the caller owns and must dispose.</summary>
/// <remarks>
/// This is the owned half of the item-stack pair: <c>Create</c> and the copy constructors hand
/// this type out, while a stack reached through another object's getter arrives as a borrowed
/// <see cref="ItemStack"/>. The native stack is destroyed on dispose.
/// </remarks>
public sealed class OwnedItemStack :
    IDisposable,
    IInteropBridge<InteropItemStack>
{
    private InteropItemStack? _interop;

    private OwnedItemStack(InteropItemStack interop) => _interop = interop;

    /// <summary>Creates a stack of <paramref name="type"/> with an amount and data value.</summary>
    public OwnedItemStack(Identifier_endstone__ItemType_ type, int amount, int data)
        : this(InteropItemStack.Create(type, amount, data))
    {
    }

    /// <summary>Copies an existing Interop wrapper into an owned stack.</summary>
    public static OwnedItemStack FromInterop(InteropItemStack other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OwnedItemStack(InteropItemStack.Create_bea8b69c(other));
    }

    /// <summary>Copies a borrowed stack into an owned one.</summary>
    /// <param name="other">The borrowed stack to copy.</param>
    public static OwnedItemStack CopyOf(ItemStack other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return FromInterop(other.Interop);
    }

    /// <inheritdoc/>
    public InteropItemStack Interop => _interop ?? throw new ObjectDisposedException(nameof(OwnedItemStack));

    /// <summary>The native handle, or zero once disposed.</summary>
    public nint Handle => _interop?.Handle ?? 0;

    /// <summary>The stack amount.</summary>
    public int Amount
    {
        get => Interop.getAmount();
        set => Interop.setAmount(value);
    }

    /// <summary>The stack data value.</summary>
    public int Data
    {
        get => Interop.getData();
        set => Interop.setData(value);
    }

    /// <summary>The stack's item type.</summary>
    public ItemType Type => Interop.getType();

    /// <summary>Replaces the stack's item type.</summary>
    public void SetType(Identifier_endstone__ItemType_ type) => Interop.setType(type);

    /// <summary>The maximum stack size for this item.</summary>
    public int MaxStackSize => Interop.getMaxStackSize();

    /// <summary>The stack's translation key.</summary>
    public string TranslationKey => Interop.getTranslationKey();

    /// <summary>The stack's item meta, when it has any.</summary>
    public ItemMeta? ItemMeta
    {
        get => Interop.getItemMeta();
        set => Interop.setItemMeta(value);
    }

    /// <summary>Whether the stack carries item meta.</summary>
    public bool HasItemMeta => Interop.hasItemMeta();

    /// <summary>The stack's NBT data.</summary>
    public CompoundTag Nbt
    {
        get => Interop.getNbt();
        set => Interop.setNbt(value);
    }

    /// <summary>Whether another stack is similar to this one.</summary>
    public bool IsSimilar(ItemStack other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Interop.isSimilar(other.Interop);
    }

    /// <summary>Whether two stacks are equal.</summary>
    public static bool operator ==(OwnedItemStack? left, OwnedItemStack? right) =>
        left is null ? right is null : right is not null && left.Interop.operator_equal(right.Interop);

    /// <summary>Whether two stacks differ.</summary>
    public static bool operator !=(OwnedItemStack? left, OwnedItemStack? right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OwnedItemStack other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Handle);

    /// <summary>Destroys the native stack.</summary>
    public void Dispose()
    {
        var interop = _interop;
        if (interop is null)
        {
            return;
        }

        _interop = null;
        interop.DisposeIfDisposable();
    }
}
