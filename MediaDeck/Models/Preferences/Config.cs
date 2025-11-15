using System.Collections.Generic;
using System.IO;
using System.Xaml;

using MediaDeck.Models.Preferences.CustomConfigs;

namespace MediaDeck.Models.Preferences;

[AddSingleton]
public class Config {
	private string? _configFilePath;
	private readonly SettingsBase[] _configs;

	/// <summary>
	/// パス設定
	/// </summary>
	public PathConfig PathConfig {
		get;
		set;
	}

	/// <summary>
	/// スキャン設定
	/// </summary>
	public ScanConfig ScanConfig {
		get;
		set;
	}

	/// <summary>
	/// 実行設定
	/// </summary>
	public ExecutionConfig ExecutionConfig {
		get;
		set;
	}

	[Obsolete("for serialize")]
	public Config() {
		this.ScanConfig = null!;
		this.PathConfig = null!;
		this.ExecutionConfig = null!;
		this._configs = null!;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	public Config(PathConfig pathConfig,ScanConfig scanConfig, ExecutionConfig executionConfig) {
		this.ScanConfig = scanConfig;
		this.PathConfig = pathConfig;
		this.ExecutionConfig = executionConfig;
		this._configs = [
			scanConfig,
			pathConfig,
			executionConfig
		];
	}

	/// <summary>
	/// ファイルパス設定
	/// </summary>
	/// <param name="path">パス</param>
	public void SetFilePath(string path) {
		this._configFilePath = path;
	}

	/// <summary>
	/// 保存
	/// </summary>
	public void Save() {
		if (this._configFilePath is null) {
			throw new InvalidOperationException();
		}
		using var ms = new MemoryStream();
		var d = this._configs.ToDictionary(x => x.GetType(), x => x.Export());
		XamlServices.Save(ms, d);
		using var fs = File.Create(this._configFilePath);
		ms.WriteTo(fs);
	}

	/// <summary>
	/// 設定ロード
	/// </summary>
	public void Load() {
		if (this._configFilePath is null) {
			throw new InvalidOperationException();
		}
		this.LoadDefault();
		if (!File.Exists(this._configFilePath)) {
			return;
		}

		if (XamlServices.Load(this._configFilePath) is not Dictionary<Type, Dictionary<string, dynamic>> config) {
			return;
		}

		foreach (var s in this._configs) {
			if (config.TryGetValue(s.GetType(), out var d)) {
				s.Import(d);
			}
		}
	}

	/// <summary>
	/// デフォルト設定ロード
	/// </summary>
	private void LoadDefault() {
		foreach (var c in this._configs) {
			c.LoadDefault();
		}
	}

	/// <summary>
	/// 破棄
	/// </summary>
	public void Dispose() {
		foreach (var c in this._configs) {
			c.Dispose();
		}
	}
}
