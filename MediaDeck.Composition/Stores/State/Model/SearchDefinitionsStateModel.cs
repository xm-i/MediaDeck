using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model.Objects;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// フィルター・ソート条件の定義リスト（アプリ全体で共有）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class SearchDefinitionsStateModel {
	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	/// フィルター条件リスト
	/// </summary>
	public ObservableList<FilterObject> FilteringConditions {
		get;
	} = [];

	/// <summary>
	/// ソート条件リスト
	/// </summary>
	public ObservableList<SortObject> SortConditions {
		get;
	}

	public SearchDefinitionsStateModel(IServiceProvider serviceProvider) {
		this._serviceProvider = serviceProvider;
		(string, SortItemKey[])[] sc = [
			("File Path", [SortItemKey.FilePath]),
			("Modified Time", [SortItemKey.ModifiedTime]),
			("Rate", [SortItemKey.Rate]),
			("Usage Count", [SortItemKey.UsageCount]),
			("File Size", [SortItemKey.FileSize])
		];
		this.SortConditions = [
			.. sc.Select(x => {
				var model = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SortObject>();
				model.DisplayName.Value = x.Item1;
				model.SortItemObjects.AddRange(x.Item2.Select(sik => new SortItemObject() { SortItemKey = sik }));
				return model;
			})
		];
	}

	public SortObject AddSortCondition() {
		var so = this._serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SortObject>();
		this.SortConditions.Add(so);
		return so;
	}

	public void RemoveSortCondition(SortObject sortObject) {
		this.SortConditions.Remove(sortObject);
	}

	public FilterObject AddFilteringCondition() {
		var fo = this._serviceProvider.CreateScope().ServiceProvider.GetRequiredService<FilterObject>();
		this.FilteringConditions.Add(fo);
		return fo;
	}

	public void RemoveFilteringCondition(FilterObject filterObject) {
		this.FilteringConditions.Remove(filterObject);
	}
}