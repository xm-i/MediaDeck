using System.ComponentModel;

using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.MediaItemTypes.UI.Base.Views;

public abstract class UserControlBase<T> : UserControl, INotifyPropertyChanged where T : class {
	public event PropertyChangedEventHandler? PropertyChanged;
	public T? ViewModel {
		get;
		set;
	}

	protected UserControlBase() {
		this.DataContextChanged += (s, e) => {
			var old = this.ViewModel;
			this.ViewModel = this.DataContext as T;
			if (old == this.ViewModel) {
				return;
			}
			this.OnViewModelChanged(old, this.ViewModel);
		};
	}

	/// <summary>
	/// ViewModel が変更されたときに呼び出されます。派生クラスでオーバーライドして処理を追加します。
	/// </summary>
	/// <param name="oldViewModel">変更前の ViewModel。</param>
	/// <param name="newViewModel">変更後の ViewModel。</param>
	protected virtual void OnViewModelChanged(T? oldViewModel, T? newViewModel) {
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ViewModel)));
	}

	/// <summary>
	/// 派生クラスから任意のプロパティ変更通知を発火します。
	/// </summary>
	protected void RaisePropertyChanged(string propertyName) {
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}