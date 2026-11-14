namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグカテゴリーのモデルクラスのインターフェース
/// </summary>
public interface ITagCategoryModel : IDisposable {
	/// <summary>
	/// タグカテゴリーID
	/// </summary>
	public int? TagCategoryId {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリー名
	/// </summary>
	public string TagCategoryName {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリーの説明
	/// </summary>
	public string Detail {
		get;
		set;
	}

	/// <summary>
	/// カテゴリーに属するタグのリスト
	/// </summary>
	public IReadOnlyObservableList<ITagModel> Tags {
		get;
	}

	/// <summary>
	/// プロパティ等の変更通知ストリーム
	/// </summary>
	public Observable<Unit> Changed {
		get;
	}

	/// <summary>
	/// 変更通知を発火します。
	/// </summary>
	public void NotifyChanged();

	/// <summary>
	/// タグを追加する
	/// </summary>
	/// <param name="tag">タグ</param>
	public void AddTag(ITagModel tag);

	/// <summary>
	/// タグを削除する
	/// </summary>
	/// <param name="tag">タグ</param>
	public void RemoveTag(ITagModel tag);

	/// <summary>
	/// タグを一括追加する
	/// </summary>
	/// <param name="tags">タグのリスト</param>
	public void AddTagRange(IEnumerable<ITagModel> tags);

	/// <summary>
	/// 全てのタグをクリアする
	/// </summary>
	public void ClearTags();
}