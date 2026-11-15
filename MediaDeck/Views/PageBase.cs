using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MediaDeck.Views;

/// <summary>
/// 指定された型の ViewModel を持つ Page の基底クラス。
/// </summary>
/// <typeparam name="T">ViewModel の型。</typeparam>
public abstract class PageBase<T> : Page, INotifyPropertyChanged where T : class {
	private T? _viewModel;

	/// <summary>
	/// プロパティ変更イベント。
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// ViewModel を取得または設定します。
	/// </summary>
	public T? ViewModel {
		get {
			return this._viewModel;
		}

		set {
			if (!ReferenceEquals(this._viewModel, value)) {
				var old = this._viewModel;
				this._viewModel = value;

				// DataContext を同期する
				if (!ReferenceEquals(this.DataContext, value)) {
					this.DataContext = value;
				}

				this.RaisePropertyChanged(nameof(this.ViewModel));
				this.OnViewModelChanged(old, value);
			}
		}
	}

	/// <summary>
	/// <see cref="PageBase{T}"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	protected PageBase() {
		this.DataContextChanged += (_, _) => {
			if (this.DataContext is T vm && !ReferenceEquals(this.ViewModel, vm)) {
				this.ViewModel = vm;
			}
		};
	}

	/// <summary>
	/// ページへナビゲートされたときの処理。NavigationEventArgs の Parameter から ViewModel を設定します。
	/// </summary>
	/// <param name="e">イベント引数。</param>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		base.OnNavigatedTo(e);
		if (e.Parameter is T vm) {
			this.ViewModel = vm;
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

	/// <summary>
	/// 派生クラスから任意のプロパティ変更通知を発火します。
	/// </summary>
	/// <param name="propertyName">変更されたプロパティ名。</param>
	protected void RaisePropertyChanged(string propertyName) {
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}