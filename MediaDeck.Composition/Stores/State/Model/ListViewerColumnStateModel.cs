using GenJsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// ListViewer の列ごとの表示状態（幅・可視性）モデル
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateJsonConfigDto]
public class ListViewerColumnStateModel {
	/// <summary>
	/// 列ID（<see cref="MediaDeck.Composition.Constants.ListViewerColumnIds"/> 定数に対応）
	/// </summary>
	public string ColumnId {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// 列幅 (px)
	/// </summary>
	public ReactiveProperty<double> Width {
		get;
	} = new(100);

	/// <summary>
	/// 表示状態
	/// </summary>
	public ReactiveProperty<bool> IsVisible {
		get;
	} = new(true);

	/// <summary>
	/// <see cref="ListViewerColumnStateModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ListViewerColumnStateModel() {
	}

	/// <summary>
	/// <see cref="ListViewerColumnStateModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="columnId">列ID</param>
	/// <param name="width">列幅</param>
	/// <param name="isVisible">表示状態</param>
	public ListViewerColumnStateModel(string columnId, double width, bool isVisible) {
		this.ColumnId = columnId;
		this.Width.Value = width;
		this.IsVisible.Value = isVisible;
	}
}