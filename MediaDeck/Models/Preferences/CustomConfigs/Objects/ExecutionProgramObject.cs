using MediaDeck.Utils.Enums;

namespace MediaDeck.Models.Preferences.CustomConfigs.Objects; 
public class ExecutionProgramObject {
	public ExecutionProgramObject() {
	}
	public ExecutionProgramObject(string path, string args, MediaType mediaType) {
		this.Path.Value = path;
		this.Args.Value = args;
		this.MediaType.Value = mediaType;
	}
	public BindableReactiveProperty<string> Path {
		get;
		set;
	} = new();

	public BindableReactiveProperty<string> Args {
		get;
		set;
	} = new();

	public BindableReactiveProperty<MediaType> MediaType {
		get;
		set;
	} = new();

}
