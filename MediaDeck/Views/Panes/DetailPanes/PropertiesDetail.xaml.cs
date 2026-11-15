using MediaDeck.ViewModels.Panes.DetailPanes;

namespace MediaDeck.Views.Panes.DetailPanes;

/// <summary>
/// プロパティおよびメタデータ詳細表示を行うビュー。
/// </summary>
public sealed partial class PropertiesDetail {
	/// <summary>
	/// <see cref="PropertiesDetail"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public PropertiesDetail() {
		this.InitializeComponent();
	}

	private void RatingControl_ValueChanged(Microsoft.UI.Xaml.Controls.RatingControl sender, object args) {
		if (this.ViewModel is not { } vm) {
			return;
		}

		vm.UpdateRateCommand.Execute(sender.Value);
	}
}

/// <summary>
/// PropertiesDetail の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class PropertiesDetailUserControl : UserControlBase<PropertiesDetailViewModel>;