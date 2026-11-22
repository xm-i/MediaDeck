using System.Windows.Input;

using MediaDeck.ViewModels.Filters.FilterItemCreators;

using Microsoft.UI.Xaml.Input;

using Windows.System;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// ファイルパスフィルター入力用コントロール。
/// </summary>
public partial class FilePathFilter {
	/// <summary>
	/// <see cref="FilePathFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FilePathFilter() {
		this.InitializeComponent();
	}

	private void FilePathTextBox_KeyDown(object sender, KeyRoutedEventArgs e) {
		if (e.Key == VirtualKey.Enter &&
			this.ViewModel?.AddFilterCommand is ICommand command &&
			command.CanExecute(null)) {
			command.Execute(null);
			e.Handled = true;
		}
	}
}

/// <summary>
/// <see cref="FilePathFilter"/> の基底クラス。
/// </summary>
public class FilePathFilterControlBase : UserControlBase<FilePathFilterCreatorViewModel>;