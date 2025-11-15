using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model.Objects;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

[AddSingleton]
[GenerateR3JsonConfigDefaultDto]
public class ScanConfigModel {
	public IServiceProvider ScopedServiceProvider {
		get;
	}

	public ScanConfigModel(IServiceProvider serviceProvider) {
		this.ScopedServiceProvider = serviceProvider;
		(string, MediaType)[] extensions = [
			(".jpg", MediaType.Image),
			(".jpeg", MediaType.Image),
			(".png", MediaType.Image),
			(".gif", MediaType.Image),
			(".bmp", MediaType.Image),
			(".tiff", MediaType.Image),
			(".tif", MediaType.Image),
			(".heif", MediaType.Image),
			(".heic", MediaType.Image),
			(".avi", MediaType.Video),
			(".mp4", MediaType.Video),
			(".m4a", MediaType.Video),
			(".mov", MediaType.Video),
			(".qt", MediaType.Video),
			(".m2ts", MediaType.Video),
			(".ts", MediaType.Video),
			(".mpeg", MediaType.Video),
			(".mpg", MediaType.Video),
			(".mkv", MediaType.Video),
			(".wmv", MediaType.Video),
			(".asf", MediaType.Video),
			(".flv", MediaType.Video),
			(".f4v", MediaType.Video),
			(".wmv", MediaType.Video),
			(".webm", MediaType.Video),
			(".ogm", MediaType.Video),
			(".pdf", MediaType.Pdf),
			(".zip", MediaType.Archive)
		];
		this.TargetExtensions = [.. extensions.Select(x => {
			var model = this.ScopedServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ExtensionObjectModel>();
			model.Extension.Value = x.Item1;
			model.MediaType.Value = x.Item2;
			return model;
		})];
	}

	public void AddTargetExtension() {
		var scope = this.ScopedServiceProvider.CreateScope();
		var config= scope.ServiceProvider.GetRequiredService<ExtensionObjectModel>();
		this.TargetExtensions.Add(config);
	}

	public void RemoveTargetExtension(ExtensionObjectModel config) {
		this.TargetExtensions.Remove(config);
	}

	/// <summary>
	/// 対象拡張子
	/// </summary>
	public ObservableList<ExtensionObjectModel> TargetExtensions {
		get;
	}
}
