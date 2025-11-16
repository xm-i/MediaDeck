using System.ComponentModel;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Stores.State.Model.Objects;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;
/// <summary>
/// 検索状態
/// </summary>

[AddSingleton]
[GenerateR3JsonConfigDefaultDto]
public class SearchStateModel {
	private readonly IServiceProvider _serviceProvider;
	/// <summary>
	/// カレント検索条件
	/// </summary>
	public ObservableList<ISearchCondition> SearchCondition {
		get;
	} = [];

	/// <summary>
	/// カレントフィルター条件
	/// </summary>
	public ReactiveProperty<Guid?> CurrentFilteringCondition {
		get;
	} = new(null);

	/// <summary>
	/// フィルター条件リスト
	/// </summary>
	public ObservableList<FilterObject> FilteringConditions {
		get;
	} = [];

	/// <summary>
	/// カレントソート条件
	/// </summary>
	public ReactiveProperty<Guid?> CurrentSortCondition {
		get;
	} = new(null);

	/// <summary>
	/// ソート条件リスト
	/// </summary>
	public ObservableList<SortObject> SortConditions {
		get;
	}

	/// <summary>
	/// 全体ソート方向
	/// </summary>
	public ReactiveProperty<ListSortDirection> SortDirection {
		get;
	} = new(ListSortDirection.Ascending);

	public SearchStateModel(IServiceProvider serviceProvider) {
		this._serviceProvider = serviceProvider;
		(string, SortItemKey[])[] sc = [
			("File Path", [SortItemKey.FilePath]),
			("Modified Time", [SortItemKey.ModifiedTime]),
			("Rate", [SortItemKey.Rate]),
			("Usage Count", [SortItemKey.UsageCount]),
			("File Size", [SortItemKey.FileSize])
		];
		this.SortConditions = [.. sc.Select(x => {
			var model = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SortObject>();
			model.DisplayName.Value = x.Item1;
			model.SortItemObjects.AddRange(x.Item2.Select(sik =>  new SortItemObject(){ SortItemKey= sik}));
			return model;
		})];
	}

	public SortObject AddSortCondition() {
		var so = this._serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SortObject>();
		this.SortConditions.Add(so);
		return so;
	}

	public void RemoveSortCondition(SortObject sortObject) {
		this.SortConditions.Remove(sortObject);
	}
}