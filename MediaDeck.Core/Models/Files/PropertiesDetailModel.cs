using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables.Metadata;
using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Files;

/// <summary>
/// プロパティ詳細（プロパティ、メタデータ、評価値、説明文）のビジネスロジックを管理するモデル。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class PropertiesDetailModel : ModelBase {
	private readonly SerialDisposable _propertyChangedSubscription = new();

	/// <summary>
	/// プロパティ一覧
	/// </summary>
	public ReactiveProperty<FileProperty[]> Properties {
		get;
	} = new([]);

	/// <summary>
	/// 評価
	/// </summary>
	public ReactiveProperty<double> Rate {
		get;
	} = new();

	/// <summary>
	/// 使用回数
	/// </summary>
	public ReactiveProperty<double> UsageCount {
		get;
	} = new();

	/// <summary>
	/// 代表ファイルのパス（複数選択時は空文字）
	/// </summary>
	public ReactiveProperty<string> RepresentativeFilePath {
		get;
	} = new(string.Empty);

	/// <summary>
	/// 説明
	/// </summary>
	public ReactiveProperty<string> Description {
		get;
	} = new(string.Empty);

	/// <summary>
	/// メタデータ
	/// </summary>
	public ReactiveProperty<MediaMetadata?> Metadata {
		get;
	} = new();

	/// <summary>
	/// <see cref="PropertiesDetailModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public PropertiesDetailModel() {
		this._propertyChangedSubscription.AddTo(this.CompositeDisposable);
		this.Properties.AddTo(this.CompositeDisposable);
		this.Rate.AddTo(this.CompositeDisposable);
		this.UsageCount.AddTo(this.CompositeDisposable);
		this.RepresentativeFilePath.AddTo(this.CompositeDisposable);
		this.Description.AddTo(this.CompositeDisposable);
		this.Metadata.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 対象ファイル群から各種状態を算出してプロパティを更新します。
	/// </summary>
	/// <param name="files">対象ファイル群</param>
	public void Refresh(IMediaItemModel[] files) {
		if (files.Length > 0) {
			this.Properties.Value =
				files
					.SelectMany(x => x.Properties)
					.GroupBy(x => x.Title)
					.Select(x => new FileProperty(x.Key,
						x.GroupBy(g => g.Value).Select(g => new ValueCountPair<string?>(g.Key, g.Count()))))
					.ToArray();
			this.Rate.Value = files.Average(x => x.Rate);
			this.UsageCount.Value = files.Average(x => x.UsageCount);
		} else {
			this.Properties.Value = [];
			this.Rate.Value = 0;
			this.UsageCount.Value = 0;
		}

		if (files.Length == 1) {
			this.RepresentativeFilePath.Value = files[0].FilePath;
			this.Description.Value = files[0].Description;
			this.Metadata.Value = files[0].Metadata;
		} else {
			this.RepresentativeFilePath.Value = string.Empty;
			this.Description.Value = string.Empty;
			this.Metadata.Value = null;
		}

		this._propertyChangedSubscription.Disposable =
			files
				.Where(m => m.Changed != null)
				.Select(m => m.Changed)
				.Merge()
				.Subscribe(_ => {
					this.Refresh(files);
				});
	}

	/// <summary>
	/// 説明文を更新します。
	/// </summary>
	/// <param name="file">対象ファイル</param>
	/// <param name="description">新しい説明文</param>
	public async Task UpdateDescriptionAsync(IMediaItemModel file, string description) {
		await file.UpdateDescriptionAsync(description);
	}

	/// <summary>
	/// 評価値を更新します。
	/// </summary>
	/// <param name="files">対象ファイル群</param>
	/// <param name="rate">新しい評価値</param>
	public async Task UpdateRateAsync(IMediaItemModel[] files, int rate) {
		foreach (var file in files) {
			await file.UpdateRateAsync(rate);
		}
	}
}