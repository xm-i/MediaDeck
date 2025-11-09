using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MediaDeck.Database;
using MediaDeck.Models.Files;
using MediaDeck.Models.Files.SearchConditions;
using MediaDeck.Models.NotificationDispatcher;
using MediaDeck.Models.Preferences;
using MediaDeck.Models.Repositories.Objects;
using MediaDeck.Utils.Constants;
using MediaDeck.Utils.Notifications;
using MediaDeck.Utils.Objects;

namespace MediaDeck.Models.Repositories;

[AddTransient]
public class FolderRepository : RepositoryBase {
	private readonly States _states;
	public FolderRepository(MediaDeckDbContext dbContext, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher,States states) {
		this._db = dbContext;
		this._searchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
		this._states = states;
		FileNotifications
			.FileRegistered
			.ThrottleLast(TimeSpan.FromSeconds(10))
			.Subscribe(async _ => await this.Load());
	}

	private readonly MediaDeckDbContext _db;
	private readonly SearchConditionNotificationDispatcher _searchConditionNotificationDispatcher;
	public ReactiveProperty<FolderObject> RootFolder {
		get;
	} = new();

	public string[] _currentDirectoryPathList = [];

	public override async Task Load() {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		var list = (await this._db
			.MediaFiles
			.GroupBy(x => x.DirectoryPath)
			.Select(x => new ValueCountPair<string>(x.Key, x.Count()))
			.ToListAsync())
			.OrderBy(x => x.Value)
			.ToList();

		var directoryList = list.Select(x => x.Value).ToArray();
		if (directoryList.SequenceEqual(this._currentDirectoryPathList)) {
			this.RootFolder.Value ??= new FolderObject(null, "", []);
			return;
		}
		this._currentDirectoryPathList = directoryList;

		var all = list.Select(x => (x.Value, x.Count, Split: x.Value.Split(Path.DirectorySeparatorChar))).ToArray();
		var maxPathDepth = all.Length == 0 ? 0 : all.Max(x => x.Split.Length);
		
		// ルート追加
		list.Add(new ("",list.Sum(x => x.Count)));
		// 足りない部分を補足
		for (var depth = 1; depth <= maxPathDepth; depth++) { // 最大の深さまで
			var target = all.Where(x => x.Split.Length > depth).ToArray(); // 深さが足りないものを除外
			list.AddRange(target
				.Select(x =>
					(
						item: x,
						joined: string.Join(Path.DirectorySeparatorChar, x.Split[0..depth]), // 対象の深さまでのパス
						joinedPlus: string.Join(Path.DirectorySeparatorChar, x.Split[0..(depth + 1)]) // 対象の深さ+1までのパス
					)
				)
				.Where(x => !all.Any(y => y.Value == x.joined)) // 対象のパスがすでに含まれていたら除外
				.GroupBy(x => x.joined) // 対象の深さまでのパスでグループ化
				.Where(x => x.DistinctBy(x => x.joinedPlus).Count() >= 2) // 対象の深さから、2つ以上のフォルダに分岐している
				.Select(x => new ValueCountPair<string>(x.Key, 0)));
		}

		this.RootFolder.Value = new FolderObject(null, "", [.. list.OrderBy(x => x.Value)]);

		this.Restore();
	}

	public void SetRepositoryCandidate(FolderObject folderObject,bool includeSubDirectory) {
		this._searchConditionNotificationDispatcher.UpdateRequest.OnNext(x => {
			x.RemoveRange(x.Where(x => x is FolderSearchCondition));
			x.Add(new FolderSearchCondition(folderObject) {
				 IncludeSubDirectories = includeSubDirectory
			 });
		});
	}

	public IEnumerable<FolderObject> GetAllFolders() {
		this.Load().Wait();
		IEnumerable<FolderObject> func(FolderObject fo) => fo.ChildFolders.SelectMany(x => func(x)).Concat([fo]).OrderBy(x => x.FolderPath);
		return func(this.RootFolder.Value);
	}

	private void Restore() {
		var condition = this._states.SearchStates.SearchCondition.FirstOrDefault(x => x is FolderSearchCondition) as FolderSearchCondition;
		if(condition == null) {
			return;
		}
		var parent = this.RootFolder.Value;
		while (true) {
			var previousParent = parent;
			foreach (var child in parent.ChildFolders) {
				if (condition.FolderPath.StartsWith(child.FolderPath)) {
					child.IsExpanded = true;
					parent = child;
					break;
				}
			}
			if (previousParent == parent) {
				break;
			}
		}
	}
}
