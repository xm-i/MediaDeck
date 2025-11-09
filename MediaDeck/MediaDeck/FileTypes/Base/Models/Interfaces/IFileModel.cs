using System.Collections.Generic;
using System.Threading.Tasks;

using MediaDeck.Models.Files;
using MediaDeck.Utils.Enums;
using MediaDeck.Utils.Objects;

namespace MediaDeck.FileTypes.Base.Models.Interfaces;

public interface IFileModel {
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
	public GpsLocation? Location {
		get;
		set;
	}

	/// <summary>
	/// タグリスト
	/// </summary>
	public List<TagModel> Tags {
		get;
		set;
	}

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
	}

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
	public Attributes<string> Properties {
		get;
	}

	public Task UpdateRateAsync(int rate);

	public Task IncrementUsageCountAsync();

	public Task UpdateDescriptionAsync(string description);

	public Task ExecuteFileAsync();
}