using System.Collections.Generic;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

/// <summary>
/// ウィンドウを閉じる際にタブを別ウィンドウへマージするか確認するダイアログ。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class CloseWindowDialog : ContentDialog {
	private readonly CompositeDisposable _disposable = new();
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// 選択された移動先ウィンドウIDを取得します。
	/// </summary>
	public Guid? SelectedTargetWindowId {
		get {
			if (this.TargetWindowComboBox.SelectedValue is Guid id) {
				return id;
			}
			return null;
		}
	}

	/// <summary>
	/// <see cref="CloseWindowDialog"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列ローカライズプロバイダー</param>
	public CloseWindowDialog(IConfigStore configStore, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.InitializeComponent();

		ThemeHelper.BindTheme(this, configStore, this._disposable);
		this.Closed += (_, _) => this._disposable.Dispose();
	}

	/// <summary>
	/// ダイアログを初期化します。
	/// </summary>
	/// <param name="tabCount">現在のウィンドウのタブ数</param>
	/// <param name="otherWindows">移動先候補となる他のウィンドウ情報 (WindowId, TabName, TabCount)</param>
	public void Initialize(int tabCount, IReadOnlyList<(Guid WindowId, string TabName, int TabCount)> otherWindows) {
		var format = this._stringProvider.GetString("CloseWindowDialog_MessageFormat");
		this.MessageTextBlock.Text = string.Format(format, tabCount);

		var items = otherWindows.Select(w => {
			string name;
			if (w.TabCount <= 1) {
				name = w.TabName;
			} else {
				var otherTabsFormat = this._stringProvider.GetString("TabViewModel_OtherTabsFormat");
				name = string.Format(otherTabsFormat, w.TabName, w.TabCount - 1);
			}
			return new TargetWindowItem(w.WindowId, name);
		}).ToList();

		this.TargetWindowComboBox.ItemsSource = items;
		if (items.Count > 0) {
			this.TargetWindowComboBox.SelectedIndex = 0;
		}

		// 他のウィンドウが2つ以上ある場合のみ移動先選択のコンボボックスを表示
		this.TargetWindowPanel.Visibility = otherWindows.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>
	/// 移動先ウィンドウ選択用のコンボボックス表示アイテム。
	/// </summary>
	/// <param name="WindowId">ウィンドウID</param>
	/// <param name="DisplayName">表示名</param>
	public record TargetWindowItem(Guid WindowId, string DisplayName);
}