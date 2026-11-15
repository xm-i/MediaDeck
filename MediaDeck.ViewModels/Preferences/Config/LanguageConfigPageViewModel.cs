using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class LanguageConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	}

	/// <summary>
	/// ページのアイコン（Segoe Fluent Icons のグリフ文字）
	/// </summary>
	public string PageIconGlyph {
		get;
	} = "\uF2B7";

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	}

	/// <summary>
	/// 言語選択肢 (Tag = BCP-47 タグ、空文字でシステム既定)
	/// </summary>
	public LanguageOption[] LanguageOptions {
		get;
	}

	/// <summary>
	/// 選択中の言語オプション
	/// </summary>
	public BindableReactiveProperty<LanguageOption> SelectedLanguage {
		get;
	}

	public LanguageConfigPageViewModel(LanguageConfigModel languageConfig, IStringProvider stringProvider) {
		this.PageName = stringProvider.GetString("Config_Language_Name");
		this.PageDescription = stringProvider.GetString("Config_Language_Description");

		this.LanguageOptions = [
			new LanguageOption { Tag = string.Empty, DisplayName = stringProvider.GetString("Config_Language_SystemDefault") },
			new LanguageOption { Tag = "ja-JP", DisplayName = "日本語 (ja-JP)" },
			new LanguageOption { Tag = "en-US", DisplayName = "English (en-US)" },
		];

		var currentTag = languageConfig.Language.Value;
		var initial = Array.Find(this.LanguageOptions, x => x.Tag == currentTag)
					  ?? this.LanguageOptions[0];

		this.SelectedLanguage = new BindableReactiveProperty<LanguageOption>(initial).AddTo(this.CompositeDisposable);

		// ViewModel の選択変更を Model に反映
		this.SelectedLanguage
			.Subscribe(opt => languageConfig.Language.Value = opt?.Tag ?? string.Empty)
			.AddTo(this.CompositeDisposable);
	}
}

/// <summary>
/// 言語の選択肢
/// </summary>
public sealed class LanguageOption {
	public string Tag {
		get;
		init;
	} = string.Empty;

	public string DisplayName {
		get;
		init;
	} = string.Empty;

	public override string ToString() {
		return this.DisplayName;
	}
}