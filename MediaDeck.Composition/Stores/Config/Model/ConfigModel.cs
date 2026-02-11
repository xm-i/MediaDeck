using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;


[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class ConfigModel(PathConfigModel pathConfigModel, ScanConfigModel scanConfigModel, ExecutionConfigModel executionConfigModel) {

	/// <summary>
	/// パス設定
	/// </summary>
	public PathConfigModel PathConfig {
		get;
		set;
	} = pathConfigModel;

	/// <summary>
	/// スキャン設定
	/// </summary>
	public ScanConfigModel ScanConfig {
		get;
		set;
	} = scanConfigModel;

	/// <summary>
	/// 実行設定
	/// </summary>
	public ExecutionConfigModel ExecutionConfig {
		get;
		set;
	} = executionConfigModel;
}
