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
	public class Multiplier : Processor {
		public override string Category { get { return "Math/Arithmetics"; } }
		public override string Name { get { return "Multiplier"; } }
		public override string Description { get { return "Outputs the result of multiplying X by Y, provided to inputs."; } }
	
		private static Type[] ValueTypes = new Type[] { typeof(double) };
		public Multiplier() {
			Inputs["x"] = new Input("x", "X", ValueTypes, true, "Value to multiply by the Y value");
			Inputs["y"] = new Input("y", "Y", ValueTypes, true, "Value to multiply by the X value");
			
			Outputs["z"] = new Output("z", "X*Y", 0.0, typeof(double), "Resulting multiplication of X by Y");
		}
		
		public override void Process() {
			if( Inputs["x"].Value == null || Inputs["y"].Value == null )
				throw new UserFriendlyException("Multiplier requires both input values to be assigned", "One of inputs is not set");
			Outputs["z"].Value = (double)Inputs["x"].Value * (double)Inputs["y"].Value;
		}
	}
}
