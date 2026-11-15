using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Core.Models.Maps;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// マップピン ViewModel
/// </summary>
public class MapPinViewModel : ViewModelBase {
	private readonly ObservableList<IMediaItemViewModel> _items = [];

	/// <summary>
	/// モデル (内部用)
	/// </summary>
	private MapPin Model {
		get;
	}

	/// <summary>
	/// クラスタに含まれるアイテムリスト
	/// </summary>
	public IReadOnlyList<IMediaItemViewModel> Items {
		get {
			return this._items;
		}
	}

	/// <summary>
	/// 代表アイテム
	/// </summary>
	public IMediaItemViewModel Core {
		get;
	}

	/// <summary>
	/// 位置情報
	/// </summary>
	public ILocation? Location {
		get;
	}

	/// <summary>
	/// 表示領域 (クラスタリング計算用)
	/// </summary>
	public Rectangle CoreRectangle {
		get {
			return this.Model.CoreRectangle;
		}
	}

	/// <summary>
	/// 件数
	/// </summary>
	public BindableReactiveProperty<int> Count {
		get;
	}

	/// <summary>
	/// ピン状態
	/// </summary>
	public BindableReactiveProperty<PinState> PinState {
		get;
	}

	/// <summary>
	/// 選択中か
	/// </summary>
	public ReadOnlyReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// 一部選択中か
	/// </summary>
	public ReadOnlyReactiveProperty<bool> IsIndeterminate {
		get;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="core">代表アイテム</param>
	/// <param name="rect">表示領域</param>
	public MapPinViewModel(IMediaItemViewModel core, Rectangle rect) {
		this.Core = core;
		this.Model = new MapPin(core.FileModel, rect).AddTo(this.CompositeDisposable);
		this.Location = this.Model.Location;

		this.AddItem(core);

		// モデルのプロパティと同期
		this.Count = this.Model.Count.AsObservable().ToBindableReactiveProperty(this.Model.Count.Value).AddTo(this.CompositeDisposable);
		this.PinState = this.Model.PinState.AsObservable().ToBindableReactiveProperty(this.Model.PinState.Value).AddTo(this.CompositeDisposable);

		this.IsSelected = this.PinState.Select(x => x == MediaDeck.Core.Models.Maps.PinState.Selected).ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsIndeterminate = this.PinState.Select(x => x == MediaDeck.Core.Models.Maps.PinState.Indeterminate).ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// アイテムを追加
	/// </summary>
	/// <param name="item">追加するアイテム</param>
	public void AddItem(IMediaItemViewModel item) {
		if (this._items.Contains(item)) {
			return;
		}
		this._items.Add(item);
		if (item.FileModel != this.Model.Core.Value && !this.Model.Items.Contains(item.FileModel)) {
			this.Model.Items.Add(item.FileModel);
		}
	}
}