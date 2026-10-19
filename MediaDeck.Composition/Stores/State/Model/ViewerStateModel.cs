using GenJsonConfig.Attributes;
using MediaDeck.Composition.Constants;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// ビューワーの状態モデル
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateJsonConfigDto]
public class ViewerStateModel {
	/// <summary>
	/// アイテムサイズ (Zoom)
	/// </summary>
	public ReactiveProperty<int> ItemSize {
		get;
	} = new(150);

	/// <summary>
	/// 現在選択されているビューワーの種類
	/// </summary>
	public ReactiveProperty<ViewerType> ActiveViewer {
		get;
	} = new(ViewerType.Wrap);

	/// <summary>
	/// サムネイル上オーバーレイ表示
	/// </summary>
	public ReactiveProperty<bool> ShowOverlay {
		get;
	} = new(true);

	/// <summary>
	/// 情報エリア表示
	/// </summary>
	public ReactiveProperty<bool> ShowInfo {
		get;
	} = new(true);

	/// <summary>
	/// ListViewer: 列設定リスト
	/// </summary>
	public ObservableList<ListViewerColumnStateModel> ListColumns {
		get;
	} = CreateDefaultColumns();

	/// <summary>
	/// 指定した列IDの列状態を取得します。存在しない場合は作成して追加します。
	/// </summary>
	/// <param name="columnId">列ID</param>
	/// <param name="defaultWidth">デフォルト幅</param>
	/// <param name="defaultVisible">デフォルト表示状態</param>
	/// <returns>列状態モデル</returns>
	public ListViewerColumnStateModel GetOrCreateColumnState(string columnId, double defaultWidth = 100, bool defaultVisible = true) {
		var column = this.ListColumns.FirstOrDefault(x => x.ColumnId == columnId);
		if (column is null) {
			column = new ListViewerColumnStateModel(columnId, defaultWidth, defaultVisible);
			this.ListColumns.Add(column);
		}
		return column;
	}

	/// <summary>
	/// 別の <see cref="ViewerStateModel"/> から設定値をコピーします。
	/// </summary>
	/// <param name="source">コピー元のモデル</param>
	public void CopyFrom(ViewerStateModel source) {
		this.ItemSize.Value = source.ItemSize.Value;
		this.ActiveViewer.Value = source.ActiveViewer.Value;
		this.ShowOverlay.Value = source.ShowOverlay.Value;
		this.ShowInfo.Value = source.ShowInfo.Value;

		foreach (var sourceCol in source.ListColumns) {
			var targetCol = this.GetOrCreateColumnState(sourceCol.ColumnId, sourceCol.Width.Value, sourceCol.IsVisible.Value);
			targetCol.Width.Value = sourceCol.Width.Value;
			targetCol.IsVisible.Value = sourceCol.IsVisible.Value;
		}
	}

	private static ObservableList<ListViewerColumnStateModel> CreateDefaultColumns() {
		return [
		new(ListViewerColumnIds.FileName, 400, true),
		new(ListViewerColumnIds.Resolution, 100, true),
		new(ListViewerColumnIds.FileSize, 100, true),
		new(ListViewerColumnIds.CreationTime, 150, true),
		new(ListViewerColumnIds.Rate, 60, true),
	];
	}
}