using System.Collections.ObjectModel;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// メディアアイテムをリスト形式（詳細表形式）で表示するビューの ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class ListViewerViewModel : ViewerPaneViewModelBase {
	/// <summary>
	/// 列ID定数。 View 側の DataTemplate キーと対応させる。
	/// </summary>
	public static class ColumnIds {
		/// <summary>サムネイル列 ID。</summary>
		public const string Thumbnail = MediaDeck.Composition.Constants.ListViewerColumnIds.Thumbnail;

		/// <summary>ファイル名列 ID。</summary>
		public const string FileName = MediaDeck.Composition.Constants.ListViewerColumnIds.FileName;

		/// <summary>解像度列 ID。</summary>
		public const string Resolution = MediaDeck.Composition.Constants.ListViewerColumnIds.Resolution;

		/// <summary>ファイルサイズ列 ID。</summary>
		public const string FileSize = MediaDeck.Composition.Constants.ListViewerColumnIds.FileSize;

		/// <summary>作成日時列 ID。</summary>
		public const string CreationTime = MediaDeck.Composition.Constants.ListViewerColumnIds.CreationTime;

		/// <summary>評価列 ID。</summary>
		public const string Rate = MediaDeck.Composition.Constants.ListViewerColumnIds.Rate;
	}

	/// <summary>
	/// <see cref="ListViewerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="filesManager">ファイル管理モデル</param>
	/// <param name="mediaContentLibraryViewModel">メディアコンテンツライブラリViewModel</param>
	/// <param name="searchConditionManagerViewModel">検索条件管理ViewModel</param>
	/// <param name="tabState">タブ状態モデル</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	/// <param name="executionConfig">外部実行設定モデル</param>
	public ListViewerViewModel(
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState,
		IStringProvider stringProvider,
		ExecutionConfigModel executionConfig)
		: base(ViewerType.List, "List", "\uE8FD", filesManager, mediaContentLibraryViewModel, searchConditionManagerViewModel, tabState, executionConfig) {
		var viewerState = tabState.ViewerState;

		ListViewerColumn CreateBoundColumn(string columnId, string header, double defaultWidth, double minWidth, bool defaultVisible = true) {
			var colState = viewerState.GetOrCreateColumnState(columnId, defaultWidth, defaultVisible);
			var column = new ListViewerColumn(columnId, header, colState.Width.Value, minWidth: minWidth, isVisible: colState.IsVisible.Value);

			column.Width
				.Skip(1)
				.Subscribe(x => colState.Width.Value = x)
				.AddTo(this.CompositeDisposable);
			column.IsVisible
				.Skip(1)
				.Subscribe(x => colState.IsVisible.Value = x)
				.AddTo(this.CompositeDisposable);

			return column;
		}

		this.ThumbnailColumn = new ListViewerColumn(ColumnIds.Thumbnail, "", 60, minWidth: 20, canHide: false).AddTo(this.CompositeDisposable);
		this.FileNameColumn = CreateBoundColumn(ColumnIds.FileName, stringProvider.GetString("Viewer_ColumnFileName.Text"), 400, minWidth: 120).AddTo(this.CompositeDisposable);
		this.ResolutionColumn = CreateBoundColumn(ColumnIds.Resolution, stringProvider.GetString("Viewer_ColumnResolution.Text"), 100, minWidth: 60).AddTo(this.CompositeDisposable);
		this.FileSizeColumn = CreateBoundColumn(ColumnIds.FileSize, stringProvider.GetString("Viewer_ColumnSize.Text"), 100, minWidth: 60).AddTo(this.CompositeDisposable);
		this.CreationTimeColumn = CreateBoundColumn(ColumnIds.CreationTime, stringProvider.GetString("Viewer_ColumnCreationTime.Text"), 150, minWidth: 80).AddTo(this.CompositeDisposable);
		this.RateColumn = CreateBoundColumn(ColumnIds.Rate, stringProvider.GetString("Viewer_ColumnRate.Text"), 60, minWidth: 40).AddTo(this.CompositeDisposable);

		this.AllColumns = [
			this.ThumbnailColumn,
			this.FileNameColumn,
			this.ResolutionColumn,
			this.FileSizeColumn,
			this.CreationTimeColumn,
			this.RateColumn,
		];
	}

	/// <summary>サムネイル列を取得します。</summary>
	public ListViewerColumn ThumbnailColumn {
		get;
	}

	/// <summary>ファイル名列を取得します。</summary>
	public ListViewerColumn FileNameColumn {
		get;
	}

	/// <summary>解像度列を取得します。</summary>
	public ListViewerColumn ResolutionColumn {
		get;
	}

	/// <summary>ファイルサイズ列を取得します。</summary>
	public ListViewerColumn FileSizeColumn {
		get;
	}

	/// <summary>作成日時列を取得します。</summary>
	public ListViewerColumn CreationTimeColumn {
		get;
	}

	/// <summary>評価列を取得します。</summary>
	public ListViewerColumn RateColumn {
		get;
	}

	/// <summary>
	/// ヘッダー右クリックメニュー用の列一覧。
	/// 並び替えは行わないため固定順序。
	/// </summary>
	public ObservableCollection<ListViewerColumn> AllColumns {
		get;
	}

	/// <summary>
	/// 列の表示・非表示を切り替える。
	/// </summary>
	public void ToggleColumnVisibility(string columnId) {
		var column = this.AllColumns.FirstOrDefault(x => x.Id == columnId);
		if (column is null || !column.CanHide) {
			return;
		}
		column.IsVisible.Value = !column.IsVisible.Value;
	}
}