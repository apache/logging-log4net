#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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

using log4net;

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// Implement this interface to create new instances of <see cref="Logger" /> 
	/// or a sub-class of <see cref="Logger" />.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface is used by the <see cref="Hierarchy"/> to 
	/// create new <see cref="Logger"/> objects.
	/// </para>
	/// <para>
	/// The <see cref="MakeNewLoggerInstance"/> method is called
	/// to create a named <see cref="Logger" />.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface ILoggerFactory
	{
		/// <summary>
		/// Constructs a new <see cref="Logger" /> instance with the 
		/// specified name.
		/// </summary>
		/// <param name="name">The name of the <see cref="Logger" />.</param>
		/// <returns>The <see cref="Logger" /> instance for the specified name.</returns>
		/// <remarks>
		/// <para>
		/// Called by the <see cref="Hierarchy"/> to create
		/// new named <see cref="Logger"/> instances.
		/// </para>
		/// </remarks>
		Logger MakeNewLoggerInstance(string name);
	}
}
