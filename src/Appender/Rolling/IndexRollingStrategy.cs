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

namespace log4net.Appender.Rolling
{
    /// <summary>
    /// This is a simple rolling strategy implementation that rolls a file by
    /// appending the index 0. If a file exists that is named exactly as that
    /// that file will be renamed to index 1 until reaching the maximum index.
    /// The last file will be removed.
    /// </summary>
    /// <author>Dominik Psenner</author>
    public class IndexRollingStrategy : IRollingStrategy
    {
        #region Implementation of IRollingStrategy

        /// <summary>
        /// This method rolls a file with backup indexes between
        /// [0..10].
        /// </summary>
        /// <param name="file"></param>
        public void Roll(string file)
        {
            DoRoll(file, file, 0, 10);
        }

        #endregion

        #region Private Methods

        private void DoRoll(string baseFilename, string currentFilename, int currentIndex, int maxIndex)
        {
            if (currentIndex > maxIndex)
            {
                if (File.Exists(currentFilename))
                {
                    File.Delete(currentFilename);
                }
                return;
            }
            if (!File.Exists(currentFilename))
            {
                return;
            }

            // determine next filename
            string nextFilename = string.Format("{0}.{1}", baseFilename, currentIndex);

            // iterate the process until we meet the end
            DoRoll(baseFilename, nextFilename, currentIndex + 1, maxIndex);

            // rename this file now that there's free room after us
            File.Move(currentFilename, nextFilename);
        }

        #endregion
    }
}
