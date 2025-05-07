using log4net;
using log4net.Config;
using Microsoft.Extensions.Logging;

namespace MauiTestApplication;

/// <inheritdoc/>
public static class MauiProgram
{
  /// <inheritdoc/>
  public static MauiApp CreateMauiApp()
  {
    using Stream stream = typeof(MauiProgram).Assembly.GetManifestResourceStream(nameof(MauiTestApplication) + ".log4net.xml")
      ?? throw new InvalidOperationException();
    ILog log = LogManager.GetLogger(typeof(MauiProgram));
    XmlConfigurator.Configure(stream);
    log.Info("Entering application.");
    MauiAppBuilder builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .ConfigureFonts(fonts =>
      {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
      });

#if DEBUG
    builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}