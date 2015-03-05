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

using IGE.IO;

namespace IGE.Processors {
	public class BitmapLoader : Processor {
		public override string Category { get { return "Bitmaps"; } }
		public override string Name { get { return "Bitmap loader"; } }
		public override string Description { get { return "Loads a bitmap from a file and outputs it."; } }
		public override bool Dynamic { get { return true; } }
	
		public BitmapLoader() {
			Attributes["path"] = new Input("path", "Source", new Type[] { typeof(string) }, true, "Path to a file, where bitmap is located.");
			
			Outputs["bitmap"] = new Output("bitmap", "Bitmap", null, typeof(Bitmap), "Bitmap, loaded from a source file.");
			Outputs["width"] = new Output("width", "Width", null, typeof(ushort), "Width of the bitmap, loaded from a source file.");
			Outputs["height"] = new Output("height", "Height", null, typeof(ushort), "Height of the bitmap, loaded from a source file.");
		}
		
		public override void Process() {
			if( Attributes["path"].Value == null )
				throw new UserFriendlyException("Bitmap source must be set");
			if( ((string)Attributes["path"].Value).Equals("") )
				throw new UserFriendlyException("Bitmap source must not be empty");
			
			BitmapFile file = GameFile.LoadFile<BitmapFile>((string)Attributes["path"].Value);
			if( file == null )
				throw new UserFriendlyException("Could not find a source bitmap file or the file format is not supported");
			
			Outputs["bitmap"].Value = file.Bitmap;
			if( file.Bitmap != null ) {
				Outputs["width"].Value = (ushort)file.Bitmap.Width;
				Outputs["height"].Value = (ushort)file.Bitmap.Height;
			}
			else {
				Outputs["width"].Value = null;
				Outputs["height"].Value = null;
			}
		}
	}
}
