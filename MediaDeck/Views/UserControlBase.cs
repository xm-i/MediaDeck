using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

/// <summary>
/// 指定された型の ViewModel を持つ UserControl の基底クラス。
/// </summary>
/// <typeparam name="T">ViewModel の型。</typeparam>
public abstract class UserControlBase<T> : UserControl where T : class {
	/// <summary>
	/// ViewModel 依存関係プロパティを定義します。
	/// ジェネリッククラス内での定義により、型ごとに個別のプロパティが登録されます。
	/// </summary>
	public static readonly DependencyProperty ViewModelProperty =
		DependencyProperty.Register(
			nameof(ViewModel),
			typeof(T),
			typeof(UserControlBase<T>),
			new PropertyMetadata(null, OnViewModelPropertyChanged));

	/// <summary>
	/// ViewModel を取得または設定します。
	/// </summary>
	public T? ViewModel {
		get {
			return (T?)this.GetValue(ViewModelProperty);
		}

		set {
			this.SetValue(ViewModelProperty, value);
		}
	}

	private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
		if (d is UserControlBase<T> control) {
			var newValue = e.NewValue as T;

			// DataContext を同期する
			if (control.DataContext != newValue) {
				control.DataContext = newValue;
			}

			// 仮想メソッドを呼び出す
			control.OnViewModelChanged(e.OldValue as T, newValue);
		}
	}

	/// <summary>
	/// ViewModel が変更されたときに呼び出されます。派生クラスでオーバーライドして処理を追加します。
	/// </summary>
	/// <param name="oldViewModel">変更前の ViewModel。</param>
	/// <param name="newViewModel">変更後の ViewModel。</param>
	protected virtual void OnViewModelChanged(T? oldViewModel, T? newViewModel) {
		// 派生クラスでのオーバーライド用のフックメソッド
	}
}