using MediaDeck.ViewModels.Filters;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace MediaDeck.Views.Filters;

/// <summary>
/// フィルター条件管理ビュー。
/// </summary>
public sealed partial class FilterManagerView {
	/// <summary>
	/// <see cref="FilterManagerView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FilterManagerView() {
		this.InitializeComponent();
	}

	private void FilterCreatorChip_Click(object sender, RoutedEventArgs e) {
		if (sender is ToggleButton button && button.Tag is IFilterCreatorViewModel creator) {
			this.ViewModel?.SelectFilterCreatorCommand.Execute(creator);
			// 選択中チップを再クリックしても未選択状態へ戻らないよう選択状態を維持する
			button.IsChecked = creator.IsSelected.Value;
		}
	}
}

/// <summary>
/// <see cref="FilterManagerView"/> の基底クラス。
/// </summary>
public class FilterManagerViewUserControl : UserControlBase<FilterManagerViewModel>;