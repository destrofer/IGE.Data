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

namespace IGE.Processors {
	public class Constant : Processor {
		public override string Category { get { return "Math/Arithmetics"; } }
		public override string Name { get { return "Constant"; } }
		public override string Description { get { return "Outputs the constant value."; } }
	
		public Constant() {
			Outputs["c"] = new Output("c", "Value", 0.0, typeof(double), "Constant value");
			Attributes["c"] = new Input("c", "Value", new Type[] { typeof(double) }, true, "Constant value");
		}
		
		public override void Process() {
			if( Attributes["c"].Value == null )
				throw new UserFriendlyException("Constant value has to be assigned", "Constant value attribute is not set");
			Outputs["c"].Value = (double)Attributes["c"].Value;
		}
	}
}
