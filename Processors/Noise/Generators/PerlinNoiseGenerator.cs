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

using IGE.Processors;

namespace IGE.Processors.Noise {
	public class PerlinNoiseGenerator : Processor {
		public override string Category { get { return "Noise/Generators"; } }
		public override string Name { get { return "Perlin noise generator"; } }
		public override string Description { get { return "Outputs a Perlin noise object, which can be used in noise processors."; } }
	
		protected Perlin m_Noise = new Perlin();
	
		public PerlinNoiseGenerator() {
			Outputs["noise"] = new Output("noise", "Perlin", m_Noise, typeof(Perlin), "Perlin noise");
			Attributes["octaves"] = new Input("octaves", "Octaves", new Type[] { typeof(int) }, false, "Number of noise octaves");
		}
		
		public override void Process() {
			if( Attributes["octaves"].Value != null && (int)Attributes["octaves"].Value >= 1 )
				m_Noise.Octaves = (int)Attributes["octaves"].Value;
			Outputs["noise"].Value = m_Noise;
		}
	}
}
