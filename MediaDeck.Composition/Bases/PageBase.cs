using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Composition.Bases;
public abstract class PageBase<T>:Page where T:class {
	public T? ViewModel {
		get; set;
	}

	protected PageBase() {
		this.DataContextChanged += (s, e) => {
			this.ViewModel = this.DataContext as T;
		};
	}
}
