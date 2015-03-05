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
	public abstract class BitmapMixerBase : Processor {
		public override string Category { get { return "Bitmaps"; } }
		public override string Name { get { return "Bitmap mixer"; } }
		public override string Description { get { return "Mixes two input bitmaps to form one final at output."; } }

		protected delegate byte MixFuncDelegate(byte x, byte y, byte a, bool isAlpha);
		protected MixFuncDelegate MixFunc = null;
		
		public BitmapMixerBase() {
			Outputs["bitmap"] = new Output("bitmap", "Bitmap", null, typeof(Bitmap), "A bitmap, resulting from mixing inputs.");
			
			Inputs["x"] = new Input("x", "X", new Type[] { typeof(Bitmap) }, false, "Primary bitmap.");
			Inputs["y"] = new Input("y", "Y", new Type[] { typeof(Bitmap) }, false, "Secondary bitmap.");
			Inputs["a"] = new Input("a", "Alpha", new Type[] { typeof(double), typeof(Bitmap) }, false, "Opacity, to apply to bitmap at Y input.");
		}
		
		public override void Process() {
			if( MixFunc == null )
				throw new UserFriendlyException(String.Format("{0} must have a MixFunc set for mixer to work", GetType().FullName), "This unit is not fully implemented yet");
			
			Bitmap x, y, a = null, o;
			if( Inputs["x"].Value == null && Inputs["y"].Value == null ) {
				Outputs["bitmap"].Value = null;
				return;
			}
			else if( Inputs["x"].Value == null ) {
				// we have only a bitmap at y value ... we'll have to copy it
				y = (Bitmap)Inputs["y"].Value;
				Outputs["bitmap"].Value = o = new Bitmap(y.Width, y.Height, y.BytesPerPixel);
				y.Pixels.CopyTo(o.Pixels, 0);
				return;
			}
			else if( Inputs["y"].Value == null ) {
				// we have only a bitmap at x value ... we'll have to copy it
				x = (Bitmap)Inputs["x"].Value;
				Outputs["bitmap"].Value = o = new Bitmap(x.Width, x.Height, x.BytesPerPixel);
				x.Pixels.CopyTo(o.Pixels, 0);
				return;
			}

			x = (Bitmap)Inputs["x"].Value;
			y = (Bitmap)Inputs["y"].Value;
			
			if( Inputs["a"].Value is Bitmap ) {
				a = (Bitmap)Inputs["a"].Value;
				if( x.Width != y.Width || x.Height != y.Height || x.Width != a.Width || x.Height != a.Height )
					throw new UserFriendlyException("Bitmaps at X, Y and Alpha inputs must have same width and height");
			}
			
			if( x.Width != y.Width || x.Height != y.Height )
				throw new UserFriendlyException("Bitmaps at X and Y inputs must have same width and height");

			if( x.BytesPerPixel != 1 && x.BytesPerPixel != 3 && x.BytesPerPixel != 4 )
				throw new UserFriendlyException(String.Format("X input bitmap has an unacceptable BytesPerPixel count ({0})", x.BytesPerPixel), "X input bitmap cannot be used in mixing because it's internal pixel format is not supported by mixer");
			if( y.BytesPerPixel != 1 && y.BytesPerPixel != 3 && y.BytesPerPixel != 4 )
				throw new UserFriendlyException(String.Format("Y input bitmap has an unacceptable BytesPerPixel count ({0})", y.BytesPerPixel), "Y input bitmap cannot be used in mixing because it's internal pixel format is not supported by mixer");
			
			int bytesPerPixel = (x.BytesPerPixel > y.BytesPerPixel) ? x.BytesPerPixel : y.BytesPerPixel;

			int idx = x.Width * x.Height - 1;
			int xidx, xdelta, xroffs, xgoffs, xboffs, xaoffs;
			int yidx, ydelta, yroffs, ygoffs, yboffs, yaoffs;
			int oidx, odelta, oroffs, ogoffs, oboffs, oaoffs;
			int aidx = 0, adelta = 0;
			byte pixel, opacity;
			
			Outputs["bitmap"].Value = o = new Bitmap(x.Width, x.Height, bytesPerPixel);
			
			InitVars("X", x, out xidx, out xdelta, out xroffs, out xgoffs, out xboffs, out xaoffs);
			InitVars("Y", y, out yidx, out ydelta, out yroffs, out ygoffs, out yboffs, out yaoffs);
			InitVars("", o, out oidx, out odelta, out oroffs, out ogoffs, out oboffs, out oaoffs);

			byte[] opix = o.Pixels, xpix = x.Pixels, ypix = y.Pixels, apix = null;
			
			if( Inputs["a"].Value == null || Inputs["a"].Value is double ) {
				// we have a constant opacity value at alpha input
				int opacityInt = (Inputs["a"].Value == null) ? (int)255 : (int)((double)Inputs["a"].Value * 256.0);
				if( opacityInt < 0 ) opacityInt = 0;
				if( opacityInt > 255 ) opacityInt = 255;
				opacity = (byte)opacityInt;
				
				if( odelta == 1 ) {
					for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
						opix[oidx] = MixFunc(xpix[xidx], ypix[yidx], opacity, false);
					}
				}
				else if( odelta == 3 ) {
					if( xdelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							pixel = xpix[xidx];
							opix[oidx + oroffs] = MixFunc(pixel, ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(pixel, ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(pixel, ypix[yidx + yboffs], opacity, false);
						}
					}
					else if( ydelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							pixel = ypix[yidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], pixel, opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], pixel, opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], pixel, opacity, false);
						}
					}
					else {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
						}
					}
				}
				else {
					if( xdelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							pixel = xpix[xidx];
							opix[oidx + oroffs] = MixFunc(pixel, ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(pixel, ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(pixel, ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(pixel, ypix[yidx + yaoffs], opacity, true);
						}
					}
					else if( xdelta == 3 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(255, ypix[yidx + yaoffs], opacity, true);
						}
					}
					else if( ydelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							pixel = ypix[yidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], pixel, opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], pixel, opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], pixel, opacity, false);
							opix[oidx + oaoffs] = MixFunc(xpix[xidx + xaoffs], pixel, opacity, true);
						}
					}
					else if( ydelta == 3 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(xpix[xidx + xaoffs], 255, opacity, true);
						}
					}
					else {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, oidx -= odelta ) {
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(xpix[xidx + xaoffs], ypix[yidx + yaoffs], opacity, true);
						}
					}
				}
			}
			else {
				// we have a bitmap at alpha input
				apix = a.Pixels;
				adelta = a.BytesPerPixel;
				aidx = (a.Width * a.Height - 1) * adelta;
				
				if( adelta == 1 ) {
					// one channel bitmap is fine as an alpha
				}
				else if( adelta == 3 ) {
					// this is quite a special case where we chould use grayscale
					// but for now we will keep default settings to use first channel of the image (most likely blue)
				}
				else if( adelta == 4 ) {
					switch( a.Format ) {
						// these two cases that the only ones having alpha channel in the beginning
						case BitmapFormat.ARGB: goto case BitmapFormat.ABGR;
						case BitmapFormat.ABGR: break;
						
						// all other formats are considered to have alpha in the end
						default: aidx += 3; break;
					}
				}
				else
					throw new UserFriendlyException("Internal BytePerPixel count of bitmap at Alpha input is not supported");
				
				if( odelta == 1 ) {
					for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
						opix[oidx] = MixFunc(xpix[xidx], ypix[yidx], apix[aidx], false);
					}
				}
				else if( odelta == 3 ) {
					if( xdelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							pixel = xpix[xidx];
							opix[oidx + oroffs] = MixFunc(pixel, ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(pixel, ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(pixel, ypix[yidx + yboffs], opacity, false);
						}
					}
					else if( ydelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							pixel = ypix[yidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], pixel, opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], pixel, opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], pixel, opacity, false);
						}
					}
					else {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
						}
					}
				}
				else {
					if( xdelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							pixel = xpix[xidx];
							opix[oidx + oroffs] = MixFunc(pixel, ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(pixel, ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(pixel, ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(pixel, ypix[yidx + yaoffs], opacity, true);
						}
					}
					else if( xdelta == 3 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(255, ypix[yidx + yaoffs], opacity, true);
						}
					}
					else if( ydelta == 1 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							pixel = ypix[yidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], pixel, opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], pixel, opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], pixel, opacity, false);
							opix[oidx + oaoffs] = MixFunc(xpix[xidx + xaoffs], pixel, opacity, true);
						}
					}
					else if( ydelta == 3 ) {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(xpix[xidx + xaoffs], 255, opacity, true);
						}
					}
					else {
						for( ; idx >= 0; idx--, xidx -= xdelta, yidx -= ydelta, aidx -= adelta, oidx -= odelta ) {
							opacity = apix[aidx];
							opix[oidx + oroffs] = MixFunc(xpix[xidx + xroffs], ypix[yidx + yroffs], opacity, false);
							opix[oidx + ogoffs] = MixFunc(xpix[xidx + xgoffs], ypix[yidx + ygoffs], opacity, false);
							opix[oidx + oboffs] = MixFunc(xpix[xidx + xboffs], ypix[yidx + yboffs], opacity, false);
							opix[oidx + oaoffs] = MixFunc(xpix[xidx + xaoffs], ypix[yidx + yaoffs], opacity, true);
						}
					}
				}
			}
		}
		
		private void CloneBitmap(int idx, byte[] opix, int oidx, int odelta, int oroffs, int ogoffs, int oboffs, int oaoffs, byte[] ipix, int iidx, int idelta, int iroffs, int igoffs, int iboffs, int iaoffs) {
			if( odelta == 1 ) {
				for( ; idx >= 0; idx--, iidx -= idelta, oidx -= odelta ) {
					opix[oidx] = ipix[iidx];
				}
			}
			else if( odelta == 3 ) {
				if( idelta == 1 ) {
					for( ; idx >= 0; idx--, iidx -= idelta, oidx -= odelta ) {
						opix[oidx + oroffs] =
						opix[oidx + ogoffs] =
						opix[oidx + oboffs] = ipix[iidx];
					}
				}
				else {
					for( ; idx >= 0; idx--, iidx -= idelta, oidx -= odelta ) {
						opix[oidx + oroffs] = ipix[iidx + iroffs];
						opix[oidx + ogoffs] = ipix[iidx + igoffs];
						opix[oidx + oboffs] = ipix[iidx + iboffs];
					}
				}
			}
			else if( idelta == 1 ) {
				for( ; idx >= 0; idx--, iidx -= idelta, oidx -= odelta ) {
					opix[oidx + oaoffs] = 255;
					opix[oidx + oroffs] =
					opix[oidx + ogoffs] =
					opix[oidx + oboffs] = ipix[iidx];
				}
			}
			else if( idelta == 3 ) {
				for( ; idx >= 0; idx--, iidx -= idelta, oidx -= odelta ) {
					opix[oidx + oaoffs] = 255;
					opix[oidx + oroffs] = ipix[iidx + iroffs];
					opix[oidx + ogoffs] = ipix[iidx + igoffs];
					opix[oidx + oboffs] = ipix[iidx + iboffs];
				}
			}
			else {
				for( ; idx >= 0; idx--, iidx -= idelta, oidx -= odelta ) {
					opix[oidx + oaoffs] = ipix[iidx + iaoffs];
					opix[oidx + oroffs] = ipix[iidx + iroffs];
					opix[oidx + ogoffs] = ipix[iidx + igoffs];
					opix[oidx + oboffs] = ipix[iidx + iboffs];
				}
			}
		}
		
		private void InitVars(string inputName, Bitmap bmp, out int idx, out int bytesPerPixel, out int roffs, out int goffs, out int boffs, out int aoffs) {
			roffs = goffs = boffs = aoffs = 0;
			bytesPerPixel = bmp.BytesPerPixel;
			idx = (bmp.Width * bmp.Height - 1) * bytesPerPixel;
			switch(bytesPerPixel) {
				case 1: break;
				case 3:
					switch(bmp.Format) {
						case BitmapFormat.RGBA: goto case BitmapFormat.RGB; // invalid format
						case BitmapFormat.ARGB: goto case BitmapFormat.RGB; // invalid format
						
						case BitmapFormat.RGB: goffs = 1; boffs = 2; break;
						default: goffs = 1; roffs = 2; break;
					}
					break;
					
				case 4:
					switch(bmp.Format) {
						case BitmapFormat.RGB: goto case BitmapFormat.RGBA; // invalid format
						
						case BitmapFormat.RGBA: goffs = 1; boffs = 2; aoffs = 3; break;
						default: goffs = 1; roffs = 2; aoffs = 3; break;
					}
					break;				
				default:
					throw new UserFriendlyException(String.Format("Internal BytePerPixel count of bitmap at {0} input is not supported", inputName));
			}
		}
	}
}
