using System.ComponentModel;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;

namespace MediaDeck.ViewModels.Sort;

/// <summary>
/// 適用済みソート項目の1行を表す ViewModel クラス。
/// </summary>
public class SortItemEditorViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="SortItemEditorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">ソート項目モデル。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <param name="toggleDirection">方向切り替えコールバック。</param>
	/// <param name="moveUp">上へ移動コールバック。</param>
	/// <param name="moveDown">下へ移動コールバック。</param>
	/// <param name="remove">削除コールバック。</param>
	public SortItemEditorViewModel(
		SortItemObject model,
		IStringProvider stringProvider,
		Action<SortItemObject> toggleDirection,
		Action<SortItemObject> moveUp,
		Action<SortItemObject> moveDown,
		Action<SortItemObject> remove) {
		this.Model = model;
		this.SortItemKey = model.SortItemKey;
		this.Direction = model.Direction;
		this.KeyDisplayName = SortDisplayHelper.GetKeyDisplayName(model.SortItemKey, stringProvider);
		this.KeyIconGlyph = SortDisplayHelper.GetKeyIconGlyph(model.SortItemKey);
		this.DirectionDisplayName = SortDisplayHelper.GetDirectionDisplayName(model.Direction, stringProvider);
		this.DirectionIconGlyph = SortDisplayHelper.GetDirectionIconGlyph(model.Direction);
		this.IsAscending = model.Direction == ListSortDirection.Ascending;

		this.Priority = new BindableReactiveProperty<int>(1).AddTo(this.CompositeDisposable);
		this.CanMoveUp = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.CanMoveDown = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);

		this.ToggleDirectionCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ToggleDirectionCommand.Subscribe(_ => {
			toggleDirection(this.Model);
		}).AddTo(this.CompositeDisposable);

		this.MoveUpCommand = this.CanMoveUp.ToReactiveCommand().AddTo(this.CompositeDisposable);
		this.MoveUpCommand.Subscribe(_ => {
			moveUp(this.Model);
		}).AddTo(this.CompositeDisposable);

		this.MoveDownCommand = this.CanMoveDown.ToReactiveCommand().AddTo(this.CompositeDisposable);
		this.MoveDownCommand.Subscribe(_ => {
			moveDown(this.Model);
		}).AddTo(this.CompositeDisposable);

		this.RemoveCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RemoveCommand.Subscribe(_ => {
			remove(this.Model);
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 対象のソート項目モデルを取得します。
	/// </summary>
	public SortItemObject Model {
		get;
	}

	/// <summary>
	/// ソートキーを取得します。
	/// </summary>
	public SortItemKey SortItemKey {
		get;
	}

	/// <summary>
	/// ソート方向を取得します。
	/// </summary>
	public ListSortDirection Direction {
		get;
	}

	/// <summary>
	/// ソートキーの表示名を取得します。
	/// </summary>
	public string KeyDisplayName {
		get;
	}

	/// <summary>
	/// ソートキーのアイコングリフを取得します。
	/// </summary>
	public string KeyIconGlyph {
		get;
	}

	/// <summary>
	/// ソート方向の表示名を取得します。
	/// </summary>
	public string DirectionDisplayName {
		get;
	}

	/// <summary>
	/// ソート方向のアイコングリフを取得します。
	/// </summary>
	public string DirectionIconGlyph {
		get;
	}

	/// <summary>
	/// 昇順であるかどうかを取得します。
	/// </summary>
	public bool IsAscending {
		get;
	}

	/// <summary>
	/// 1始まりの優先順位番号を取得します。
	/// </summary>
	public BindableReactiveProperty<int> Priority {
		get;
	}

	/// <summary>
	/// 上へ移動可能かどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> CanMoveUp {
		get;
	}

	/// <summary>
	/// 下へ移動可能かどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> CanMoveDown {
		get;
	}

	/// <summary>
	/// 昇順/降順を切り替えるコマンドを取得します。
	/// </summary>
	public ReactiveCommand ToggleDirectionCommand {
		get;
	}

	/// <summary>
	/// 優先順位を1つ上へ移動するコマンドを取得します。
	/// </summary>
	public ReactiveCommand MoveUpCommand {
		get;
	}

	/// <summary>
	/// 優先順位を1つ下へ移動するコマンドを取得します。
	/// </summary>
	public ReactiveCommand MoveDownCommand {
		get;
	}

	/// <summary>
	/// このソート項目を削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand RemoveCommand {
		get;
	}

	/// <summary>
	/// リスト内のインデックスと総数に基づいて優先順位番号および移動可能状態を更新します。
	/// </summary>
	/// <param name="index">0始まりの現在のインデックス。</param>
	/// <param name="totalCount">総項目数。</param>
	public void UpdatePosition(int index, int totalCount) {
		this.Priority.Value = index + 1;
		this.CanMoveUp.Value = index > 0;
		this.CanMoveDown.Value = index >= 0 && index < totalCount - 1;
	}
}