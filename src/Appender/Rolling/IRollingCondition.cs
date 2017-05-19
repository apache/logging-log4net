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

namespace log4net.Appender.Rolling
{
    /// <summary>
    /// The interface definition of a rolling condition.
    /// </summary>
    /// <author>Dominik Psenner</author>
    public interface IRollingCondition
    {
        /// <summary>
        /// This method should implement all checks needed to determine if a file
        /// can be rolled based on the conditions it implies to the file.
        /// </summary>
        /// <param name="file">the file to be rolled</param>
        /// <returns>true when the file has met all conditions to be rolled</returns>
        bool IsMet(string file);
    }
}
