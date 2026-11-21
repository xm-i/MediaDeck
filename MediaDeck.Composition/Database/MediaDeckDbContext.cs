using MediaDeck.Composition.Tables;

using Microsoft.EntityFrameworkCore;

namespace MediaDeck.Composition.Database;

/// <summary>
/// コンストラクタ
/// </summary>
/// <param name="dbConnection"></param>
public class MediaDeckDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions) {
	/// <summary>
	/// メディアアイテムテーブル
	/// </summary>
	public DbSet<MediaItem> MediaItems {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテムテーブル
	/// </summary>
	public DbSet<ImageFile> ImageFiles {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテムテーブル
	/// </summary>
	public DbSet<VideoFile> VideoFiles {
		get;
		set;
	} = null!;

	/// <summary>
	/// 位置情報テーブル
	/// </summary>
	public DbSet<Position> Positions {
		get;
		set;
	} = null!;

	/// <summary>
	/// 位置情報(住所)テーブル
	/// </summary>
	public DbSet<PositionAddress> PositionAddresses {
		get;
		set;
	} = null!;

	/// <summary>
	/// 位置情報(別名)テーブル
	/// </summary>
	public DbSet<PositionNameDetail> PositionNameDetails {
		get;
		set;
	} = null!;

	/// <summary>
	/// タグテーブル
	/// </summary>
	public DbSet<Tag> Tags {
		get;
		set;
	} = null!;

	/// <summary>
	/// タグ別名テーブル
	/// </summary>
	public DbSet<TagAlias> TagAliases {
		get;
		set;
	} = null!;

	/// <summary>
	/// タグ分類テーブル
	/// </summary>
	public DbSet<TagCategory> TagCategories {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテム・タグ中間テーブル
	/// </summary>
	public DbSet<MediaItemTag> MediaItemTags {
		get;
		set;
	} = null!;

	/// <summary>
	/// アルバムテーブル
	/// </summary>
	public DbSet<Album> Albums {
		get;
		set;
	} = null!;

	/// <summary>
	/// メディアアイテム・アルバム中間テーブル
	/// </summary>
	public DbSet<MediaItemAlbum> MediaItemAlbums {
		get;
		set;
	} = null!;

	/// <summary>
	/// データベースバージョン
	/// </summary>
	public DbSet<DbVersion> DbVersions {
		get;
		set;
	} = null!;

	/// <summary>
	/// テーブル設定
	/// </summary>
	/// <param name="modelBuilder"></param>
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		// Primary Keys
		modelBuilder.Entity<MediaItem>().HasKey(mf => mf.MediaItemId);
		modelBuilder.Entity<ImageFile>().HasKey(i => i.MediaItemId);
		modelBuilder.Entity<VideoFile>().HasKey(v => v.MediaItemId);
		modelBuilder.Entity<Position>().HasKey(p => new { p.Latitude, p.Longitude });
		modelBuilder.Entity<PositionAddress>().HasKey(pa => new { pa.Latitude, pa.Longitude, pa.Type });
		modelBuilder.Entity<PositionNameDetail>().HasKey(pnd => new { pnd.Latitude, pnd.Longitude, pnd.Desc });
		modelBuilder.Entity<MediaItemTag>().HasKey(mft => new { mft.MediaItemId, mft.TagId });
		modelBuilder.Entity<Tag>().HasKey(t => t.TagId);
		modelBuilder.Entity<TagAlias>().HasKey(ta => new { ta.TagId, ta.TagAliasId });
		modelBuilder.Entity<TagCategory>().HasKey(tc => tc.TagCategoryId);
		modelBuilder.Entity<Album>().HasKey(a => a.AlbumId);
		modelBuilder.Entity<MediaItemAlbum>().HasKey(mia => new { mia.MediaItemId, mia.AlbumId });
		modelBuilder.Entity<DbVersion>().HasKey(x => x.Id);

		// Index
		modelBuilder.Entity<MediaItem>()
			.HasIndex(x => x.FilePath)
			.IsUnique();

		modelBuilder.Entity<MediaItem>()
			.HasIndex(x => x.FullHash);

		modelBuilder.Entity<MediaItem>()
			.HasIndex(x => x.DirectoryPath);

		modelBuilder.Entity<MediaItem>()
			.HasIndex(x => new { x.PreHash, x.IsExists });

		// Relation
		modelBuilder.Entity<ImageFile>()
			.HasOne(i => i.MediaItem)
			.WithOne(m => m.ImageFile)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<VideoFile>()
			.HasOne(v => v.MediaItem)
			.WithOne(m => m.VideoFile)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Position>()
			.HasMany(p => p.MediaItems)
			.WithOne(m => m.Position!)
			.HasForeignKey(p => new { p.Latitude, p.Longitude })
			.OnDelete(DeleteBehavior.ClientSetNull);

		modelBuilder.Entity<PositionAddress>()
			.HasOne(pa => pa.Position)
			.WithMany(p => p.Addresses)
			.HasForeignKey(p => new { p.Latitude, p.Longitude })
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<PositionNameDetail>()
			.HasOne(pnd => pnd.Position)
			.WithMany(p => p.NameDetails)
			.HasForeignKey(p => new { p.Latitude, p.Longitude })
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<MediaItemTag>()
			.HasOne(mft => mft.MediaItem)
			.WithMany(mf => mf.MediaItemTags)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<MediaItemTag>()
			.HasOne(mft => mft.Tag)
			.WithMany(t => t.MediaItemTags)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TagAlias>()
			.HasOne(t => t.Tag)
			.WithMany(t => t.TagAliases)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TagCategory>()
			.HasMany(tc => tc.Tags)
			.WithOne(t => t.TagCategory!)
			.HasForeignKey(t => t.TagCategoryId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<MediaItem>()
			.ComplexProperty(m => m.Metadata, b => {
				b.ToJson();
				b.ComplexCollection(m => m.Entries);
			});

		// MediaItemAdditionalInfo は現在プロパティを持たないため、ComplexProperty にするとモデル検証エラーとなる。
		// 単一 Owned Entity は警告対象外のため OwnsOne(...).ToJson() を維持する。
		modelBuilder.Entity<MediaItem>()
			.OwnsOne(m => m.AdditionalInfo, b => {
				b.ToJson();
			});

		modelBuilder.Entity<Album>()
			.HasIndex(a => a.Path)
			.IsUnique();

		modelBuilder.Entity<MediaItemAlbum>()
			.HasOne(mia => mia.MediaItem)
			.WithMany(m => m.MediaItemAlbums)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<MediaItemAlbum>()
			.HasOne(mia => mia.Album)
			.WithMany(a => a.MediaItemAlbums)
			.OnDelete(DeleteBehavior.Cascade);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
		configurationBuilder.Properties<string>().UseCollation("NOCASE");
		base.ConfigureConventions(configurationBuilder);
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder);

		//		optionsBuilder.LogTo(x => Debug.WriteLine(x), LogLevel.Information,DbContextLoggerOptions.SingleLine);
	}
}