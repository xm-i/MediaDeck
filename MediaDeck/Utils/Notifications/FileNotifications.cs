using MediaDeck.Database.Tables;

namespace MediaDeck.Utils.Notifications;
public static class FileNotifications {
	public static Subject<MediaFile> FileRegistered {
		get;
	} = new();
}
