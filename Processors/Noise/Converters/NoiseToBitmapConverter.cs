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
	public class NoiseToBitmapConverter : Processor {
		public override string Category { get { return "Noise/Converters"; } }
		public override string Name { get { return "Noise to bitmap converter"; } }
		public override string Description { get { return "Outputs the 2D bitmap, built using noise, given on input."; } }
	
		public NoiseToBitmapConverter() {
			Inputs["noise"] = new Input("noise", "Noise", new Type[] { typeof(INoiseGenerator) }, true, "Input noise");
			Inputs["z"] = new Input("z", "Z", new Type[] { typeof(double) }, false, "Z axis value in noise space (in case when 3D noise has to be used)");
			Inputs["t"] = new Input("t", "T", new Type[] { typeof(double) }, false, "T axis value in noise space (in case when 4D noise has to be used)");

			Attributes["width"] = new Input("width", "Width", new Type[] { typeof(ushort) }, true, "Width of the output bitmap");
			Attributes["height"] = new Input("height", "Height", new Type[] { typeof(ushort) }, true, "Height of the output bitmap");
			Attributes["xscale"] = new Input("xscale", "X scale", new Type[] { typeof(double) }, false, "Horizaontal scale of the noise");
			Attributes["yscale"] = new Input("yscale", "Y scale", new Type[] { typeof(double) }, false, "Vertical scale of the noise");
			Attributes["xoffset"] = new Input("xoffset", "X offset", new Type[] { typeof(double) }, false, "Horizaontal offset of the noise");
			Attributes["yoffset"] = new Input("yoffset", "Y offset", new Type[] { typeof(double) }, false, "Vertical offset of the noise");
			
			Outputs["bitmap"] = new Output("bitmap", "Bitmap", null, typeof(Bitmap), "Bitmap, built from the input noise.");
		}
		
		public override void Process() {
			if( Inputs["noise"].Value == null )
				throw new UserFriendlyException("Noise to bitmap converter requires a noise on input");
			if( Attributes["width"].Value == null || Attributes["height"].Value == null )
				throw new UserFriendlyException("Bitmap width and height must be set");
			if( (ushort)Attributes["width"].Value == 0 || (ushort)Attributes["height"].Value == 0 )
				throw new UserFriendlyException("Bitmap width and height cannot be 0");
			
			INoiseGenerator noise = (INoiseGenerator)Inputs["noise"].Value;
			int i, j, w, h, idx;
			double x, y, z, xs, ys, r, ix;
			w = (int)(ushort)Attributes["width"].Value;
			h = (int)(ushort)Attributes["height"].Value;
			xs = (Attributes["xscale"].Value == null || (double)Attributes["xscale"].Value == 0.0) ? 1.0 : (1.0 / (double)Attributes["xscale"].Value);
			ys = (Attributes["yscale"].Value == null || (double)Attributes["yscale"].Value == 0.0) ? 1.0 : (1.0 / (double)Attributes["yscale"].Value);
			ix = ((Attributes["xoffset"].Value == null) ? 0.0 : (double)Attributes["xoffset"].Value) + (double)(w - 1) * xs;
			y = ((Attributes["yoffset"].Value == null) ? 0.0 : (double)Attributes["yoffset"].Value) + (double)(h - 1) * ys;
			idx = w * h - 1;
			
			Bitmap bmp = new Bitmap(w, h, 1);
			Outputs["bitmap"].Value = bmp;
			byte[] pixels = bmp.Pixels;
			
			if( Inputs["t"].Value != null ) {
				z = (Inputs["z"].Value == null) ? 0.0 : (double)Inputs["z"].Value;
				double t = (double)Inputs["t"].Value;
				for( j = h - 1; j >= 0; j--, y -= ys ) {
					x = ix;
					for( i = w - 1; i >= 0; i--, x -= xs, idx-- ) {
						r = noise[x, y, z, t] * 256.0;
						pixels[idx] = (r < 0.0) ? (byte)0 : ((r >= 256.0) ? (byte)255 : (byte)r);
					}
				}
			}
			else if( Inputs["z"].Value != null ) {
				z = (double)Inputs["z"].Value;
				for( j = h - 1; j >= 0; j--, y -= ys ) {
					x = ix;
					for( i = w - 1; i >= 0; i--, x -= xs, idx-- ) {
						r = noise[x, y, z] * 256.0;
						pixels[idx] = (r < 0.0) ? (byte)0 : ((r >= 256.0) ? (byte)255 : (byte)r);
					}
				}
			}
			else {
				for( j = h - 1; j >= 0; j--, y -= ys ) {
					x = ix;
					for( i = w - 1; i >= 0; i--, x -= xs, idx-- ) {
						r = noise[x, y] * 256.0;
						pixels[idx] = (r < 0.0) ? (byte)0 : ((r >= 256.0) ? (byte)255 : (byte)r);
					}
				}
			}
		}
	}
}
