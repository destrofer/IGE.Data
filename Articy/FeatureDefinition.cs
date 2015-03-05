/*
 * Author: Viacheslav Soroka
 * 
 * This file is part of IGE <https://github.com/destrofer/IGE>.
 * 
 * IGE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * IGE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with IGE.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace IGE.Data.Articy {
	/// <summary>
	/// </summary>
	public class FeatureDefinition : ArticyObject {
		// properties are indexed by their technical name since it will be a faster way of finding entity properties (entities don't contain property ids)
		// by id properties area be available via project's objects dictionary
		public Dictionary<string, PropertyDefinition> Properties = new Dictionary<string, PropertyDefinition>();
		
		public FeatureDefinition(string id) : base(id) {
		}
	}
}
