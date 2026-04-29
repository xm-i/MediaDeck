using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Resources;

public sealed class MediaItemThumbnailControlTemplateSelector : DataTemplateSelector {
	public DataTemplate? Image {
		get;
		set;
	}

	public DataTemplate? Video {
		get;
		set;
	}

	public DataTemplate? Pdf {
		get;
		set;
	}

	public DataTemplate? Archive {
		get;
		set;
	}

	public DataTemplate? FolderGroup {
		get;
		set;
	}

	public DataTemplate? Unknown {
		get;
		set;
	}

	protected override DataTemplate SelectTemplateCore(object item) {
		return this.SelectTemplateCore(item, null!);
	}

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) {
		if (item is not IMediaItemViewModel vm) {
			return this.Unknown ?? new DataTemplate();
		}

		return vm.MediaType switch {
			MediaType.Image => this.Image,
			MediaType.Video => this.Video,
			MediaType.Pdf => this.Pdf,
			MediaType.Archive => this.Archive,
			MediaType.FolderGroup => this.FolderGroup,
			_ => this.Unknown,
		} ?? new DataTemplate();
	}
}