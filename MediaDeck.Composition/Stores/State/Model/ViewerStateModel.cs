using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class ViewerStateModel {
	/// <summary>
	/// アイテムサイズ (Zoom)
	/// </summary>
	public ReactiveProperty<int> ItemSize {
		get;
	} = new(150);

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
	/// ListViewer: 列幅 (ファイル名)
	/// </summary>
	public ReactiveProperty<double> ListFileNameColumnWidth {
		get;
	} = new(400);

	/// <summary>
	/// ListViewer: 列幅 (解像度)
	/// </summary>
	public ReactiveProperty<double> ListResolutionColumnWidth {
		get;
	} = new(100);

	/// <summary>
	/// ListViewer: 列幅 (サイズ)
	/// </summary>
	public ReactiveProperty<double> ListFileSizeColumnWidth {
		get;
	} = new(100);

	/// <summary>
	/// ListViewer: 列幅 (作成日時)
	/// </summary>
	public ReactiveProperty<double> ListCreationTimeColumnWidth {
		get;
	} = new(150);

	/// <summary>
	/// ListViewer: 列幅 (評価)
	/// </summary>
	public ReactiveProperty<double> ListRateColumnWidth {
		get;
	} = new(60);

	/// <summary>
	/// ListViewer: 表示状態 (ファイル名)
	/// </summary>
	public ReactiveProperty<bool> ListFileNameColumnVisible {
		get;
	} = new(true);

	/// <summary>
	/// ListViewer: 表示状態 (解像度)
	/// </summary>
	public ReactiveProperty<bool> ListResolutionColumnVisible {
		get;
	} = new(true);

	/// <summary>
	/// ListViewer: 表示状態 (サイズ)
	/// </summary>
	public ReactiveProperty<bool> ListFileSizeColumnVisible {
		get;
	} = new(true);

	/// <summary>
	/// ListViewer: 表示状態 (作成日時)
	/// </summary>
	public ReactiveProperty<bool> ListCreationTimeColumnVisible {
		get;
	} = new(true);

	/// <summary>
	/// ListViewer: 表示状態 (評価)
	/// </summary>
	public ReactiveProperty<bool> ListRateColumnVisible {
		get;
	} = new(true);
}