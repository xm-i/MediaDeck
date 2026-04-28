using System.Collections.Generic;
using System.Threading.Tasks;
using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;

namespace MediaDeck.MediaItemTypes.Base.Models;

public abstract class BaseMediaItemModel : ModelBase, IMediaItemModel {
	private readonly IMediaItemTypeProvider _mediaItemTypeProvider;
	private readonly IServiceProvider _scopedServiceProvider;

	protected IMediaItemOperator FileOperator {
		get;
	}

	private readonly Subject<Unit> _changed = new();

	public Observable<Unit> Changed {
		get {
			return this._changed.AsObservable();
		}
	}

	/// <summary>
	/// BaseMediaItemModel のコンストラクタ。
	/// </summary>
	/// <param name="id">メディアアイテムID</param>
	/// <param name="filePath">ファイルパス</param>
	/// <param name="fileOperator">ファイルオペレーター</param>
	/// <param name="mediaType">メディアタイプ</param>
	/// <param name="mediaItemTypeProvider">このモデルに対応するメディアアイテムタイプ。実行ロジック等の委譲先。</param>
	public BaseMediaItemModel(long id, string filePath, IMediaItemOperator fileOperator, MediaType mediaType, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : base() {
		this._mediaItemTypeProvider = mediaItemTypeProvider;
		this._scopedServiceProvider = scopedServiceProvider;
		this.FileOperator = fileOperator;
		this.Id = id;
		this.FilePath = filePath;
		this.MediaType = mediaType;
		this._changed.AddTo(this.CompositeDisposable);
	}

	public MediaType MediaType {
		get;
	}

	public long Id {
		get;
	}

	public string FilePath {
		get;
	}

	public string? ThumbnailFilePath {
		get;
		set;
	}

	public bool Exists {
		get;
		set;
	}

	/// <summary>
	/// 座標
	/// </summary>
	public IGpsLocation? Location {
		get;
		set;
	}

	/// <summary>
	/// タグリスト
	/// </summary>
	public List<ITagModel> Tags {
		get;
		set;
	} = [];

	/// <summary>
	/// 解像度
	/// </summary>
	public ComparableSize? Resolution {
		get;
		set;
	}

	/// <summary>
	/// 評価
	/// </summary>
	public int Rate {
		get;
		set;
	}

	/// <summary>
	/// 使用回数
	/// </summary>
	public int UsageCount {
		get;
		set;
	}

	/// <summary>
	/// 説明
	/// </summary>
	public string Description {
		get;
		set;
	} = "";

	/// <summary>
	/// 作成日時
	/// </summary>
	public DateTime CreationTime {
		get;
		set;
	}

	/// <summary>
	/// 編集日時
	/// </summary>
	public DateTime ModifiedTime {
		get;
		set;
	}

	/// <summary>
	/// 最終アクセス日時
	/// </summary>
	public DateTime LastAccessTime {
		get;
		set;
	}


	/// <summary>
	/// 登録日時
	/// </summary>
	public DateTime RegisteredTime {
		get;
		set;
	}

	/// <summary>
	/// ファイルサイズ
	/// </summary>
	public long FileSize {
		get;
		set;
	}

	/// <summary>
	/// プロパティ
	/// </summary>
	public virtual Attributes<string> Properties {
		get {
			return new Dictionary<string, string> {
				{ "作成日時", $"{this.CreationTime}" },
				{ "編集日時", $"{this.ModifiedTime}" },
				{ "最終アクセス日時", $"{this.LastAccessTime}" },
				{ "登録日時", $"{this.RegisteredTime}" },
				{ "ファイルサイズ", $"{StringUtility.LongToFileSize(this.FileSize)}" },
				{ "解像度", $"{this.Resolution?.ToString()}" }
			}.ToAttributes();
		}
	}

	public async Task UpdateRateAsync(int rate) {
		await this.FileOperator.UpdateRateAsync(this.Id, rate);
		this.Rate = rate;
		this._changed.OnNext(Unit.Default);
	}

	public async Task IncrementUsageCountAsync() {
		await this.FileOperator.IncrementUsageCountAsync(this.Id);
		this.UsageCount++;
		this._changed.OnNext(Unit.Default);
	}

	public async Task UpdateDescriptionAsync(string description) {
		await this.FileOperator.UpdateDescriptionAsync(this.Id, description);
		this.Description = description;
		this._changed.OnNext(Unit.Default);
	}

	/// <summary>
	/// ファイルを実行する。実行ロジックは IMediaItemType.ExecuteAsync に委譲する。
	/// </summary>
	public async Task ExecuteFileAsync() {
		await this._mediaItemTypeProvider.ExecuteAsync(this.FilePath, this._scopedServiceProvider);
		await this.IncrementUsageCountAsync();
	}
}