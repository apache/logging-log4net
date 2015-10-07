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

using System.Collections;
using log4net.Appender;
using log4net.Layout;
using log4net.Util;

namespace SampleAppendersApp.Appender
{
    /// <summary>
    /// 
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    ///   <appender name="PatternLayoutAdoNetAppender" type="ConsoleApplication1.PatternLayoutAdoNetAppender, ConsoleApplication1">
    ///   <connectionType value="log4net.Tests.Appender.AdoNet.Log4NetConnection, log4net.Tests" />
    ///   <connectionString value="..." />
    ///   <commandText value="INSERT INTO Log4Net (CustomValue1, CustomValue2) VALUES (@CustomValue1, @CustsomValue2)" />
    ///   <converter>
    ///    <name value="echo" />
    ///     <type value="ConsoleApplication1.EchoConverter, ConsoleApplication1" />
    ///   </converter>
    ///   <converter>
    ///     <name value="reverse" />
    ///     <type value="ConsoleApplication1.ReverseConverter, ConsoleApplication1" />
    ///   </converter>
    ///   <patternLayoutParameter>
    ///     <parameterName value="@CustomValue1"/>
    ///     <dbType value="String" />
    ///     <conversionPattern value="%echo{Hello World}" />
    ///   </patternLayoutParameter>
    ///   <patternLayoutParameter>
    ///     <parameterName value="@CustomValue2"/>
    ///     <dbType value="String" />
    ///     <conversionPattern value="%reverse{Goodbye}" />
    ///   </patternLayoutParameter>
    ///   </appender>
    /// ]]>
    /// </code>
    /// </example>
    public class PatternLayoutAdoNetAppender : AdoNetAppender
    {
        private readonly ArrayList m_converters = new ArrayList();

        public void AddConverter(ConverterInfo converterInfo)
        {
            m_converters.Add(converterInfo);
        }

        public void AddPatternLayoutParameter(PatternLayoutAdoNetAppenderParameter parameter)
        {
            PatternLayout patternLayout = new PatternLayout(parameter.ConversionPattern);
            addConveters(patternLayout);
            patternLayout.ActivateOptions();

            parameter.Layout = new Layout2RawLayoutAdapter(patternLayout);
            m_parameters.Add(parameter);
        }

        private void addConveters(PatternLayout patternLayout)
        {
            foreach (ConverterInfo conveterInfo in m_converters)
            {
                patternLayout.AddConverter(conveterInfo);
            }
        }
    }
}