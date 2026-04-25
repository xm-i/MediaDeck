using System.Collections.Generic;
using System.Threading.Tasks;
using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.MediaItemTypes.Base.Models;

internal abstract class BaseMediaItemModel : ModelBase, IMediaItemModel {
	private readonly ConfigModel _config;

	protected IMediaItemOperator FileOperator {
		get;
	}

	private readonly Subject<Unit> _changed = new();

	public Observable<Unit> Changed {
		get {
			return this._changed.AsObservable();
		}
	}

	internal BaseMediaItemModel(long id, string filePath, IMediaItemOperator fileOperator, MediaType mediaType, ConfigModel config) : base() {
		this._config = config;
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

	public async Task ExecuteFileAsync() {
		var epo = this._config.ExecutionConfig.ExecutionPrograms.FirstOrDefault(x => x.MediaType.Value == this.MediaType);
		if (epo is null) {
			ShellUtility.ShellExecute(this.FilePath);
		} else {
			var arguments = string.Format(epo.Args.Value, $"\"{this.FilePath}\"");
			ShellUtility.ShellExecute(epo.Path.Value, arguments);
		}

		await this.IncrementUsageCountAsync();
	}
}