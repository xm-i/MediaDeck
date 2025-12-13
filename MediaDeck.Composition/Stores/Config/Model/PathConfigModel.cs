using System.IO;

using MediaDeck.Composition.Constants;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDefaultDto]
public class PathConfigModel {
	/// <summary>
	/// サムネイルフォルダパス
	/// </summary>

	public ReactiveProperty<string> ThumbnailFolderPath {
		get;
	} = new(Path.Combine(FilePathConstants.BaseDirectory, "thumbs"));

	/// <summary>
	/// 一時フォルダパス
	/// </summary>

	public ReactiveProperty<string> TemporaryFolderPath {
		get;
	} = new(Path.Combine(FilePathConstants.BaseDirectory, "temp"));

	/// <summary>
	/// FFMpegフォルダパス
	/// </summary>

	public ReactiveProperty<string> FFMpegFolderPath {
		get;
	} = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "Assets"));
}
