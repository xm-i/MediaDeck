using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Files;

/// <summary>
/// タグ詳細（タグ付与・削除・集計・タグ候補サジェスト用モデル取得）のビジネスロジックを管理するモデル。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class TagsDetailModel : ModelBase {
	private readonly SerialDisposable _propertyChangedSubscription = new();
	private readonly ITagsManager _tagsManager;
	private readonly ObservableList<ValueCountPair<ITagModel>> _tags = [];

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
	/// タグカテゴリ一覧
	/// </summary>
	public IReadOnlyObservableList<ITagCategoryModel> TagCategories {
		get {
			return this._tagsManager.TagCategories;
		}
	}

	/// <summary>
	/// タグ候補一覧
	/// </summary>
	public IReadOnlyObservableList<ITagModel> TagModels {
		get {
			return this._tagsManager.Tags;
		}
	}

	/// <summary>
	/// <see cref="TagsDetailModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagsManager">タグマネージャー</param>
	public TagsDetailModel(ITagsManager tagsManager) {
		this._tagsManager = tagsManager;
		this._propertyChangedSubscription.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タグリストを再計算して更新します。
	/// </summary>
	/// <param name="files">対象ファイル群</param>
	public void Refresh(IMediaItemModel[] files) {
		this._tags.Clear();
		if (files.Length > 0) {
			this._tags.AddRange(files
				.SelectMany(x => x.Tags)
				.GroupBy(x => x.TagId)
				.Select(x => new ValueCountPair<ITagModel>(x.First(), x.Count())));
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
	/// ファイル群にタグを追加します。
	/// </summary>
	/// <param name="files">対象ファイル群</param>
	/// <param name="tag">追加するタグ</param>
	public async Task AddTagAsync(IMediaItemModel[] files, ITagModel tag) {
		await this._tagsManager.AddTagAsync(files, tag);
		this.Refresh(files);
	}

	/// <summary>
	/// ファイル群からタグを削除します。
	/// </summary>
	/// <param name="files">対象ファイル群</param>
	/// <param name="tagId">削除するタグID</param>
	public async Task RemoveTagAsync(IMediaItemModel[] files, int tagId) {
		await this._tagsManager.RemoveTagAsync(files, tagId);
		this.Refresh(files);
	}

	/// <summary>
	/// タグを名前で検索します。
	/// </summary>
	/// <param name="tagName">タグ名</param>
	/// <returns>見つかったタグモデル。見つからない場合は null</returns>
	public Task<ITagModel?> FindTagByNameAsync(string tagName) {
		return this._tagsManager.FindTagByNameAsync(tagName);
	}
}