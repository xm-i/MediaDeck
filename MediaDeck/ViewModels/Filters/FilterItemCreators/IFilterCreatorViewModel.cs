using System.ComponentModel;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;
/// <summary>
/// フィルター作成VM
/// </summary>
public interface IFilterCreatorViewModel: INotifyPropertyChanged {
	/// <summary>
	/// 表示名
	/// </summary>
	public string Title {
		get;
	}

	/// <summary>
	/// 追加コマンド
	/// </summary>
	public ReactiveCommand AddFilterCommand {
		get;
	}
}
