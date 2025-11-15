using System.Collections.Generic;
using System.IO;

using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;

namespace MediaDeck.Models.Files.Metadata.Images.Formats;
/// <summary>
/// Jpegメタデータ取得クラス
/// </summary>
public class Jpeg : ImageBase {
	private readonly IReadOnlyList<MetadataExtractor.Directory> _reader;

	/// <summary>
	/// 幅
	/// </summary>
	public override int Width {
		get;
	}

	/// <summary>
	/// 高さ
	/// </summary>
	public override int Height {
		get;
	}

	/// <summary>
	/// 緯度
	/// </summary>
	public override Rational[]? Latitude {
		get;
	}

	/// <summary>
	/// 経度
	/// </summary>
	public override Rational[]? Longitude {
		get;
	}

	/// <summary>
	/// 高度
	/// </summary>
	public override Rational? Altitude {
		get;
	}

	/// <summary>
	/// 緯度方向(N/S)
	/// </summary>
	public override string? LatitudeRef {
		get;
	}

	/// <summary>
	/// 経度方向(E/W)
	/// </summary>
	public override string? LongitudeRef {
		get;
	}

	/// <summary>
	/// 高度方向(0/1)
	/// </summary>
	public override byte? AltitudeRef {
		get;
	}

	/// <summary>
	/// 画像の方向
	/// </summary>
	public override int? Orientation {
		get;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">画像ファイルストリーム</param>
	internal Jpeg(Stream stream) : base(stream) {
		this._reader = JpegMetadataReader.ReadMetadata(stream);
		var d = this._reader.First(x => x is JpegDirectory);
		var gps = this._reader.FirstOrDefault(x => x is GpsDirectory);
		var ifd0 = this._reader.FirstOrDefault(x => x is ExifDirectoryBase);
		this.Width = d.GetUInt16(JpegDirectory.TagImageWidth);
		this.Height = d.GetUInt16(JpegDirectory.TagImageHeight);

		if (ifd0 != null && ifd0.TryGetUInt16(ExifDirectoryBase.TagOrientation, out var orientation)) {
			this.Orientation = orientation;
		}

		if (gps != null) {
			this.Latitude = gps.GetRationalArray(GpsDirectory.TagLatitude);
			this.Longitude = gps.GetRationalArray(GpsDirectory.TagLongitude);
			this.LatitudeRef = gps.GetString(GpsDirectory.TagLatitudeRef);
			this.LongitudeRef = gps.GetString(GpsDirectory.TagLongitudeRef);
			if (gps.TryGetRational(GpsDirectory.TagAltitude, out var r)) {
				this.Altitude = r;
			}
			if (gps.TryGetByte(GpsDirectory.TagAltitudeRef, out var b)) {
				this.AltitudeRef = b;
			}
		}
	}

	public Database.Tables.Metadata.Jpeg CreateMetadataRecord() {
		var metadata = new Database.Tables.Metadata.Jpeg();

		var gps = this._reader.FirstOrDefault(x => x is GpsDirectory);
		var ifd0 = this._reader.FirstOrDefault(x => x is ExifDirectoryBase);
		var subIfd = this._reader.FirstOrDefault(x => x is ExifSubIfdDirectory);

		metadata.ImageDescription = this.GetString(ifd0, ExifDirectoryBase.TagImageDescription);
		metadata.ImageDescription = this.GetString(ifd0, ExifDirectoryBase.TagImageDescription);
		metadata.Make = this.GetString(ifd0, ExifDirectoryBase.TagMake);
		metadata.Model = this.GetString(ifd0, ExifDirectoryBase.TagModel);
		metadata.Orientation = this.GetShort(ifd0, ExifDirectoryBase.TagOrientation);
		metadata.XResolution = this.GetInt(ifd0, ExifDirectoryBase.TagXResolution);
		metadata.YResolution = this.GetInt(ifd0, ExifDirectoryBase.TagYResolution);
		metadata.ResolutionUnit = this.GetShort(ifd0, ExifDirectoryBase.TagResolutionUnit);
		metadata.TransferFunction = this.GetBinary(ifd0, ExifDirectoryBase.TagTransferFunction);
		metadata.Software = this.GetString(ifd0, ExifDirectoryBase.TagSoftware);
		metadata.DateTime = this.GetString(ifd0, ExifDirectoryBase.TagDateTime);
		metadata.Artist = this.GetString(ifd0, ExifDirectoryBase.TagArtist);
		metadata.WhitePoint = ifd0?.GetRationalArray(ExifDirectoryBase.TagWhitePoint)?.Select(x => x.ToByte()).ToArray();
		metadata.PrimaryChromaticities = this.GetBinary(ifd0, ExifDirectoryBase.TagPrimaryChromaticities);
		metadata.YCbCrCoefficients = this.GetBinary(ifd0, ExifDirectoryBase.TagYCbCrCoefficients);
		metadata.YCbCrPositioning = this.GetShort(ifd0, ExifDirectoryBase.TagYCbCrPositioning);
		metadata.ReferenceBlackWhite = this.GetBinary(ifd0, ExifDirectoryBase.TagReferenceBlackWhite);
		metadata.Copyright = this.GetString(ifd0, ExifDirectoryBase.TagCopyright);


		(metadata.ExposureTimeDenominator, metadata.ExposureTimeNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagExposureTime);
		(metadata.FNumberDenominator, metadata.FNumberNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagFNumber);
		metadata.ExposureProgram = this.GetShort(subIfd, ExifDirectoryBase.TagExposureProgram);
		metadata.SpectralSensitivity = this.GetString(subIfd, ExifDirectoryBase.TagSpectralSensitivity);
		metadata.OECF = this.GetBinary(subIfd, ExifDirectoryBase.TagOptoElectricConversionFunction);
		metadata.SensitivityType = this.GetShort(subIfd, ExifDirectoryBase.TagSensitivityType);
		metadata.StandardOutputSensitivity = this.GetInt(subIfd, ExifDirectoryBase.TagStandardOutputSensitivity);
		metadata.RecommendedExposureIndex = this.GetInt(subIfd, ExifDirectoryBase.TagRecommendedExposureIndex);
		metadata.ExifVersion = this.GetBinary(subIfd, ExifDirectoryBase.TagExifVersion);
		metadata.DateTimeOriginal = this.GetString(subIfd, ExifDirectoryBase.TagDateTimeOriginal);
		metadata.DateTimeDigitized = this.GetString(subIfd, ExifDirectoryBase.TagDateTimeDigitized);
		metadata.ComponentsConfiguration = this.GetBinary(subIfd, ExifDirectoryBase.TagComponentsConfiguration);
		(metadata.CompressedBitsPerPixelDenominator, metadata.CompressedBitsPerPixelNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagCompressedAverageBitsPerPixel);
		(metadata.ShutterSpeedValueDenominator, metadata.ShutterSpeedValueNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagShutterSpeed);
		(metadata.ApertureValueDenominator, metadata.ApertureValueNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagAperture);
		(metadata.BrightnessValueDenominator, metadata.BrightnessValueNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagBrightnessValue);
		(metadata.ExposureBiasValueDenominator, metadata.ExposureBiasValueNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagExposureBias);
		(metadata.MaxApertureValueDenominator, metadata.MaxApertureValueNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagMaxAperture);
		(metadata.SubjectDistanceDenominator, metadata.SubjectDistanceNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagSubjectDistance);
		metadata.MeteringMode = this.GetShort(subIfd, ExifDirectoryBase.TagMeteringMode);
		metadata.Flash = this.GetShort(subIfd, ExifDirectoryBase.TagFlash);
		(metadata.FocalLengthDenominator, metadata.FocalLengthNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagFocalLength);
		metadata.MakerNote = this.GetBinary(subIfd, ExifDirectoryBase.TagMakernote);
		metadata.UserComment = this.GetBinary(subIfd, ExifDirectoryBase.TagUserComment);
		metadata.SubSecTime = this.GetString(subIfd, ExifDirectoryBase.TagSubsecondTime);
		metadata.SubSecTimeOriginal = this.GetString(subIfd, ExifDirectoryBase.TagSubsecondTimeOriginal);
		metadata.SubSecTimeDigitized = this.GetString(subIfd, ExifDirectoryBase.TagSubsecondTimeDigitized);
		metadata.FlashpixVersion = this.GetBinary(subIfd, ExifDirectoryBase.TagFlashpixVersion);
		metadata.ColorSpace = this.GetShort(subIfd, ExifDirectoryBase.TagColorSpace);
		metadata.RelatedSoundFile = this.GetString(subIfd, ExifDirectoryBase.TagRelatedSoundFile);
		(metadata.FlashEnergyDenominator, metadata.FlashEnergyNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagFlashEnergy);
		(metadata.FocalPlaneXResolutionDenominator, metadata.FocalPlaneXResolutionNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagFocalPlaneXResolution);
		(metadata.FocalPlaneYResolutionDenominator, metadata.FocalPlaneYResolutionNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagFocalPlaneYResolution);
		metadata.FocalPlaneResolutionUnit = this.GetShort(subIfd, ExifDirectoryBase.TagFocalPlaneResolutionUnit);
		metadata.SubjectLocation = this.GetBinary(subIfd, ExifDirectoryBase.TagSubjectLocation);
		(metadata.ExposureIndexDenominator, metadata.ExposureIndexNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagExposureIndex);
		metadata.SensingMethod = this.GetShort(subIfd, ExifDirectoryBase.TagSensingMethod);
		metadata.FileSource = this.GetInt(subIfd, ExifDirectoryBase.TagFileSource);
		metadata.SceneType = this.GetInt(subIfd, ExifDirectoryBase.TagSceneType);
		metadata.CFAPattern = this.GetBinary(subIfd, ExifDirectoryBase.TagCfaPattern);
		metadata.CustomRendered = this.GetShort(subIfd, ExifDirectoryBase.TagCustomRendered);
		metadata.ExposureMode = this.GetShort(subIfd, ExifDirectoryBase.TagExposureMode);
		metadata.WhiteBalance = this.GetShort(subIfd, ExifDirectoryBase.TagWhiteBalanceMode);
		(metadata.DigitalZoomRatioDenominator, metadata.DigitalZoomRatioNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagDigitalZoomRatio);
		metadata.FocalLengthIn35mmFilm = this.GetShort(subIfd, ExifDirectoryBase.Tag35MMFilmEquivFocalLength);
		metadata.SceneCaptureType = this.GetShort(subIfd, ExifDirectoryBase.TagSceneCaptureType);
		metadata.GainControl = this.GetShort(subIfd, ExifDirectoryBase.TagGainControl);
		metadata.Contrast = this.GetShort(subIfd, ExifDirectoryBase.TagContrast);
		metadata.Saturation = this.GetShort(subIfd, ExifDirectoryBase.TagSaturation);
		metadata.Sharpness = this.GetShort(subIfd, ExifDirectoryBase.TagSharpness);
		metadata.DeviceSettingDescription = this.GetBinary(subIfd, ExifDirectoryBase.TagDeviceSettingDescription);
		metadata.SubjectDistanceRange = this.GetShort(subIfd, ExifDirectoryBase.TagSubjectDistanceRange);
		metadata.ImageUniqueID = this.GetString(subIfd, ExifDirectoryBase.TagImageUniqueId);
		metadata.CameraOwnerName = this.GetString(subIfd, ExifDirectoryBase.TagCameraOwnerName);
		metadata.BodySerialNumber = this.GetString(subIfd, ExifDirectoryBase.TagBodySerialNumber);
		metadata.LensSpecification = this.GetBinary(subIfd, ExifDirectoryBase.TagLensSpecification);
		metadata.LensMake = this.GetString(subIfd, ExifDirectoryBase.TagLensMake);
		metadata.LensModel = this.GetString(subIfd, ExifDirectoryBase.TagLensModel);
		metadata.LensSerialNumber = this.GetString(subIfd, ExifDirectoryBase.TagLensSerialNumber);
		(metadata.GammaDenominator, metadata.GammaNumerator) = this.GetRational(subIfd, ExifDirectoryBase.TagGamma);

		metadata.GPSVersionID = this.GetBinary(gps, GpsDirectory.TagVersionId);
		metadata.GPSLatitudeRef = this.GetString(gps, GpsDirectory.TagLatitudeRef);
		(metadata.GPSLatitudeDoa, metadata.GPSLatitudeMoa, metadata.GPSLatitudeSoa) = this.Get3Rational(gps, GpsDirectory.TagLatitude);
		metadata.GPSLongitudeRef = this.GetString(gps, GpsDirectory.TagLongitudeRef);
		(metadata.GPSLongitudeDoa, metadata.GPSLongitudeMoa, metadata.GPSLongitudeSoa) = this.Get3Rational(gps, GpsDirectory.TagLongitude);
		metadata.GPSAltitudeRef = this.GetInt(gps, GpsDirectory.TagAltitudeRef);
		(metadata.GPSAltitudeDenominator, metadata.GPSAltitudeNumerator) = this.GetRational(gps, GpsDirectory.TagAltitude);
		(metadata.GPSTimeStampHour, metadata.GPSTimeStampMinutes, metadata.GPSTimeStampSeconds) = this.Get3Rational(gps, GpsDirectory.TagTimeStamp);
		metadata.GPSSatellites = this.GetString(gps, GpsDirectory.TagSatellites);
		metadata.GPSStatus = this.GetString(gps, GpsDirectory.TagStatus);
		metadata.GPSMeasureMode = this.GetString(gps, GpsDirectory.TagMeasureMode);
		(metadata.GPSDOPDenominator, metadata.GPSDOPNumerator) = this.GetRational(gps, GpsDirectory.TagDop);
		metadata.GPSSpeedRef = this.GetString(gps, GpsDirectory.TagSpeedRef);
		(metadata.GPSSpeedDenominator, metadata.GPSSpeedNumerator) = this.GetRational(gps, GpsDirectory.TagSpeed);
		metadata.GPSTrackRef = this.GetString(gps, GpsDirectory.TagTrackRef);
		(metadata.GPSTrackDenominator, metadata.GPSTrackNumerator) = this.GetRational(gps, GpsDirectory.TagTrack);
		metadata.GPSImgDirectionRef = this.GetString(gps, GpsDirectory.TagImgDirectionRef);
		(metadata.GPSImgDirectionDenominator, metadata.GPSImgDirectionNumerator) = this.GetRational(gps, GpsDirectory.TagImgDirection);
		metadata.GPSMapDatum = this.GetString(gps, GpsDirectory.TagMapDatum);
		metadata.GPSDestLatitudeRef = this.GetString(gps, GpsDirectory.TagDestLatitudeRef);
		(metadata.GPSDestLatitudeDoa, metadata.GPSDestLatitudeMoa, metadata.GPSDestLatitudeSoa) = this.Get3Rational(gps, GpsDirectory.TagDestLatitude);
		metadata.GPSDestLongitudeRef = this.GetString(gps, GpsDirectory.TagDestLongitudeRef);
		(metadata.GPSDestLongitudeDoa, metadata.GPSDestLongitudeMoa, metadata.GPSDestLongitudeSoa) = this.Get3Rational(gps, GpsDirectory.TagDestLongitude);
		metadata.GPSDestBearingRef = this.GetString(gps, GpsDirectory.TagDestBearingRef);
		(metadata.GPSDestBearingDenominator, metadata.GPSDestBearingNumerator) = this.GetRational(gps, GpsDirectory.TagDestBearing);
		metadata.GPSDestDistanceRef = this.GetString(gps, GpsDirectory.TagDestDistanceRef);
		(metadata.GPSDestDistanceDenominator, metadata.GPSDestDistanceNumerator) = this.GetRational(gps, GpsDirectory.TagDestDistance);
		metadata.GPSProcessingMethod = this.GetBinary(gps, GpsDirectory.TagProcessingMethod);
		metadata.GPSAreaInformation = this.GetBinary(gps, GpsDirectory.TagAreaInformation);
		metadata.GPSDateStamp = this.GetString(gps, GpsDirectory.TagDateStamp);
		metadata.GPSDifferential = this.GetShort(gps, GpsDirectory.TagDifferential);

		return metadata;
	}
}
