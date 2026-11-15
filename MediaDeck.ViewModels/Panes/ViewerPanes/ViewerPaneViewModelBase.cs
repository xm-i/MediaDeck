using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// 各ビューアペインの基底 ViewModel。
/// 共通のメディアライブラリ参照、検索条件マネージャ、表示設定（アイテムサイズ、オーバーレイ、情報表示）を提供します。
/// </summary>
public abstract class ViewerPaneViewModelBase : ViewModelBase {
	/// <summary>アイテム表示サイズの最小値。</summary>
	public const int MinItemSize = 50;

	/// <summary>アイテム表示サイズの最大値。</summary>
	public const int MaxItemSize = 500;

	private readonly FilesManager _filesManager;
	private readonly ExecutionConfigModel _executionConfig;

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
	/// <param name="executionConfig">外部実行設定モデル。</param>
	protected ViewerPaneViewModelBase(
		ViewerType viewerType,
		string name,
		string iconGlyph,
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState,
		ExecutionConfigModel executionConfig) {
		this._filesManager = filesManager;
		this._executionConfig = executionConfig;
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
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
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
	}

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

	/// <summary>
	/// 指定されたメディアアイテムを削除し、現在の検索結果を再読み込みします。
	/// </summary>
	/// <param name="fileViewModels">削除対象のメディアアイテム。</param>
	/// <returns>非同期タスク。</returns>
	public async Task DeleteFilesAndReloadAsync(IEnumerable<IMediaItemViewModel> fileViewModels) {
		await this.RemoveFilesAsync(fileViewModels);
		this.SearchConditionManagerViewModel.Reload();
	}

	/// <summary>
	/// マウスホイール入力等に応じてアイテム表示サイズを増減させます。
	/// </summary>
	/// <param name="delta">ホイールの変化量。</param>
	public void AdjustItemSize(int delta) {
		const int step = 20;
		this.ItemSize.Value = delta switch {
			> 0 => Math.Min(MaxItemSize, this.ItemSize.Value + step),
			< 0 => Math.Max(MinItemSize, this.ItemSize.Value - step),
			_ => this.ItemSize.Value,
		};
	}

	/// <summary>
	/// 指定されたメディア種別に対応する外部実行プログラム一覧を取得します。
	/// </summary>
	/// <param name="mediaType">メディア種別。</param>
	/// <returns>実行プログラム設定一覧。</returns>
	public IReadOnlyList<IExecutionProgramObjectModel> GetExternalPrograms(MediaType mediaType) {
		return this._executionConfig.GetPrograms(mediaType);
	}

	/// <summary>
	/// 指定した実行プログラムで対象ファイルを順次実行します。
	/// </summary>
	/// <param name="program">実行プログラム設定。</param>
	/// <param name="files">対象ファイル一覧。</param>
	/// <returns>非同期タスク。</returns>
	public async Task ExecuteFilesAsync(IExecutionProgramObjectModel program, IEnumerable<IMediaItemViewModel> files) {
		foreach (var file in files) {
			await file.ExecuteFileAsync(program);
		}
	}

	/// <summary>
	/// 指定したファイルを指定パスのアルバムに追加します。
	/// </summary>
	/// <param name="albumPath">追加先アルバムのフルパス。</param>
	/// <param name="targetFiles">対象ファイル一覧。</param>
	/// <returns>非同期タスク。</returns>
	public async Task AddToAlbumAsync(string albumPath, IEnumerable<IMediaItemViewModel> targetFiles) {
		await this.MediaContentLibraryViewModel.AddToAlbumAsync(albumPath, targetFiles);
	}

	/// <summary>
	/// 最近使用したアルバムのパス一覧を取得します。
	/// </summary>
	/// <param name="count">取得件数。</param>
	/// <returns>アルバムパス一覧。</returns>
	public async Task<IReadOnlyList<string>> GetRecentAlbumPathsAsync(int count = 12) {
		return await this.MediaContentLibraryViewModel.GetRecentAlbumPathsAsync(count);
	}

	/// <summary>
	/// 指定されたメディアアイテムのメタデータを更新します。
	/// </summary>
	/// <param name="targetFiles">対象メディアアイテム一覧。</param>
	public void UpdateMetadata(IEnumerable<IMediaItemViewModel> targetFiles) {
		this.MediaContentLibraryViewModel.UpdateMetadata(targetFiles);
	}

	/// <summary>
	/// 指定されたメディアアイテムの存在するフォルダをエクスプローラーで開きます。
	/// </summary>
	/// <param name="fileViewModel">対象メディアアイテム。</param>
	public void ShowInExplorer(IMediaItemViewModel? fileViewModel) {
		if (fileViewModel is null || string.IsNullOrEmpty(fileViewModel.FilePath)) {
			return;
		}
		if (File.Exists(fileViewModel.FilePath) || Directory.Exists(fileViewModel.FilePath)) {
			ShellUtility.ShowInExplorer(fileViewModel.FilePath);
		}
	}
}