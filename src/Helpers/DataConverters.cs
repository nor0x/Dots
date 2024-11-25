using Avalonia.Data.Converters;
using Avalonia.Media;
using Dots.Data;
using System;
using System.Globalization;

namespace Dots.Helpers
{
	public class ReleaseTypeToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is ReleaseType releaseType)
			{
				return releaseType switch
				{
					ReleaseType.Sts => Color.Parse("#e85aad"),
					ReleaseType.Lts => Color.Parse("#116329"),
					_ => Color.Parse("#000000"),
				};
			}
			else return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class SupportPhaseToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SupportPhase supportPhase)
			{
				return supportPhase switch
				{
					SupportPhase.Eol => Color.Parse("#e16f24"),
					SupportPhase.Maintenance => Color.Parse("#eac54f"),
					SupportPhase.Active => Color.Parse("#2da44e"),
					SupportPhase.GoLive => Color.Parse("#a475f9"),
					_ => Color.Parse("#000000"),
				};
			}
			else return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class PreviewTypeToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string previewType)
			{
				if (previewType.Contains("-preview")) return Color.Parse("#bf8700");
				else if (previewType.Contains("-rc")) return Color.Parse("#7d4e00");
				else return Color.Parse("#000000");
			}
			else return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class PreviewTypeToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string previewType)
			{
				if (previewType.Contains("-preview")) return "Preview";
				else if (previewType.Contains("-rc")) return "Release Candidate";
				else return Color.Parse("#000000");
			}
			else return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class RidToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Data.Rid rid && rid.ToString() is string ridstring)
			{
				if (ridstring.Contains("Win")) return "🪟";
				else if (ridstring.Contains("Osx")) return "🍏";
				else return "🐧";
			}
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}	
	
	public class FileNameToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string filename)
			{

				if (filename.EndsWith(".exe"))
				{
					return Color.Parse("#2da44e");
				}
				else if (filename.EndsWith(".tar.gz") || filename.EndsWith(".zip"))
				{
					return Color.Parse("#bf8700");
				}
				else if (filename.EndsWith(".pkg"))
				{
					return Color.Parse("#0969da");
				}
				else
				{
					return Color.Parse("#000000");
				}
			}
			else return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}	
	
	public class FileNameToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string filename)
			{

				if (filename.EndsWith(".exe"))
				{
					return LucideIcons.AppWindow;
				}
				else if (filename.EndsWith(".tar.gz") || filename.EndsWith(".zip"))
				{
					return LucideIcons.FolderArchive;
				}
				else if (filename.EndsWith(".pkg"))
				{
					return LucideIcons.Package;
				}
				else
				{
					return Color.Parse("#000000");
				}
			}
			else return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class StringIsNullOrEmptyConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string str && !string.IsNullOrEmpty(str)) return true;
			else return false;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

