using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// 各ビューアペインの基底 ViewModel。
/// 共通のメディアライブラリ参照、検索条件マネージャ、表示設定（アイテムサイズ、オーバーレイ、情報表示）を提供します。
/// </summary>
public abstract class ViewerPaneViewModelBase : ViewModelBase {
	private readonly FilesManager _filesManager;

	/// <summary>
	/// <see cref="ViewerPaneViewModelBase"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewerType">ビューア種別。</param>
	/// <param name="name">表示名。</param>
	/// <param name="iconGlyph">アイコンのグリフ文字。</param>
	/// <param name="filesManager">ファイルマネージャ。</param>
	/// <param name="mediaContentLibraryViewModel">メディアライブラリ ViewModel。</param>
	/// <param name="searchConditionManagerViewModel">検索条件マネージャ ViewModel。</param>
	/// <param name="tabState">タブ状態モデル。</param>
	protected ViewerPaneViewModelBase(
		ViewerType viewerType,
		string name,
		string iconGlyph,
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState) {
		this._filesManager = filesManager;
		this.ViewerType = viewerType;
		this.Name = name;
		this.IconGlyph = iconGlyph;
		this.MediaContentLibraryViewModel = mediaContentLibraryViewModel;
		this.SearchConditionManagerViewModel = searchConditionManagerViewModel;

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

	/// <summary>
	/// ビューア種別を取得します。
	/// </summary>
	public ViewerType ViewerType {
		get;
	}

	/// <summary>
	/// ビューア名を取得します。
	/// </summary>
	public string Name {
		get;
	}

	/// <summary>
	/// アイコングリフを取得します。
	/// </summary>
	public string IconGlyph {
		get;
	}

	/// <summary>
	/// このビューアが選択中であるかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsSelected {
		get;
	} = new(false);

	/// <summary>
	/// メディアライブラリ ViewModel を取得します。
	/// </summary>
	public MediaContentLibraryViewModel MediaContentLibraryViewModel {
		get;
	}

	/// <summary>
	/// 検索条件マネージャ ViewModel を取得します。
	/// </summary>
	public SearchConditionManagerViewModel SearchConditionManagerViewModel {
		get;
	}

	/// <summary>
	/// アイテム表示サイズを取得します。
	/// </summary>
	public BindableReactiveProperty<int> ItemSize {
		get;
	}

	/// <summary>
	/// オーバーレイ表示設定を取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowOverlay {
		get;
	}

	/// <summary>
	/// 情報領域表示設定を取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowInfo {
		get;
	}

	/// <summary>
	/// 指定されたメディアアイテムを削除します。
	/// </summary>
	/// <param name="fileViewModels">削除対象のメディアアイテム。</param>
	/// <returns>非同期タスク。</returns>
	public async Task RemoveFilesAsync(IEnumerable<IMediaItemViewModel> fileViewModels) {
		await this._filesManager.RemoveFilesAsync(fileViewModels.Select(x => x.FileModel));
	}
}