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

	/// <summary>
	/// 新しいソート条件を追加します。
	/// </summary>
	/// <returns>追加されたソート条件オブジェクト。</returns>
	public SortObject AddSortCondition() {
		var so = this._serviceProvider.GetRequiredService<SortObject>();
		this.SortConditions.Add(so);
		return so;
	}

	/// <summary>
	/// 指定したソート条件を削除します。
	/// </summary>
	/// <param name="sortObject">削除対象のソート条件オブジェクト。</param>
	public void RemoveSortCondition(SortObject sortObject) {
		for (var i = 0; i < this.SortConditions.Count; i++) {
			if (ReferenceEquals(this.SortConditions[i], sortObject)) {
				this.SortConditions.RemoveAt(i);
				return;
			}
		}
		this.SortConditions.Remove(sortObject);
	}

	/// <summary>
	/// 新しいフィルター条件を追加します。
	/// </summary>
	/// <returns>追加されたフィルター条件オブジェクト。</returns>
	public FilterObject AddFilteringCondition() {
		var fo = this._serviceProvider.GetRequiredService<FilterObject>();
		this.FilteringConditions.Add(fo);
		return fo;
	}

	/// <summary>
	/// 指定したフィルター条件を削除します。
	/// </summary>
	/// <param name="filterObject">削除対象のフィルター条件オブジェクト。</param>
	public void RemoveFilteringCondition(FilterObject filterObject) {
		this.FilteringConditions.Remove(filterObject);
	}
}