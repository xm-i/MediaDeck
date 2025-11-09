using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Composition.Bases;
public abstract class UserControlBase<T>:UserControl where T:class {
	public T? ViewModel {
		get; set;
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
