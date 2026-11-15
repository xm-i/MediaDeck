using GenJsonConfig.Attributes;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.State.Model.Objects;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Composition.Stores.Config.Model;

/// <summary>
/// フィルター・ソート条件の定義リスト（アプリ全体で共有）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateJsonConfigDto]
public class SearchDefinitionsConfigModel {
	private readonly IServiceProvider _serviceProvider;
	private readonly IStringProvider _stringProvider;

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

	public SearchDefinitionsConfigModel(IServiceProvider serviceProvider, IStringProvider stringProvider) {
		this._serviceProvider = serviceProvider;
		this._stringProvider = stringProvider;
		(string, SortItemKey[])[] sc = [
			(stringProvider.GetString("Sort_FilePath"), [SortItemKey.FilePath]),
			(stringProvider.GetString("Sort_ModifiedTime"), [SortItemKey.ModifiedTime]),
			(stringProvider.GetString("Sort_Rate"), [SortItemKey.Rate]),
			(stringProvider.GetString("Sort_UsageCount"), [SortItemKey.UsageCount]),
			(stringProvider.GetString("Sort_FileSize"), [SortItemKey.FileSize]),
			(stringProvider.GetString("Sort_Random"), [SortItemKey.Random])
		];
		this.SortConditions = [
			.. sc.Select(x => {
				var model = serviceProvider.GetRequiredService<SortObject>();
				model.DisplayName.Value = x.Item1;
				model.SortItemObjects.AddRange(x.Item2.Select(sik => new SortItemObject() { SortItemKey = sik }));
				return model;
			})
		];
	}

	public SortObject AddSortCondition() {
		var so = this._serviceProvider.GetRequiredService<SortObject>();
		this.SortConditions.Add(so);
		return so;
	}

	public void RemoveSortCondition(SortObject sortObject) {
		this.SortConditions.Remove(sortObject);
	}

	public FilterObject AddFilteringCondition() {
		var fo = this._serviceProvider.GetRequiredService<FilterObject>();
		this.FilteringConditions.Add(fo);
		return fo;
	}

	public void RemoveFilteringCondition(FilterObject filterObject) {
		this.FilteringConditions.Remove(filterObject);
	}
}