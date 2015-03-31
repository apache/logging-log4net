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
using System.IO;
using System.Threading;
using log4net.DateFormatter;

namespace log4net.playground {

    // dmcs -m:log4net.playground.AbsoluteTimeDateFormatterTiming -r:build/bin/log4net-1.3/mono/4.0/debug/log4net-1.3.dll src/playground/AbsoluteTimeDateFormatterTiming.cs
    public class AbsoluteTimeDateFormatterTiming {

        private static long TICKS = 0;

        private static readonly Random RND = new Random(Environment.TickCount);
        private static readonly AbsoluteTimeDateFormatter F = new AbsoluteTimeDateFormatter();
        
        public static void Main(string[] args) {

            Thread[] threads = new Thread[20];
            for (int i = 0; i < threads.Length; i++) {
                threads[i] = new Thread(SingleThread);
                threads[i].Start();
            }
            for (int i = 0; i < threads.Length; i++) {
                threads[i].Join();
            }

            Console.Error.WriteLine("used {0} ticks", TICKS);
        }

        private static void SingleThread() {
            StringWriter sw = new StringWriter();
            for (int i = 0; i < 1000; i++) {
                Thread.Sleep(RND.Next(15));
                int before = Environment.TickCount;
                for (int j = 0; j < 1000; j++) {
                    F.FormatDate(DateTime.Now, sw);
                }
                int after = Environment.TickCount;
                Interlocked.Add(ref TICKS, after - before);
            }
        }

    }

}