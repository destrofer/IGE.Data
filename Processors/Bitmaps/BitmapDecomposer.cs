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
	public class BitmapDecomposer : Processor {
		public override string Category { get { return "Bitmaps"; } }
		public override string Name { get { return "Bitmap decomposer"; } }
		public override string Description { get { return "Decomposes input bitmap into four R, G, B and Alpha component bitmaps."; } }
	
		public BitmapDecomposer() {
			Inputs["bitmap"] = new Input("bitmap", "Bitmap", new Type[] { typeof(Bitmap) }, true, "Bitmap, to decompose into R, G, B and Alpha components.");
			
			Outputs["r"] = new Output("r", "R", null, typeof(Bitmap), "Red component of the input bitmap.");
			Outputs["g"] = new Output("g", "G", null, typeof(Bitmap), "Green component of the input bitmap.");
			Outputs["b"] = new Output("b", "B", null, typeof(Bitmap), "Blue component of the input bitmap.");
			Outputs["a"] = new Output("a", "A", null, typeof(Bitmap), "Alpha component of the input bitmap.");
		}
		
		public override void Process() {
			if( Inputs["bitmap"].Value == null )
				throw new UserFriendlyException("Bitmap decomposer requires a bitmap on input");
			Bitmap input = (Bitmap)Inputs["bitmap"].Value;
			
			Bitmap
				r = new Bitmap(input.Width, input.Height, 1),
				g = new Bitmap(input.Width, input.Height, 1),
				b = new Bitmap(input.Width, input.Height, 1),
				a = new Bitmap(input.Width, input.Height, 1);
			
			Outputs["r"].Value = r;
			Outputs["g"].Value = g;
			Outputs["b"].Value = b;
			Outputs["a"].Value = a;
			
			r.Format = BitmapFormat.R;
			g.Format = BitmapFormat.G;
			b.Format = BitmapFormat.B;
			a.Format = BitmapFormat.A;
			
			byte[] ip = input.Pixels, rp = r.Pixels, gp = g.Pixels, bp = b.Pixels, ap = a.Pixels, out1 = null, out2 = null, out3 = null, out4 = null;
			
			int idx, idx3, idx4, size = input.Width * input.Height - 1;
			
			if( input.BytesPerPixel == 4 ) {
				switch(input.Format) {
					// this format is for 3 BytesPerPixel
					case BitmapFormat.RGB: goto case BitmapFormat.RGBA;
					
					case BitmapFormat.ABGR: out1 = ap; out2 = bp; out3 = gp; out4 = rp; break;
					case BitmapFormat.ARGB: out1 = ap; out2 = rp; out3 = gp; out4 = bp; break;
					case BitmapFormat.RGBA: out1 = rp; out2 = gp; out3 = bp; out4 = ap; break;
					default: out1 = bp; out2 = gp; out3 = rp; out4 = ap; break;
				}
				for( idx = size, idx4 = size * 4; idx >= 0; idx--, idx4 -= 4 ) {
					out1[idx] = ip[idx4];
					out2[idx] = ip[idx4 + 1];
					out3[idx] = ip[idx4 + 2];
					out4[idx] = ip[idx4 + 3];
				}
			}
			else if( input.BytesPerPixel == 3 ) {
				switch(input.Format) {
					// these formats are for 4 BytesPerPixel
					case BitmapFormat.RGBA: goto case BitmapFormat.RGB;
					case BitmapFormat.ARGB: goto case BitmapFormat.RGB;
					
					case BitmapFormat.RGB: out1 = rp; out2 = gp; out3 = bp; break;
					default: out1 = bp; out2 = gp; out3 = rp; break;
				}
				for( idx = size, idx3 = size * 3; idx >= 0; idx--, idx3 -= 3 ) {
					out1[idx] = ip[idx3];
					out2[idx] = ip[idx3 + 1];
					out3[idx] = ip[idx3 + 2];
					ap[idx] = 255;
				}
			}
			else if( input.BytesPerPixel == 1 ) {
				if( input.Format == BitmapFormat.A ) {
					for( idx = size; idx >= 0; idx-- ) {
						out1[idx] = out2[idx] = out3[idx] = 255;
						out4[idx] = ip[idx];
					}
				}
				else {
					for( idx = size; idx >= 0; idx-- ) {
						out1[idx] = out2[idx] = out3[idx] = ip[idx];
						out4[idx] = 255;
					}
				}
			}
			else
				throw new UserFriendlyException(String.Format("input bitmap has an unacceptable BytesPerPixel count ({0})", input.BytesPerPixel), "Input bitmap cannot be decomposed because it's internal pixel format is not supported by decomposer");
		}
	}
}
