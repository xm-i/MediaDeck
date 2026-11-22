using System.ComponentModel;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// フィルター作成VM
/// </summary>
public interface IFilterCreatorViewModel : INotifyPropertyChanged {
	/// <summary>
	/// フィルター作成種別を取得します。
	/// </summary>
	public FilterCreatorKind Kind {
		get;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public string Title {
		get;
	}

	/// <summary>
	/// カテゴリチップ表示用の短いタイトルを取得します。
	/// </summary>
	public string ShortTitle {
		get;
	}

	/// <summary>
	/// Segoe Fluent Icons のグリフ文字列を取得します。
	/// </summary>
	public string IconGlyph {
		get;
	}

	/// <summary>
	/// フィルターの説明文を取得します。
	/// </summary>
	public string Description {
		get;
	}

	/// <summary>
	/// 現在選択されているかどうかを表すプロパティを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// 追加コマンドを取得します。
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}
}