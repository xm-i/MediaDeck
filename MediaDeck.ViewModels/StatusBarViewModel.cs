using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.ViewModels;

/// <summary>
/// ステータスバーの情報を管理するViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class StatusBarViewModel : ViewModelBase {
	/// <summary>
	/// メディアライブラリのViewModel
	/// </summary>
	public MediaContentLibraryViewModel MediaContentLibrary {
		get;
	}

	/// <summary>
	/// アイテムの表示サイズ
	/// </summary>
	public BindableReactiveProperty<int> ItemSize {
		get;
	}

	/// <summary>
	/// 表示モード切り替え用のViewModel
	/// </summary>
	public ViewerSelectorViewModel ViewerSelector {
		get;
	}

	/// <summary>
	/// サムネイル上オーバーレイ表示
	/// </summary>
	public BindableReactiveProperty<bool> ShowOverlay {
		get;
	}

	/// <summary>
	/// 情報エリア表示
	/// </summary>
	public BindableReactiveProperty<bool> ShowInfo {
		get;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="mediaContentLibrary">メディアライブラリのViewModel</param>
	/// <param name="viewerSelector">表示モード切り替え用のViewModel</param>
	/// <param name="tabState">タブの状態モデル</param>
	public StatusBarViewModel(MediaContentLibraryViewModel mediaContentLibrary, ViewerSelectorViewModel viewerSelector, TabStateModel tabState) {
		this.MediaContentLibrary = mediaContentLibrary;
		this.ViewerSelector = viewerSelector;

		// StateのItemSizeと双方向に同期するプロパティを作成
		this.ItemSize = tabState.ViewerState.ItemSize
			.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ItemSize.Value, this.CompositeDisposable)
			.AddTo(this.CompositeDisposable);

		this.ShowOverlay = tabState.ViewerState.ShowOverlay
			.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ShowOverlay.Value, this.CompositeDisposable)
			.AddTo(this.CompositeDisposable);

		this.ShowInfo = tabState.ViewerState.ShowInfo
			.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ShowInfo.Value, this.CompositeDisposable)
			.AddTo(this.CompositeDisposable);
	}
}