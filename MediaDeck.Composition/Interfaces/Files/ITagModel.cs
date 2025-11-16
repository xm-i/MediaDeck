using System.Collections.Generic;

namespace MediaDeck.Composition.Interfaces.Files; 
public interface ITagModel {
	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get;
		set;
	}

	/// <summary>
	/// タグ分類
	/// </summary>
	public int TagCategoryId {
		get;
		set;
	}

	public ITagCategoryModel TagCategory {
		get;
		set;
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public string TagName {
		get;
		set;
	}

	/// <summary>
	/// タグ説明
	/// </summary>
	public string Detail {
		get;
		set;
	}

	public string Romaji {
		get;
		set;
	}

	public List<ITagAliasModel> TagAliases {
		get;
		set;
	}

	public BindableReactiveProperty<string?> RepresentativeText {
		get;
		set;
	}
}

public interface ITagCategoryModel {
	/// <summary>
	/// タグ分類ID
	/// </summary>
	public int TagCategoryId {
		get;
		set;
	}

	/// <summary>
	/// タグ分類名
	/// </summary>
	public string TagCategoryName {
		get;
		set;
	}

	/// <summary>
	/// タグ分類の説明
	/// </summary>
	public string Detail {
		get;
		set;
	}
}

public interface ITagAliasModel {
	/// <summary>
	/// タグ別名ID
	/// </summary>
	public int TagAliasId {
		get;
		set;
	}

	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get;
		set;
	}

	/// <summary>
	/// 別名
	/// </summary>
	public string Alias {
		get;
		set;
	}

	/// <summary>
	/// 読み仮名
	/// </summary>
	public string? Ruby {
		get;
		set;
	}

	public string? Romaji {
		get;
		set;
	}
}
