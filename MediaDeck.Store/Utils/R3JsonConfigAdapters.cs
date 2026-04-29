using R3;
using R3.JsonConfig;

namespace MediaDeck.Store.Utils;

/// <summary>
/// ReactiveProperty&lt;T&gt; 用の IJsonConfigWrapper アダプター。
/// </summary>
/// <typeparam name="T">内部値の型。</typeparam>
public sealed class ReactivePropertyAdapter<T> : IJsonConfigWrapper<ReactiveProperty<T>, T>
{
	/// <inheritdoc />
	public T Get(ReactiveProperty<T> wrapper) => wrapper.Value;

	/// <inheritdoc />
	public void Set(ReactiveProperty<T> wrapper, T value) => wrapper.Value = value;
}
