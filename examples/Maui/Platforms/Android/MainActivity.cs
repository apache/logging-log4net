using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content.PM;

namespace MauiTestApplication;

/// <inheritdoc/>
[SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{ }