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
	public class WhiteNoiseGenerator : Processor {
		public override string Category { get { return "Noise/Generators"; } }
		public override string Name { get { return "White noise generator"; } }
		public override string Description { get { return "Every time outputs a random double value in range of [0, 1]."; } }
		public override bool Dynamic { get { return true; } }
	
		protected ExtRandom m_Random = new ExtRandom();
	
		public WhiteNoiseGenerator() {
			Outputs["noise"] = new Output("noise", "Random", m_Random.NextDouble(), typeof(double), "Random value");
		}
		
		public override void Process() {
			Outputs["noise"].Value = (double)m_Random.NextDouble();
		}
	}
}
