using System.Reactive.Linq;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Utils.Objects;
using Reactive.Bindings;

namespace MediaDeck.Models.Maps;
/// <summary>
/// マップピン
/// </summary>
/// <remarks>
/// このグループを一つのピンとして表示する
/// </remarks>
public class MapPin {
	/// <summary>
	/// 代表メディア
	/// </summary>
	public IReactiveProperty<IFileModel> Core {
		get;
	} = new ReactivePropertySlim<IFileModel>();

	/// <summary>
	/// 表示領域
	/// </summary>
	/// <remarks>
	/// この領域がかぶるアイテムを吸収していく
	/// </remarks>
	public Rectangle CoreRectangle {
		get;
	}

	/// <summary>
	/// 件数
	/// </summary>
	public int Count {
		get;
		private set;
	}

	/// <summary>
	/// メディアファイルリスト
	/// VM作成中にコレクションが変化する可能性がある場合は必ずSyncRootでロックすること。
	/// </summary>
	public ObservableList<IFileModel> Items {
		get;
	} = [];

	public GpsLocation? Location {
		get;
	}

	/// <summary>
	/// ピン状態
	/// </summary>
	public IBindableReactiveProperty<PinState> PinState {
		get;
	} = new BindableReactiveProperty<PinState>(Maps.PinState.Unselected);

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="core">代表ファイル</param>
	/// <param name="rectangle">表示領域</param>
	public MapPin(IFileModel core, Rectangle rectangle) {
		this.Core.Value = core;
		this.Items.Add(core);
		this.CoreRectangle = rectangle;
		this.Location = this.Core.Value.Location;
		this.Count = this.Items.Count;
		this.Items.ObserveCountChanged().Subscribe(x => this.Count = x);
	}

	public override string ToString() {
		return $"<[{base.ToString()}] {this.Core.Value.FilePath}>";
	}
}

/// <summary>
/// ピン状態
/// </summary>
public enum PinState {
	Selected,
	Indeterminate,
	Unselected
}