using System.Collections.Generic;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Database.Tables;

namespace MediaDeck.Models.Files; 
public class TagModel: ITagModel {
	[Obsolete("for serialize")]
	public TagModel() {
		this.TagCategory = null!;
		this.TagName = null!;
		this.Detail = null!;
		this.Romaji = null!;
		this.TagAliases = null!;
	}
	public TagModel(Tag tag) {
		this.TagId = tag.TagId;
		this.TagCategoryId = tag.TagCategoryId;
		this.TagCategory = new TagCategoryModel(tag.TagCategory);
		this.TagName = tag.TagName;
		this.Detail = tag.Detail;
		this.Romaji = tag.TagName.KatakanaToHiragana().HiraganaToRomaji();
		this.TagAliases = [.. tag.TagAliases.Select(x => new TagAliasModel(x))];
	}

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
	} = new();
}

public class TagCategoryModel: ITagCategoryModel {
	[Obsolete("for serialize")]
	public TagCategoryModel() {
		this.TagCategoryName = null!;
		this.Detail = null!;
	}
	public TagCategoryModel(TagCategory tagCategory) {
		this.TagCategoryId = tagCategory.TagCategoryId;
		this.TagCategoryName = tagCategory.TagCategoryName;
		this.Detail = tagCategory.Detail;
	}

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

public class TagAliasModel: ITagAliasModel {
	[Obsolete("for serialize")]
	public TagAliasModel() {
		this.Alias = null!;
	}
	public TagAliasModel(TagAlias tagAlias) {
		this.TagAliasId = tagAlias.TagAliasId;
		this.TagId = tagAlias.TagId;
		this.Alias = tagAlias.Alias;
		this.Ruby = tagAlias.Ruby;
		this.Romaji = (tagAlias.Ruby ?? tagAlias.Alias.KatakanaToHiragana()).HiraganaToRomaji();
	}
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
