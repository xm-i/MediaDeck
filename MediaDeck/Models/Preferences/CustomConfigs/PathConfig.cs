using System.IO;
using MediaDeck.Utils.Constants;

namespace MediaDeck.Models.Preferences.CustomConfigs;

[AddSingleton]
public class PathConfig : SettingsBase {
	/// <summary>
	/// サムネイルフォルダパス
	/// </summary>

	public SettingsItem<string> ThumbnailFolderPath {
		get;
	} = new(Path.Combine(FilePathConstants.BaseDirectory, "thumbs"));

	/// <summary>
	/// 一時フォルダパス
	/// </summary>

	public SettingsItem<string> TemporaryFolderPath {
		get;
	} = new(Path.Combine(FilePathConstants.BaseDirectory, "temp"));

	/// <summary>
	/// FFMpegフォルダパス
	/// </summary>

	public SettingsItem<string> FFMpegFolderPath {
		get;
	} = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "Assets"));
}
