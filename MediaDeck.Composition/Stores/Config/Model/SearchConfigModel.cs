using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class SearchConfigModel {
	/// <summary>
	/// 初期ロード件数
	/// </summary>
	public ReactiveProperty<int> InitialLoadCount { get; } = new(500);

	/// <summary>
	/// 増分読み込み件数
	/// </summary>
	public ReactiveProperty<int> IncrementalLoadCount { get; } = new(3000);

	/// <summary>
	/// 最大ロード件数
	/// </summary>
	public ReactiveProperty<int> MaxLoadCount { get; } = new(50000);
}