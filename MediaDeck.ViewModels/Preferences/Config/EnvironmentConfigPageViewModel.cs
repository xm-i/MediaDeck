using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class EnvironmentConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
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
	} = "\uF4C4";

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	}

	/// <summary>
	/// テーマ選択肢
	/// </summary>
	public ThemeOption[] ThemeOptions {
		get;
	}

	/// <summary>
	/// バックドロップ選択肢
	/// </summary>
	public SystemBackdropOption[] SystemBackdropOptions {
		get;
	}

	/// <summary>
	/// 選択中のテーマ
	/// </summary>
	public BindableReactiveProperty<ThemeOption> SelectedTheme {
		get;
	}

	/// <summary>
	/// 選択中のバックドロップ
	/// </summary>
	public BindableReactiveProperty<SystemBackdropOption> SelectedSystemBackdrop {
		get;
	}

	/// <summary>
	/// アプリケーションデータフォルダのパス
	/// </summary>
	public string AppDataDirectoryPath {
		get;
	}

	/// <summary>
	/// アプリケーションデータフォルダをエクスプローラーで開くコマンド
	/// </summary>
	public ReactiveCommand OpenAppDataDirectoryCommand {
		get;
	} = new();

	public EnvironmentConfigPageViewModel(EnvironmentConfigModel environmentConfig, IStringProvider stringProvider, IAppPathProvider appPathProvider) {
		this.PageName = stringProvider.GetString("Config_Environment_Name");
		this.PageDescription = stringProvider.GetString("Config_Environment_Description");

		this.ThemeOptions = [
			new ThemeOption { Theme = AppTheme.Default, DisplayName = stringProvider.GetString("Config_Environment_Theme_Default") },
			new ThemeOption { Theme = AppTheme.Light, DisplayName = stringProvider.GetString("Config_Environment_Theme_Light") },
			new ThemeOption { Theme = AppTheme.Dark, DisplayName = stringProvider.GetString("Config_Environment_Theme_Dark") },
		];

		this.SystemBackdropOptions = [
			new SystemBackdropOption { Backdrop = AppSystemBackdrop.Mica, DisplayName = stringProvider.GetString("Config_Environment_SystemBackdrop_Mica") },
			new SystemBackdropOption { Backdrop = AppSystemBackdrop.Acrylic, DisplayName = stringProvider.GetString("Config_Environment_SystemBackdrop_Acrylic") },
		];

		var currentTheme = environmentConfig.Theme.Value;
		var initialTheme = Array.Find(this.ThemeOptions, x => x.Theme == currentTheme)
						  ?? this.ThemeOptions[0];

		this.SelectedTheme = new BindableReactiveProperty<ThemeOption>(initialTheme).AddTo(this.CompositeDisposable);

		var currentSystemBackdrop = environmentConfig.SystemBackdrop.Value;
		var initialSystemBackdrop = Array.Find(this.SystemBackdropOptions, x => x.Backdrop == currentSystemBackdrop)
								 ?? this.SystemBackdropOptions[0];

		this.SelectedSystemBackdrop = new BindableReactiveProperty<SystemBackdropOption>(initialSystemBackdrop).AddTo(this.CompositeDisposable);
		this.AppDataDirectoryPath = appPathProvider.BaseDirectory;

		// ViewModel の選択変更を Model に反映
		this.SelectedTheme
			.Subscribe(opt => environmentConfig.Theme.Value = opt?.Theme ?? AppTheme.Default)
			.AddTo(this.CompositeDisposable);

		this.SelectedSystemBackdrop
			.Subscribe(opt => environmentConfig.SystemBackdrop.Value = opt?.Backdrop ?? AppSystemBackdrop.None)
			.AddTo(this.CompositeDisposable);

		this.OpenAppDataDirectoryCommand
			.Subscribe(_ => ShellUtility.ShellExecute(this.AppDataDirectoryPath))
			.AddTo(this.CompositeDisposable);
	}
}

/// <summary>
/// テーマの選択肢
/// </summary>
public sealed class ThemeOption {
	public AppTheme Theme {
		get;
		init;
	}

	public string DisplayName {
		get;
		init;
	} = string.Empty;

	public override string ToString() {
		return this.DisplayName;
	}
}

/// <summary>
/// システムバックドロップの選択肢
/// </summary>
public sealed class SystemBackdropOption {
	public AppSystemBackdrop Backdrop {
		get;
		init;
	}

	public string DisplayName {
		get;
		init;
	} = string.Empty;

	public override string ToString() {
		return this.DisplayName;
	}
}