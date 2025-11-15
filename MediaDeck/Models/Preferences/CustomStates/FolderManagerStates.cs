using System;
using System.Collections.Generic;
using System.Text;
using MediaDeck.Models.Files.SearchConditions;
using MediaDeck.Models.FolderManager;

namespace MediaDeck.Models.Preferences.CustomStates; 
/// <summary>
/// フォルダ管理状態
/// </summary>

[AddSingleton]
public class FolderManagerStates : SettingsBase {
	/// <summary>
	/// 管理対象フォルダリスト
	/// </summary>
	public SettingsCollection<FolderModel> Folders {
		get;
	} = new SettingsCollection<FolderModel>([]);
}
