using MediaDeck.Common.Base;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// ListViewer の列定義。表示順は ListViewerViewModel.AllColumns の並び順で決定される。
/// </summary>
public sealed class ListViewerColumn : ViewModelBase {
	public ListViewerColumn(string id, string header, double width, double minWidth = 40, bool isVisible = true, bool canHide = true) {
		this.Id = id;
		this.Header = header;
		this.MinWidth = minWidth;
		this.CanHide = canHide;
		this.Width = new BindableReactiveProperty<double>(width);
		this.IsVisible = new BindableReactiveProperty<bool>(isVisible);
		this.EffectiveWidth = new BindableReactiveProperty<double>(isVisible ? width : 0);
		this.EffectiveMinWidth = new BindableReactiveProperty<double>(isVisible ? minWidth : 0);

		this.Width
			.Subscribe(x => {
				if (!this.IsVisible.Value) {
					return;
				}
				this.EffectiveWidth.Value = Math.Max(this.MinWidth, x);
			})
			.AddTo(this.CompositeDisposable);

		this.IsVisible
			.Subscribe(visible => {
				if (visible) {
					this.EffectiveMinWidth.Value = this.MinWidth;
					this.EffectiveWidth.Value = Math.Max(this.MinWidth, this.Width.Value);
				} else {
					this.EffectiveMinWidth.Value = 0;
					this.EffectiveWidth.Value = 0;
				}
			})
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 列を一意に識別するID。 View 側で対応する DataTemplate を解決するキーに使用される。
	/// </summary>
	public string Id {
		get;
	}

	/// <summary>
	/// ヘッダー表示名。
	/// </summary>
	public string Header {
		get;
	}

	/// <summary>
	/// 列幅(px)。ユーザーリサイズで更新される。
	/// </summary>
	public BindableReactiveProperty<double> Width {
		get;
	}

	/// <summary>
	/// 最小幅。
	/// </summary>
	public double MinWidth {
		get;
	}

	/// <summary>
	/// 表示状態を反映した実際の幅。非表示時は0。
	/// </summary>
	public BindableReactiveProperty<double> EffectiveWidth {
		get;
	}

	/// <summary>
	/// 表示状態を反映した実際の最小幅。非表示時は0。
	/// </summary>
	public BindableReactiveProperty<double> EffectiveMinWidth {
		get;
	}

	/// <summary>
	/// 表示有無。
	/// </summary>
	public BindableReactiveProperty<bool> IsVisible {
		get;
	}

	/// <summary>
	/// 非表示にできる列か。サムネイルなど常時表示したい列は false を指定する。
	/// </summary>
	public bool CanHide {
		get;
	}
}