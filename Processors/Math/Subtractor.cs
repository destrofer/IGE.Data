﻿/*
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
	public class Subtractor : Processor {
		public override string Category { get { return "Math/Arithmetics"; } }
		public override string Name { get { return "Subtractor"; } }
		public override string Description { get { return "Outputs the result of subtracting two numbers, provided to inputs X and Y."; } }
	
		private static Type[] ValueTypes = new Type[] { typeof(double) };
		public Subtractor() {
			Inputs["x"] = new Input("x", "X", ValueTypes, true, "Value to subtract Y value from");
			Inputs["y"] = new Input("y", "Y", ValueTypes, true, "Value to be subtracted from the X value");
			
			Outputs["z"] = new Output("z", "X-Y", 0.0, typeof(double), "Resulting subtraction of X and Y");
		}
		
		public override void Process() {
			if( Inputs["x"].Value == null || Inputs["y"].Value == null )
				throw new UserFriendlyException("Subtractor requires both input values to be assigned", "One of inputs is not set");
			Outputs["z"].Value = (double)Inputs["x"].Value - (double)Inputs["y"].Value;
		}
	}
}
