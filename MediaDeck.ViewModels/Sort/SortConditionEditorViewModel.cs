using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;

namespace MediaDeck.ViewModels.Sort;

/// <summary>
/// 単一のソート条件セットを編集するための ViewModel クラス。
/// </summary>
public class SortConditionEditorViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;
	private readonly ISynchronizedView<SortItemObject, SortItemEditorViewModel> _sortItemsView;

	/// <summary>
	/// <see cref="SortConditionEditorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">ソート設定オブジェクト。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <param name="candidateSortKeys">共有するソートキー選択候補一覧（省略時は新規生成）。</param>
	/// <param name="directionOptions">共有するソート方向選択候補一覧（省略時は新規生成）。</param>
	public SortConditionEditorViewModel(
		SortObject model,
		IStringProvider stringProvider,
		IReadOnlyList<SortKeyOption>? candidateSortKeys = null,
		IReadOnlyList<SortDirectionOption>? directionOptions = null) {
		this.Model = model;
		this._stringProvider = stringProvider;

		this.DisplayName = this.Model.DisplayName
			.ToTwoWayBindableReactiveProperty(this.Model.DisplayName.Value ?? string.Empty, this.CompositeDisposable);

		this.CandidateSortKeys = candidateSortKeys ?? CreateCandidateSortKeys(stringProvider);
		this.SelectedSortKey = new BindableReactiveProperty<SortKeyOption?>(this.CandidateSortKeys.FirstOrDefault())
			.AddTo(this.CompositeDisposable);

		this.DirectionOptions = directionOptions ?? CreateDirectionOptions(stringProvider);
		this.SelectedDirectionOption = new BindableReactiveProperty<SortDirectionOption?>(this.DirectionOptions[0])
			.AddTo(this.CompositeDisposable);
		this.Direction = new BindableReactiveProperty<ListSortDirection>(ListSortDirection.Ascending)
			.AddTo(this.CompositeDisposable);

		this.SelectedDirectionOption.Subscribe(option => {
			if (option is not null && this.Direction.Value != option.Direction) {
				this.Direction.Value = option.Direction;
			}
		}).AddTo(this.CompositeDisposable);

		this.Direction.Subscribe(dir => {
			var option = this.DirectionOptions.FirstOrDefault(x => x.Direction == dir);
			if (option is not null && !ReferenceEquals(this.SelectedDirectionOption.Value, option)) {
				this.SelectedDirectionOption.Value = option;
			}
		}).AddTo(this.CompositeDisposable);

		this._sortItemsView = this.Model.SortItemObjects
			.CreateViewWithDisposal(x => new SortItemEditorViewModel(
				x,
				this._stringProvider,
				this.ToggleSortItemDirection,
				this.MoveSortItemUp,
				this.MoveSortItemDown,
				this.RemoveSortItem))
			.AddTo(this.CompositeDisposable);

		this.SortItems = this._sortItemsView
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);

		this.SortItemCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);
		this.HasSortItems = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.IsEmpty = new BindableReactiveProperty<bool>(true).AddTo(this.CompositeDisposable);
		this.SummaryText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);

		this.AddSortItemCommand = this.SelectedSortKey
			.Select(x => x is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.AddSortItemCommand.Subscribe(_ => {
			if (this.SelectedSortKey.Value is not { } selectedKey) {
				return;
			}
			this.Model.AddSortItemObject(selectedKey.Key, this.Direction.Value);
		}).AddTo(this.CompositeDisposable);

		this.AddSortItemByKeyCommand = new ReactiveCommand<SortItemKey?>().AddTo(this.CompositeDisposable);
		this.AddSortItemByKeyCommand.Subscribe(x => {
			if (x is not { } sortItemKey) {
				return;
			}
			this.Model.AddSortItemObject(sortItemKey, this.Direction.Value);
		}).AddTo(this.CompositeDisposable);

		this.RemoveSortItemCommand = new ReactiveCommand<SortItemObject>().AddTo(this.CompositeDisposable);
		this.RemoveSortItemCommand.Subscribe(this.RemoveSortItem).AddTo(this.CompositeDisposable);

		this.Model.SortItemObjects.ObserveChanged()
			.Subscribe(_ => {
				this.UpdateSortItemStates();
			})
			.AddTo(this.CompositeDisposable);

		this.UpdateSortItemStates();
	}

	/// <summary>
	/// モデルを取得します。
	/// </summary>
	public SortObject Model {
		get;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// UI表示用の適用済みソート項目一覧を取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<SortItemEditorViewModel> SortItems {
		get;
	}

	/// <summary>
	/// ソートキー選択候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<SortKeyOption> CandidateSortKeys {
		get;
	}

	/// <summary>
	/// 選択中のソートキー候補を取得します。
	/// </summary>
	public BindableReactiveProperty<SortKeyOption?> SelectedSortKey {
		get;
	}

	/// <summary>
	/// ソート方向選択候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<SortDirectionOption> DirectionOptions {
		get;
	}

	/// <summary>
	/// 選択中のソート方向オプションを取得します。
	/// </summary>
	public BindableReactiveProperty<SortDirectionOption?> SelectedDirectionOption {
		get;
	}

	/// <summary>
	/// 設定用ソート方向を取得します。
	/// </summary>
	public BindableReactiveProperty<ListSortDirection> Direction {
		get;
	}

	/// <summary>
	/// 登録されているソート項目の件数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> SortItemCount {
		get;
	}

	/// <summary>
	/// ソート項目が1件以上登録されているかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> HasSortItems {
		get;
	}

	/// <summary>
	/// ソート項目が0件であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// 条件の概要テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> SummaryText {
		get;
	}

	/// <summary>
	/// 選択中のソートキーと方向でソート項目を追加するコマンドを取得します。
	/// </summary>
	public ReactiveCommand AddSortItemCommand {
		get;
	}

	/// <summary>
	/// 指定したソートキーでソート項目を追加するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<SortItemKey?> AddSortItemByKeyCommand {
		get;
	}

	/// <summary>
	/// 指定したソート項目を削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<SortItemObject> RemoveSortItemCommand {
		get;
	}

	/// <summary>
	/// 指定したソート項目の昇順/降順を反転します。
	/// </summary>
	/// <param name="item">対象のソート項目。</param>
	public void ToggleSortItemDirection(SortItemObject item) {
		var index = this.FindSortItemIndex(item);
		if (index < 0) {
			return;
		}
		var reversed = item.Direction == ListSortDirection.Ascending
			? ListSortDirection.Descending
			: ListSortDirection.Ascending;
		this.Model.ReplaceSortItemObject(index, item.SortItemKey, reversed);
	}

	/// <summary>
	/// 指定したソート項目を1つ上（優先順位を高く）へ移動します。
	/// </summary>
	/// <param name="item">対象のソート項目。</param>
	public void MoveSortItemUp(SortItemObject item) {
		var index = this.FindSortItemIndex(item);
		if (index > 0) {
			this.Model.MoveSortItemObject(index, index - 1);
		}
	}

	/// <summary>
	/// 指定したソート項目を1つ下（優先順位を低く）へ移動します。
	/// </summary>
	/// <param name="item">対象のソート項目。</param>
	public void MoveSortItemDown(SortItemObject item) {
		var index = this.FindSortItemIndex(item);
		if (index >= 0 && index < this.Model.SortItemObjects.Count - 1) {
			this.Model.MoveSortItemObject(index, index + 1);
		}
	}

	/// <summary>
	/// 指定したソート項目を削除します。
	/// </summary>
	/// <param name="item">対象のソート項目。</param>
	public void RemoveSortItem(SortItemObject item) {
		var index = this.FindSortItemIndex(item);
		if (index >= 0) {
			this.Model.SortItemObjects.RemoveAt(index);
		} else {
			this.Model.RemoveSortItemObject(item);
		}
	}

	/// <summary>
	/// ソートキー選択候補一覧を生成します。
	/// </summary>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>ソートキー選択候補一覧。</returns>
	public static IReadOnlyList<SortKeyOption> CreateCandidateSortKeys(IStringProvider stringProvider) {
		return Enum.GetValues<SortItemKey>()
			.Select(k => new SortKeyOption(
				k,
				SortDisplayHelper.GetKeyDisplayName(k, stringProvider),
				SortDisplayHelper.GetKeyIconGlyph(k)))
			.ToArray();
	}

	/// <summary>
	/// ソート方向選択候補一覧を生成します。
	/// </summary>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <returns>ソート方向選択候補一覧。</returns>
	public static IReadOnlyList<SortDirectionOption> CreateDirectionOptions(IStringProvider stringProvider) {
		return [
			new SortDirectionOption(
				ListSortDirection.Ascending,
				SortDisplayHelper.GetDirectionDisplayName(ListSortDirection.Ascending, stringProvider),
				SortDisplayHelper.GetDirectionIconGlyph(ListSortDirection.Ascending)),
			new SortDirectionOption(
				ListSortDirection.Descending,
				SortDisplayHelper.GetDirectionDisplayName(ListSortDirection.Descending, stringProvider),
				SortDisplayHelper.GetDirectionIconGlyph(ListSortDirection.Descending))
		];
	}

	private int FindSortItemIndex(SortItemObject item) {
		for (var i = 0; i < this.Model.SortItemObjects.Count; i++) {
			if (ReferenceEquals(this.Model.SortItemObjects[i], item)) {
				return i;
			}
		}
		return this.Model.SortItemObjects.IndexOf(item);
	}

	private void UpdateSortItemStates() {
		var totalCount = this.Model.SortItemObjects.Count;
		var fallbackIndex = 0;
		foreach (var vm in this._sortItemsView) {
			var actualIndex = this.FindSortItemIndex(vm.Model);
			vm.UpdatePosition(actualIndex >= 0 ? actualIndex : fallbackIndex, totalCount);
			fallbackIndex++;
		}

		this.SortItemCount.Value = totalCount;
		this.HasSortItems.Value = totalCount > 0;
		this.IsEmpty.Value = totalCount == 0;
		this.SummaryText.Value = SortDisplayHelper.FormatSummary(this.Model.SortItemObjects, this._stringProvider);
	}
}