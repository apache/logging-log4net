using Foundation;

namespace MauiTestApplication;

/// <inheritdoc/>
[Register("AppDelegate")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class AppDelegate : MauiUIApplicationDelegate
{
  /// <inheritdoc/>
  protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}