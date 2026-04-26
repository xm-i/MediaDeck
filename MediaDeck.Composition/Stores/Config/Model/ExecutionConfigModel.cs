using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class ExecutionConfigModel {
	private readonly IServiceProvider _serviceProvider;
	private IMediaItemTypeService _mediaItemTypeService {
		get {
			return field ??= this._serviceProvider.GetRequiredService<IMediaItemTypeService>();
		}
	}

	public ExecutionConfigModel(IServiceProvider serviceProvider) {
		this._serviceProvider = serviceProvider;
	}

	/// <summary>
	/// 実行プログラム
	/// </summary>
	public ObservableList<IExecutionProgramObjectModel> ExecutionPrograms {
		get;
	} = [];

	public void AddExecutionProgram(MediaType mediaType) {
		var model = this._mediaItemTypeService.CreateExecutionProgramObjectModel(mediaType);
		this.ExecutionPrograms.Add(model);
	}

	public void RemoveExecutionProgram(IExecutionProgramObjectModel program) {
		this.ExecutionPrograms.Remove(program);
	}
}