#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using log4net.Ext.EventID;

// Configure this assembly using the 'EventIDLogApp.exe.config' config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace EventIDLogApp
{
  /// <summary>
  /// EventIDLogApp
  /// </summary>
  internal static class EventIDLogApp
  {
    // Create a logger for use in this class
    private static readonly IEventIDLog log = EventIDLogManager.GetLogger(typeof(EventIDLogApp));

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      log.Info(1, "Application [" + typeof(EventIDLogApp).Assembly.GetName().Name + "] Start");

      log.Warn(40, "This is a warn message ");

      log.Info(2, "Application [" + typeof(EventIDLogApp).Assembly.GetName().Name + "] Stop");
    }
  }
}