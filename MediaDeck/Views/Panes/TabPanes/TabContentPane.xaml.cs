using MediaDeck.ViewModels;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.TabPanes;

/// <summary>
/// タブのコンテンツ領域（各ペインとグリッドスプリッターの配置）を表示するビュー。
/// </summary>
public sealed partial class TabContentPane {
	/// <summary>
	/// <see cref="TabContentPane"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TabContentPane() {
		this.InitializeComponent();
		this.Loaded += (_, _) => this.UpdateRepositoryPaneMaxHeight(this.RootGrid.ActualHeight);
	}

	private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
		this.UpdateRepositoryPaneMaxHeight(e.NewSize.Height);
	}

	/// <summary>
	/// ウィンドウ縮小時にソート・フィルター領域が最小高さを下回らないよう、上段リポジトリ領域の最大高さを制限します。
	/// </summary>
	/// <param name="containerHeight">ルートグリッドの現在の高さ。</param>
	private void UpdateRepositoryPaneMaxHeight(double containerHeight) {
		if (double.IsNaN(containerHeight) || containerHeight <= 0) {
			return;
		}
		var availableHeight = containerHeight - this.RootGrid.Padding.Top - this.RootGrid.Padding.Bottom;
		var splitterHeight = this.RepositoryRowSplitter.ActualHeight > 0 ? this.RepositoryRowSplitter.ActualHeight : 4.0;
		var minRepositoryHeight = this.RepositoryPaneRow.MinHeight;
		var minSortFilterHeight = this.SortFilterPaneRow.MinHeight;
		this.RepositoryPaneRow.MaxHeight = Math.Max(minRepositoryHeight, availableHeight - splitterHeight - minSortFilterHeight);
	}
}

/// <summary>
/// TabContentPane の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class TabContentPaneUserControl : UserControlBase<TabContentViewModel>;