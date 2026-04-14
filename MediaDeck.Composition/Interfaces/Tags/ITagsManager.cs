using MediaDeck.Composition.Interfaces.FileTypes.Models;

namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグ管理のインターフェース
/// </summary>
public interface ITagsManager {
	/// <summary>
	/// タグカテゴリーのリスト
	/// </summary>
	public ObservableList<ITagCategoryModel> TagCategories {
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
	public ObservableList<ITagModel> Tags {
		get;
	}

	/// <summary>
	/// 名前でタグを検索する
	/// </summary>
	/// <param name="tagName">タグ名</param>
	/// <returns>見つかったタグ。見つからない場合は null</returns>
	public Task<ITagModel?> FindTagByNameAsync(string tagName);

	/// <summary>
	/// タグを作成する
	/// </summary>
	/// <param name="tagCategoryId">カテゴリーID</param>
	/// <param name="tagName">タグ名</param>
	/// <param name="detail">詳細</param>
	/// <param name="aliases">別名リスト</param>
	/// <returns>作成されたタグ</returns>
	public Task<ITagModel?> CreateTagAsync(int? tagCategoryId, string tagName, string detail, IEnumerable<ITagAliasModel> aliases);

	/// <summary>
	/// ファイルにタグを追加する
	/// </summary>
	/// <param name="fileModels">対象ファイル</param>
	/// <param name="tag">追加するタグ</param>
	/// <returns>タスク</returns>
	public Task AddTagAsync(IFileModel[] fileModels, ITagModel tag);

	/// <summary>
	/// ファイルからタグを削除する
	/// </summary>
	/// <param name="fileModels">対象ファイル</param>
	/// <param name="tagId">削除するタグのID</param>
	/// <returns>タスク</returns>
	public Task RemoveTagAsync(IFileModel[] fileModels, int tagId);

	/// <summary>
	/// タグカテゴリをシステムから完全に削除する
	/// </summary>
	/// <param name="categoryModel">削除するタグカテゴリのモデル</param>
	/// <returns>タスク</returns>
	public Task DeleteTagCategoryAsync(ITagCategoryModel categoryModel);

	/// <summary>
	/// タグをシステムから完全に削除する
	/// </summary>
	/// <param name="tagModel">削除するタグのモデル</param>
	/// <returns>タスク</returns>
	public Task DeleteTagAsync(ITagModel tagModel);

	/// <summary>
	/// 変更を全て保存する
	/// </summary>
	/// <returns>タスク</returns>
	public Task SaveAsync();

	/// <summary>
	/// データを初期化（ロード）する
	/// </summary>
	/// <returns>タスク</returns>
	public Task InitializeAsync();
}