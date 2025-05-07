using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace MauiApp;

/// <inheritdoc/>
internal static class Program : MauiApplication
{
  /// <inheritdoc/>
  protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

  private static void Main(string[] args) => new Program().Run(args);
}