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

using IGE;

namespace IGE.Data.Articy {
	/// <summary>
	/// </summary>
	public class FlowObjectPin : GraphPin {
		private ulong m_Id;
		protected string m_Script = null;
		
		public ulong Id { get { return m_Id; } }
		public string Script { get { return m_Script; } set { m_Script = value; } }
		
		public FlowObjectPin(string id, PinType type) : base(type) {
			m_Id = Project.ParseHexValue(id);
			m_Script = null;
			m_Type = type;
		}
		
		public virtual void OnLoadFinished() {
		}
		
		public override string ToString() {
			return String.Format("{0} '0x{1:X16}'", GetType().FullName, m_Id);
		}
	}
}
