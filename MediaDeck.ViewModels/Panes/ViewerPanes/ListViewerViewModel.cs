using System.Collections.ObjectModel;
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

	public ListViewerViewModel(FilesManager filesManager, TabStateModel tabState) : base(ViewerType.List, "List", "\uE8FD", filesManager) {
		var viewerState = tabState.ViewerState;

		this.ThumbnailColumn = new ListViewerColumn(ColumnIds.Thumbnail, "", 60, minWidth: 20, canHide: false);
		this.FileNameColumn = new ListViewerColumn(ColumnIds.FileName, "ファイル名", viewerState.ListFileNameColumnWidth.Value, minWidth: 120, isVisible: viewerState.ListFileNameColumnVisible.Value);
		this.ResolutionColumn = new ListViewerColumn(ColumnIds.Resolution, "解像度", viewerState.ListResolutionColumnWidth.Value, minWidth: 60, isVisible: viewerState.ListResolutionColumnVisible.Value);
		this.FileSizeColumn = new ListViewerColumn(ColumnIds.FileSize, "サイズ", viewerState.ListFileSizeColumnWidth.Value, minWidth: 60, isVisible: viewerState.ListFileSizeColumnVisible.Value);
		this.CreationTimeColumn = new ListViewerColumn(ColumnIds.CreationTime, "作成日時", viewerState.ListCreationTimeColumnWidth.Value, minWidth: 80, isVisible: viewerState.ListCreationTimeColumnVisible.Value);
		this.RateColumn = new ListViewerColumn(ColumnIds.Rate, "評価", viewerState.ListRateColumnWidth.Value, minWidth: 40, isVisible: viewerState.ListRateColumnVisible.Value);

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