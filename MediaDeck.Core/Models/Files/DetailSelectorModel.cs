using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Files;

/// <summary>
/// 詳細セレクタのビジネスロジックを管理するモデル
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class DetailSelectorModel : IDisposable {
	private readonly SerialDisposable _propertyChangedSubscription = new();
	private readonly ITagsManager _tagsManager;
	private readonly ObservableList<ValueCountPair<ITagModel>> _tags = [];

	/// <summary>
	/// TargetFilesの中身の変更を通知するためのSubject
	/// </summary>
	public Subject<Unit> ContentChanged {
		get;
	} = new();

	/// <summary>
	/// タグリスト
	/// </summary>
	public ObservableList<ValueCountPair<ITagModel>> Tags {
		get {
			return this._tags;
		}
	}

	/// <summary>
	/// TagsManagerへのアクセス
	/// </summary>
	public ITagsManager TagsManager {
		get {
			return this._tagsManager;
		}
	}

	/// <summary>
	/// タグカテゴリ
	/// </summary>
	public IReadOnlyObservableList<ITagCategoryModel> TagCategories {
		get {
			return this._tagsManager.TagCategories;
		}
	}

	/// <summary>
	/// タグ候補
	/// </summary>
	public IReadOnlyObservableList<ITagModel> TagModels {
		get {
			return this._tagsManager.Tags;
		}
	}

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
	/// 代表ファイルのパス 複数選択時は空文字
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

	public DetailSelectorModel(ITagsManager tagsManager) {
		this._tagsManager = tagsManager;
	}

	/// <summary>
	/// TargetFilesから各種状態を算出してプロパティを更新する
	/// </summary>
	public void Refresh(IFileModel[] files) {
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
			this.RefreshTags(files);
		} else {
			this.Properties.Value = [];
			this.Rate.Value = 0;
			this.UsageCount.Value = 0;
			this._tags.Clear();
		}

		if (files.Length == 1) {
			this.RepresentativeFilePath.Value = files[0].FilePath;
			this.Description.Value = files[0].Description;
		} else {
			this.RepresentativeFilePath.Value = string.Empty;
			this.Description.Value = string.Empty;
		}

		this._propertyChangedSubscription.Disposable =
			files
				.Select(m => m.Changed)
				.Merge()
				.Subscribe(_ => {
					this.Refresh(files);
				});
	}

	public void Dispose() {
		this._propertyChangedSubscription.Dispose();
	}

	/// <summary>
	/// タグリストを再計算して更新する
	/// </summary>
	public void RefreshTags(IFileModel[] files) {
		this._tags.Clear();
		this._tags.AddRange(files
			.SelectMany(x => x.Tags)
			.GroupBy(x => x.TagId)
			.Select(x => new ValueCountPair<ITagModel>(x.First(), x.Count())));
	}

	/// <summary>
	/// ファイルにタグを追加する
	/// </summary>
	public async Task AddTagAsync(IFileModel[] files, ITagModel tag) {
		await this._tagsManager.AddTagAsync(files, tag);
		this.RefreshTags(files);
	}

	/// <summary>
	/// ファイルからタグを削除する
	/// </summary>
	public async Task RemoveTagAsync(IFileModel[] files, int tagId) {
		await this._tagsManager.RemoveTagAsync(files, tagId);
		this.RefreshTags(files);
	}

	/// <summary>
	/// タグを名前で検索する
	/// </summary>
	public Task<ITagModel?> FindTagByNameAsync(string tagName) {
		return this._tagsManager.FindTagByNameAsync(tagName);
	}

	/// <summary>
	/// 説明を更新する
	/// </summary>
	public async Task UpdateDescriptionAsync(IFileModel file, string description) {
		await file.UpdateDescriptionAsync(description);
	}

	/// <summary>
	/// 評価を更新する
	/// </summary>
	public async Task UpdateRateAsync(IFileModel[] files, int rate) {
		foreach (var file in files) {
			await file.UpdateRateAsync(rate);
		}
	}

	/// <summary>
	/// タグ候補のフィルタリング条件に一致するか判定する
	/// </summary>
	/// <param name="tag">判定対象のタグ</param>
	/// <param name="text">検索テキスト</param>
	/// <param name="representativeText">一致したエイリアスのテキスト（直接一致の場合はnull）</param>
	/// <returns>フィルタ条件に一致する場合true</returns>
	public static bool MatchesTagFilter(ITagModel tag, string text, out string? representativeText) {
		representativeText = null;
		if (string.IsNullOrEmpty(text)) {
			return false;
		}
		if (tag.TagName.Contains(text) ||
			(tag.Ruby?.Contains(text) ?? false) ||
			(tag.Romaji?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
			return true;
		}
		var result =
			tag
				.TagAliases
				.FirstOrDefault(x =>
					x.Alias.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
					(x.Ruby?.Contains(text) ?? false) ||
					(x.Romaji?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false));
		representativeText = result?.Alias;
		return result != null;
	}
}