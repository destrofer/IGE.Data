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
	public class BitmapSaver : Processor {
		public override string Category { get { return "Bitmaps"; } }
		public override string Name { get { return "Bitmap saver"; } }
		public override string Description { get { return "Saves a bitmap from input to the destination file."; } }
	
		public BitmapSaver() {
			Attributes["path"] = new Input("path", "Destination", new Type[] { typeof(string) }, true, "Path to a file, where bitmap should be saved to.");
			
			Inputs["bitmap"] = new Input("bitmap", "Bitmap", new Type[] { typeof(Bitmap) }, true, "Bitmap, saved to the destination file.");
		}
		
		public override void Process() {
			if( Attributes["path"].Value == null )
				throw new UserFriendlyException("Bitmap destination must be set");
			if( ((string)Attributes["path"].Value).Equals("") )
				throw new UserFriendlyException("Bitmap destination must not be empty");
			if( Inputs["bitmap"].Value == null )
				throw new UserFriendlyException("Bitmap saver requires a bitmap on input");
			
			BitmapFile file = new BitmapFile();
			file.Bitmap = (Bitmap)Inputs["bitmap"].Value;
			file.Save((string)Attributes["path"].Value);
		}
	}
}
