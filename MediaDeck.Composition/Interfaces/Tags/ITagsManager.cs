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
	/// タグの情報を更新する
	/// </summary>
	/// <param name="tagId">タグID</param>
	/// <param name="tagCategoryId">カテゴリーID</param>
	/// <param name="tagName">タグ名</param>
	/// <param name="detail">詳細</param>
	/// <param name="aliases">別名リスト</param>
	/// <returns>タスク</returns>
	public Task UpdateTagAsync(int tagId, int? tagCategoryId, string tagName, string detail, IEnumerable<ITagAliasModel> aliases);

	/// <summary>
	/// タグカテゴリーを新規作成する
	/// </summary>
	/// <param name="tagCategoryName">カテゴリー名</param>
	/// <param name="detail">詳細</param>
	/// <returns>作成されたタグカテゴリーモデル</returns>
	public Task<ITagCategoryModel> CreateTagCategoryAsync(string tagCategoryName, string detail);

	/// <summary>
	/// タグカテゴリーを更新する
	/// </summary>
	/// <param name="tagCategoryId">カテゴリーID</param>
	/// <param name="tagCategoryName">カテゴリー名</param>
	/// <param name="detail">詳細</param>
	/// <returns>タスク</returns>
	public Task UpdateTagCategoryAsync(int tagCategoryId, string tagCategoryName, string detail);

	/// <summary>
	/// データを初期化（ロード）する
	/// </summary>
	/// <returns>タスク</returns>
	public Task InitializeAsync();
}