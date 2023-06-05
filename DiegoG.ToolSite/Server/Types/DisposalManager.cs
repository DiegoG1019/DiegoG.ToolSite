namespace DiegoG.ToolSite.Server.Types;

public readonly record struct DisposalManager<T>(
        T Item,
        bool ShouldDispose,
        Func<ValueTask>? AfterDisposal = null
    ) : IDisposable where T : IDisposable
{
    public T Item { get; } = Item ?? throw new ArgumentNullException(nameof(Item));

    public void Dispose()
    {
        if (Item is not null && ShouldDispose)
            Item.Dispose();
        var t = AfterDisposal?.Invoke();
        if (t is ValueTask task)
            task.Preserve().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public DisposalManager<T> GetItem(out T item)
    {
        item = Item;
        return this;
    }
}
