using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;

namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグ管理のインターフェース
/// </summary>
public interface ITagsManager {
	/// <summary>
	/// タグカテゴリーのリスト
	/// </summary>
	public IReadOnlyObservableList<ITagCategoryModel> TagCategories {
		get;
	}

	/// <summary>
	/// 未分類のタグカテゴリー（システム定義）
	/// </summary>
	public ITagCategoryModel NoCategory {
		get;
	}

	/// <summary>
	/// 全タグのリスト
	/// </summary>
	public IReadOnlyObservableList<ITagModel> Tags {
		get;
	}

	/// <summary>
	/// タグの内容や所属カテゴリーが更新されたことを通知するストリーム
	/// </summary>
	public Observable<ITagModel> TagChanged {
		get;
	}

	/// <summary>
	/// 新規タグカテゴリーを即座に作成し、データベースに保存する
	/// </summary>
	/// <param name="name">カテゴリー名</param>
	/// <param name="detail">説明</param>
	/// <returns>作成されたタグカテゴリー</returns>
	public Task<ITagCategoryModel> CreateTagCategoryAsync(string name = "", string detail = "");

	/// <summary>
	/// タグカテゴリーを更新し、データベースに保存する
	/// </summary>
	/// <param name="categoryModel">更新対象のカテゴリーモデル</param>
	/// <param name="name">新しいカテゴリー名</param>
	/// <param name="detail">新しい説明</param>
	/// <returns>タスク</returns>
	public Task UpdateTagCategoryAsync(ITagCategoryModel categoryModel, string name, string detail);

	/// <summary>
	/// タグカテゴリをシステムから完全に削除する
	/// </summary>
	/// <param name="categoryModel">削除するタグカテゴリのモデル</param>
	/// <returns>タスク</returns>
	public Task DeleteTagCategoryAsync(ITagCategoryModel categoryModel);

	/// <summary>
	/// 名前でタグを検索する
	/// </summary>
	/// <param name="tagName">タグ名</param>
	/// <returns>見つかったタグ。見つからない場合は null</returns>
	public Task<ITagModel?> FindTagByNameAsync(string tagName);

	/// <summary>
	/// タグを即座に作成し、データベースに保存する
	/// </summary>
	/// <param name="tagCategoryId">カテゴリーID</param>
	/// <param name="tagName">タグ名</param>
	/// <param name="ruby">読み</param>
	/// <param name="detail">詳細</param>
	/// <param name="aliases">別名リスト</param>
	/// <returns>作成されたタグ</returns>
	public Task<ITagModel?> CreateTagImmediatelyAsync(int? tagCategoryId, string tagName, string? ruby, string detail, IEnumerable<ITagAliasModel> aliases);

	/// <summary>
	/// タグの内容を更新し、データベースに保存する
	/// </summary>
	/// <param name="tagModel">更新対象のタグモデル</param>
	/// <param name="targetCategory">所属先のカテゴリーモデル</param>
	/// <param name="tagName">タグ名</param>
	/// <param name="ruby">読み</param>
	/// <param name="detail">詳細</param>
	/// <param name="aliases">別名リスト</param>
	/// <returns>タスク</returns>
	public Task UpdateTagAsync(ITagModel tagModel, ITagCategoryModel targetCategory, string tagName, string? ruby, string detail, IEnumerable<ITagAliasModel> aliases);

	/// <summary>
	/// タグをシステムから完全に削除する
	/// </summary>
	/// <param name="tagModel">削除するタグのモデル</param>
	/// <returns>タスク</returns>
	public Task DeleteTagAsync(ITagModel tagModel);

	/// <summary>
	/// ファイルにタグを追加する
	/// </summary>
	/// <param name="fileModels">対象ファイル</param>
	/// <param name="tag">追加するタグ</param>
	/// <returns>タスク</returns>
	public Task AddTagAsync(IMediaItemModel[] fileModels, ITagModel tag);

	/// <summary>
	/// ファイルからタグを削除する
	/// </summary>
	/// <param name="fileModels">対象ファイル</param>
	/// <param name="tagId">削除するタグのID</param>
	/// <returns>タスク</returns>
	public Task RemoveTagAsync(IMediaItemModel[] fileModels, int tagId);

	/// <summary>
	/// データを初期化（ロード）する
	/// </summary>
	/// <returns>タスク</returns>
	public Task InitializeAsync();
}