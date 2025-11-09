using System.Windows.Controls;

using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.Views.Panes.ViewerPanes.Controls;

public class FileThumbnailGrid: Grid {
	public BaseFileViewModel? FileViewModel {
		get;
		set;
	}
}
