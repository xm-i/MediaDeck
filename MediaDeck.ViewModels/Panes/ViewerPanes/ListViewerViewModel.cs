using System.Collections.ObjectModel;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class ListViewerViewModel : ViewerPaneViewModelBase {
	/// <summary>
	/// 列ID定数。 View 側の DataTemplate キーと対応させる。
	/// </summary>
	public static class ColumnIds {
		public const string Thumbnail = "Thumbnail";
		public const string FileName = "FileName";
		public const string Resolution = "Resolution";
		public const string FileSize = "FileSize";
		public const string CreationTime = "CreationTime";
		public const string Rate = "Rate";
	}

	/// <summary>
	/// <see cref="ListViewerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="filesManager">ファイル管理モデル</param>
	/// <param name="mediaContentLibraryViewModel">メディアコンテンツライブラリViewModel</param>
	/// <param name="searchConditionManagerViewModel">検索条件管理ViewModel</param>
	/// <param name="tabState">タブ状態モデル</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public ListViewerViewModel(
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState,
		IStringProvider stringProvider)
		: base(ViewerType.List, "List", "\uE8FD", filesManager, mediaContentLibraryViewModel, searchConditionManagerViewModel, tabState) {
		var viewerState = tabState.ViewerState;

		this.ThumbnailColumn = new ListViewerColumn(ColumnIds.Thumbnail, "", 60, minWidth: 20, canHide: false);
		this.FileNameColumn = new ListViewerColumn(ColumnIds.FileName, stringProvider.GetString("Viewer_ColumnFileName.Text"), viewerState.ListFileNameColumnWidth.Value, minWidth: 120, isVisible: viewerState.ListFileNameColumnVisible.Value);
		this.ResolutionColumn = new ListViewerColumn(ColumnIds.Resolution, stringProvider.GetString("Viewer_ColumnResolution.Text"), viewerState.ListResolutionColumnWidth.Value, minWidth: 60, isVisible: viewerState.ListResolutionColumnVisible.Value);
		this.FileSizeColumn = new ListViewerColumn(ColumnIds.FileSize, stringProvider.GetString("Viewer_ColumnSize.Text"), viewerState.ListFileSizeColumnWidth.Value, minWidth: 60, isVisible: viewerState.ListFileSizeColumnVisible.Value);
		this.CreationTimeColumn = new ListViewerColumn(ColumnIds.CreationTime, stringProvider.GetString("Viewer_ColumnCreationTime.Text"), viewerState.ListCreationTimeColumnWidth.Value, minWidth: 80, isVisible: viewerState.ListCreationTimeColumnVisible.Value);
		this.RateColumn = new ListViewerColumn(ColumnIds.Rate, stringProvider.GetString("Viewer_ColumnRate.Text"), viewerState.ListRateColumnWidth.Value, minWidth: 40, isVisible: viewerState.ListRateColumnVisible.Value);

		this.AllColumns = [
			this.ThumbnailColumn,
			this.FileNameColumn,
			this.ResolutionColumn,
			this.FileSizeColumn,
			this.CreationTimeColumn,
			this.RateColumn,
		];

		this.FileNameColumn.Width
			.Subscribe(x => viewerState.ListFileNameColumnWidth.Value = x)
			.AddTo(this.CompositeDisposable);
		this.FileNameColumn.IsVisible
			.Subscribe(x => viewerState.ListFileNameColumnVisible.Value = x)
			.AddTo(this.CompositeDisposable);

		this.ResolutionColumn.Width
			.Subscribe(x => viewerState.ListResolutionColumnWidth.Value = x)
			.AddTo(this.CompositeDisposable);
		this.ResolutionColumn.IsVisible
			.Subscribe(x => viewerState.ListResolutionColumnVisible.Value = x)
			.AddTo(this.CompositeDisposable);

		this.FileSizeColumn.Width
			.Subscribe(x => viewerState.ListFileSizeColumnWidth.Value = x)
			.AddTo(this.CompositeDisposable);
		this.FileSizeColumn.IsVisible
			.Subscribe(x => viewerState.ListFileSizeColumnVisible.Value = x)
			.AddTo(this.CompositeDisposable);

		this.CreationTimeColumn.Width
			.Subscribe(x => viewerState.ListCreationTimeColumnWidth.Value = x)
			.AddTo(this.CompositeDisposable);
		this.CreationTimeColumn.IsVisible
			.Subscribe(x => viewerState.ListCreationTimeColumnVisible.Value = x)
			.AddTo(this.CompositeDisposable);

		this.RateColumn.Width
			.Subscribe(x => viewerState.ListRateColumnWidth.Value = x)
			.AddTo(this.CompositeDisposable);
		this.RateColumn.IsVisible
			.Subscribe(x => viewerState.ListRateColumnVisible.Value = x)
			.AddTo(this.CompositeDisposable);
	}

	public ListViewerColumn ThumbnailColumn {
		get;
	}

	public ListViewerColumn FileNameColumn {
		get;
	}

	public ListViewerColumn ResolutionColumn {
		get;
	}

	public ListViewerColumn FileSizeColumn {
		get;
	}

	public ListViewerColumn CreationTimeColumn {
		get;
	}

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