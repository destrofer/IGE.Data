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

namespace IGE.Data.Articy {
	/// <summary>
	/// </summary>
	public class ArticyFlowConnection : GraphConnection, IArticyObject {
		private ulong m_Id;
		protected string m_ExternalId;
		protected string m_TechnicalName;
		protected string m_Label;
		
		public ulong Id { get { return m_Id; } }
		public string ExternalId { get { return m_ExternalId; } set { m_ExternalId = value; } }
		public string TechnicalName { get { return m_TechnicalName; } set { m_TechnicalName = value; } }
		public string Label { get { return m_Label; } set { m_Label = value; } }
		
		public ArticyFlowConnection(string id) : base() {
			m_ExternalId = null;
			m_TechnicalName = null;
			m_Id = Project.ParseHexValue(id);
			m_Label = null;
		}
		
		private ArticyFlowConnection() {
		}
		
		public virtual void OnLoadFinished() {
		}
	}
}
