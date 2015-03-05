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
	public class BitmapMixer : BitmapMixerBase {
		public override string Category { get { return "Bitmaps"; } }
		public override string Name { get { return "Bitmap mixer"; } }
		public override string Description { get { return "Mixes two input bitmaps to form one final at output."; } }
	
		public BitmapMixer() {
			MixFunc = MixPixels;
		}
		
		protected byte MixPixels(byte x, byte y, byte a, bool isAlpha) {
			return (byte)((int)x * (255 - (int)a) / 255 + (int)y * (int)a / 255);
		}
	}
}
