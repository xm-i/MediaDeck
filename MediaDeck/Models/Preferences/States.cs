using System.Collections.Generic;
using System.IO;
using System.Xaml;

using MediaDeck.Composition.Constants;
using MediaDeck.Models.Preferences.CustomStates;

namespace MediaDeck.Models.Preferences;
/// <summary>
/// 状態
/// </summary>
[AddSingleton]
public class States {
	private readonly SettingsBase[] _states;

	/// <summary>
	/// 検索の状態
	/// </summary>
	public SearchStates SearchStates {
		get;
		set;
	}

	/// <summary>
	/// フォルダ管理状態
	/// </summary>
	public FolderManagerStates FolderManagerStates {
		get;
		set;
	}

	[Obsolete("for serialize")]
	public States() {
		this.SearchStates = null!;
		this.FolderManagerStates = null!;
		this._states = null!;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	public States(SearchStates searchStates,FolderManagerStates folderManagerStates) {
		this.SearchStates = searchStates;
		this.FolderManagerStates = folderManagerStates;
		this._states = [this.SearchStates,this.FolderManagerStates];
		this.Load();
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		using var ms = new MemoryStream();
		var d = this._states.ToDictionary(x => x.GetType(), x => x.Export());
		XamlServices.Save(ms, d);
		using var fs = File.Create(FilePathConstants.StateFilePath);
		ms.WriteTo(fs);
	}

	/// <summary>
	/// ロード
	/// </summary>
	public void Load() {
		this.LoadDefault();

		if (XamlServices.Load(FilePathConstants.StateFilePath) is not Dictionary<Type, Dictionary<string, dynamic>> states) {
			return;
		}
		foreach (var s in this._states) {
			try {
				s.Import(states[s.GetType()]);
			} catch (Exception) {
				s.LoadDefault();
			}
		}
	}

	/// <summary>
	/// デフォルトロード
	/// </summary>
	private void LoadDefault() {
		foreach (var s in this._states) {
			s.LoadDefault();
		}
	}
}
