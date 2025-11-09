using MediaDeck.Models.Preferences.CustomConfigs.Objects;
using MediaDeck.Utils.Enums;

namespace MediaDeck.Models.Preferences.CustomConfigs;

[AddSingleton]
public class ScanConfig : SettingsBase {
	/// <summary>
	/// 対象拡張子
	/// </summary>

	public SettingsCollection<ExtensionConfig> TargetExtensions {
		get;
	} = new SettingsCollection<ExtensionConfig>(
		new ExtensionConfig(".jpg", MediaType.Image),
		new ExtensionConfig(".jpeg", MediaType.Image),
		new ExtensionConfig(".png", MediaType.Image),
		new ExtensionConfig(".gif", MediaType.Image),
		new ExtensionConfig(".bmp", MediaType.Image),
		new ExtensionConfig(".tiff", MediaType.Image),
		new ExtensionConfig(".tif", MediaType.Image),
		new ExtensionConfig(".heif", MediaType.Image),
		new ExtensionConfig(".heic", MediaType.Image),
		new ExtensionConfig(".avi", MediaType.Video),
		new ExtensionConfig(".mp4", MediaType.Video),
		new ExtensionConfig(".m4a", MediaType.Video),
		new ExtensionConfig(".mov", MediaType.Video),
		new ExtensionConfig(".qt", MediaType.Video),
		new ExtensionConfig(".m2ts", MediaType.Video),
		new ExtensionConfig(".ts", MediaType.Video),
		new ExtensionConfig(".mpeg", MediaType.Video),
		new ExtensionConfig(".mpg", MediaType.Video),
		new ExtensionConfig(".mkv", MediaType.Video),
		new ExtensionConfig(".wmv", MediaType.Video),
		new ExtensionConfig(".asf", MediaType.Video),
		new ExtensionConfig(".flv", MediaType.Video),
		new ExtensionConfig(".f4v", MediaType.Video),
		new ExtensionConfig(".wmv", MediaType.Video),
		new ExtensionConfig(".webm", MediaType.Video),
		new ExtensionConfig(".ogm", MediaType.Video),
		new ExtensionConfig(".pdf", MediaType.Pdf),
		new ExtensionConfig(".zip", MediaType.Archive)) {
		MaybeEditMember = true
	};
}
