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
	public class BitmapComposer : Processor {
		public override string Category { get { return "Bitmaps"; } }
		public override string Name { get { return "Bitmap composer"; } }
		public override string Description { get { return "Composes a bitmap from four R, G, B and Alpha component bitmaps provided at inputs."; } }
	
		public BitmapComposer() {
			Outputs["bitmap"] = new Output("bitmap", "Bitmap", null, typeof(Bitmap), "Bitmap, composed from R, G, B and Aplha components.");
			
			Inputs["r"] = new Input("r", "R", new Type[] { typeof(Bitmap) }, false, "Red component for the output bitmap.");
			Inputs["g"] = new Input("g", "G", new Type[] { typeof(Bitmap) }, false, "Green component for the output bitmap.");
			Inputs["b"] = new Input("b", "B", new Type[] { typeof(Bitmap) }, false, "Blue component for the output bitmap.");
			Inputs["a"] = new Input("a", "A", new Type[] { typeof(Bitmap) }, false, "Alpha component for the output bitmap.");
		}
		
		public override void Process() {
			Bitmap r = (Bitmap)Inputs["r"].Value;
			Bitmap g = (Bitmap)Inputs["g"].Value;
			Bitmap b = (Bitmap)Inputs["b"].Value;
			Bitmap a = (Bitmap)Inputs["a"].Value;
			
			if( r == null && g == null && b == null && a == null ) {
				Outputs["bitmap"].Value = null;
				return;
			}
			
			int width = 0, height = 0;
			if( r != null && width > 0 && r.Width != width && r.Height != height )
				throw new UserFriendlyException("All input bitmaps must have same widths and heights");
			else if( r != null ) {
				width = r.Width;
				height = r.Height;
			}
			if( g != null && width > 0 && g.Width != width && g.Height != height )
				throw new UserFriendlyException("All input bitmaps must have same widths and heights");
			else if( g != null ) {
				width = g.Width;
				height = g.Height;
			}
			if( b != null && width > 0 && b.Width != width && b.Height != height )
				throw new UserFriendlyException("All input bitmaps must have same widths and heights");
			else if( b != null ) {
				width = b.Width;
				height = b.Height;
			}
			if( a != null && width > 0 && a.Width != width && a.Height != height )
				throw new UserFriendlyException("All input bitmaps must have same widths and heights");
			else if( a != null ) {
				width = a.Width;
				height = a.Height;
			}
			
			Bitmap outBitmap = new Bitmap(width, height, 4);
			Outputs["bitmap"].Value = outBitmap;
			
			outBitmap.Format = BitmapFormat.BGRA;
			byte[] outPixels = outBitmap.Pixels;
			
			int i;
			int idx = (width * height - 1) * 4;
			
			byte[] blank = null;
			if( r == null || g == null || b == null ) {			
				blank = new byte[width * height];
				for( i = blank.Length - 1; i >= 0; i-- )
					blank[i] = 0;
			}

			byte[] solid = null;
			if( a == null || a.BytesPerPixel == 3 ) {
				solid = new byte[width * height];
				for( i = solid.Length - 1; i >= 0; i-- )
					solid[i] = 255;
			}
			
			byte[] rp, gp, bp, ap;
			int rpp, gpp, bpp, app;
			int ridx, gidx, bidx, aidx;
			
			InitVars(BitmapFormat.R, r, width, height, blank, out rp, out rpp, out ridx);
			InitVars(BitmapFormat.G, g, width, height, blank, out gp, out gpp, out gidx);
			InitVars(BitmapFormat.B, b, width, height, blank, out bp, out bpp, out bidx);
			InitVars(BitmapFormat.A, a, width, height, solid, out ap, out app, out aidx);
			
			for( ; idx >= 0; idx -= 4, ridx -= rpp, gidx -= gpp, bidx -= bpp, aidx -= app ) {
				outPixels[idx] = bp[bidx];
				outPixels[idx + 1] = gp[gidx];
				outPixels[idx + 2] = rp[ridx];
				outPixels[idx + 3] = ap[aidx];
			}
		}
		
		private void InitVars(BitmapFormat component, Bitmap bmp, int width, int height, byte[] blank, out byte[] pixels, out int bytesPerPixel, out int initialIdx) {
			initialIdx = width * height - 1;
			if( bmp == null ) {
				pixels = blank;
				bytesPerPixel = 1;
				return;
			}
			pixels = bmp.Pixels;
			bytesPerPixel = bmp.BytesPerPixel;
			if( bytesPerPixel == 1 ) {
				// no changes have to be made
			}
			else if( bytesPerPixel == 3 ) {
				// the input image does not contain an alpha channel so it's either: make a grayscale from R,G and B, or use solid color map
				if( component == BitmapFormat.A ) {
					pixels = blank;
					bytesPerPixel = 1;
					return;
				}
				initialIdx *= bytesPerPixel;
				switch( bmp.Format ) {
					case BitmapFormat.RGB:
						switch( component ) {
							case BitmapFormat.G: initialIdx += 1; break;
							case BitmapFormat.B: initialIdx += 2; break;
						}
						break;
						
					default:
						switch( component ) {
							case BitmapFormat.G: initialIdx += 1; break;
							case BitmapFormat.R: initialIdx += 2; break;
						}
						break;
				}
			}
			else if( bytesPerPixel == 4 ) {
				initialIdx *= bytesPerPixel;
				switch( bmp.Format ) {
					case BitmapFormat.RGBA:
						switch( component ) {
							case BitmapFormat.G: initialIdx += 1; break;
							case BitmapFormat.B: initialIdx += 2; break;
							case BitmapFormat.A: initialIdx += 3; break;
						}
						break;

					case BitmapFormat.ARGB:
						switch( component ) {
							case BitmapFormat.R: initialIdx += 1; break;
							case BitmapFormat.G: initialIdx += 2; break;
							case BitmapFormat.B: initialIdx += 3; break;
						}
						break;
						
					case BitmapFormat.ABGR:
						switch( component ) {
							case BitmapFormat.B: initialIdx += 1; break;
							case BitmapFormat.G: initialIdx += 2; break;
							case BitmapFormat.R: initialIdx += 3; break;
						}
						break;
						
					default:
						switch( component ) {
							case BitmapFormat.G: initialIdx += 1; break;
							case BitmapFormat.R: initialIdx += 2; break;
							case BitmapFormat.A: initialIdx += 3; break;
						}
						break;
				}
			}
			else
				throw new UserFriendlyException(String.Format("{0} input component bitmap has an unacceptable BytesPerPixel count ({1})", component, bytesPerPixel), String.Format("{0} component input bitmap cannot be used in composition because it's internal pixel format is not supported by composer", component));
		}
	}
}
