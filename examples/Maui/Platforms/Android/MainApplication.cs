using Android.App;
using Android.Runtime;

namespace MauiTestApplication;

/// <inheritdoc/>
[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) 
  : MauiApplication(handle, ownership)
{
  /// <inheritdoc/>
  protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}