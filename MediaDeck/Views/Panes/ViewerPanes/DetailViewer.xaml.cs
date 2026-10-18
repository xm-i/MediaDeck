using CommunityToolkit.WinUI;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// 詳細ビューアの View 実装。
/// </summary>
public sealed partial class DetailViewer {
	/// <summary>
	/// <see cref="DetailViewer"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public DetailViewer() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.List.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(this.HandleListPointerWheelChanged), true);
		};
	}

	/// <inheritdoc />
	protected override async void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		base.List_SelectionChanged(sender, e);
		if (sender is not ListView listView) {
			return;
		}
		if (listView.SelectedIndex == -1) {
			return;
		}
		await listView.SmoothScrollIntoViewWithIndexAsync(listView.SelectedIndex, ScrollItemPlacement.Center);
	}
}