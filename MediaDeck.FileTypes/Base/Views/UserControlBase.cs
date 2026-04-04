using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.FileTypes.Base.Views;

internal abstract class UserControlBase<T> : UserControl where T : class {
	internal T? ViewModel {
		get;
		set;
	}

	protected UserControlBase() {
		this.DataContextChanged += (s, e) => {
			var old = this.ViewModel;
			this.ViewModel = this.DataContext as T;
			this.OnViewModelChanged(old, this.ViewModel);
		};
	}

	protected virtual void OnViewModelChanged(T? oldViewModel, T? newViewModel) {
	}
}