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
using System.Collections.Generic;

namespace IGE.Processors {
	public abstract class Processor {
		protected Dictionary<string, Input> m_Attributes = new Dictionary<string, Input>();
		public Dictionary<string, Input> Attributes {
			get { return m_Attributes; }
		}
		
		protected Dictionary<string, Input> m_Inputs = new Dictionary<string, Input>();
		public Dictionary<string, Input> Inputs {
			get { return m_Inputs; }
		}
		
		protected Dictionary<string, Output> m_Outputs = new Dictionary<string, Output>();
		public Dictionary<string, Output> Outputs {
			get { return m_Outputs; }
		}
		
		protected Dictionary<string, object> m_AttributeSettings = new Dictionary<string, object>();
		public Dictionary<string, object> AttributeSettings {
			get { return m_AttributeSettings; }
		}
		
		public virtual string Category { get { return "Miscellaneous"; } }
		public abstract string Name { get; }
		public virtual string Description { get { return ""; } }
		
		/// <summary>
		/// </summary>
		/// <returns>True if processor output may change even when input does not change</returns>
		public virtual bool Dynamic { get { return false; } }

		/// <summary>
		/// </summary>
		/// <returns>True if processor output has changed during last processing</returns>
		public virtual bool OutputChanged { get { return true; } }
		
		public Processor() {
		}
		
		public abstract void Process();
		
		#region inner AccessPoint class
		public abstract class AccessPoint {
			protected string m_Id;
			public string Id {
				get { return m_Id; }
				set { m_Id = value; }
			}
			
			protected string m_Name;
			public string Name {
				get { return m_Name; }
				set { m_Name = value; }
			}
			
			protected string m_Description;
			public string Description {
				get { return m_Description; }
				set { m_Description = value; }
			}
			
			public AccessPoint(string id, string name, string description) {
				m_Id = id;
				m_Name = name ?? "???";
				m_Description = description ?? "";
			}
		}
		#endregion inner AccessPoint class

		#region inner Input class
		public class Input : AccessPoint {
			protected bool m_Required;
			public virtual bool Required {
				get { return m_Required; }
				set { m_Required = value; }
			}
			
			protected object m_Value = null;
			public virtual object Value {
				get { return m_Value; }
				set {
					if( value != null && !Accepts(value.GetType()) )
						throw new Exception(String.Format("Input \"{0}\" does not accept value of a given type ({1})", m_Name, value.GetType().FullName));
					m_Value = value;
					if( OnValueChange != null )
						OnValueChange(this);
				}
			}
			
			protected Type[] m_AcceptedTypes;
			public virtual Type[] AcceptedTypes {
				get { return m_AcceptedTypes; }
				set { m_AcceptedTypes = value; }
			}
			
			public event ValueChangeEventHandler OnValueChange;
			
			public Input(string id, string name) : this(id, name, null, false, "") {
			}
			
			public Input(string id, string name, string description) : this(id, name, null, false, description) {
			}
			
			public Input(string id, string name, bool required) : this(id, name, null, required, "") {
			}
			
			public Input(string id, string name, bool required, string description) : this(id, name, null, required, description) {
			}
			
			public Input(string id, string name, Type[] acceptedTypes) : this(id, name, acceptedTypes, false, "") {
			}
			
			public Input(string id, string name, Type[] acceptedTypes, string description) : this(id, name, acceptedTypes, false, description) {
			}
			
			public Input(string id, string name, Type[] acceptedTypes, bool required) : this(id, name, acceptedTypes, required, "") {
			}
			
			public Input(string id, string name, Type[] acceptedTypes, bool required, string description) : base(id, name, description) {
				m_AcceptedTypes = acceptedTypes;
				m_Required = required;
			}
			
			public virtual bool Accepts(Type type) {
				if( m_AcceptedTypes == null )
					return true;
				foreach( Type ckType in m_AcceptedTypes )
					if( ckType.IsAssignableFrom(type) )
						return true;
				return false;
			}
			
			public virtual bool Accepts(object obj) {
				if( obj == null && m_Required )
					return false;
				return Accepts(obj.GetType());
			}
			
			public delegate void ValueChangeEventHandler(Input invoker);
		}
		#endregion inner Input class

		#region inner Output class
		public class Output : AccessPoint {
			protected Type m_Type;
			public virtual Type OutputType {
				get { return m_Type; }
				set {
					if( value == null )
						throw new Exception("Output type cannot be null.");
					m_Type = value;
				}
			}
			
			protected object m_Value = null;
			public virtual object Value {
				get { return m_Value; }
				set {
					if( value != null && !m_Type.IsAssignableFrom(value.GetType()) )
						throw new Exception(String.Format("Output \"{0}\" value type ({1}) does not match the one set in OutputType property ({2}).", m_Name, value.GetType().FullName, m_Type.GetType().FullName));
					m_Value = value;
				}
			}
			
			public Output(string id, string name, object initialValue) : this(id, name, initialValue, typeof(object), "") {
			}
			
			public Output(string id, string name, object initialValue, string description) : this(id, name, initialValue, typeof(object), description) {
			}
			
			public Output(string id, string name, object initialValue, Type type) : this(id, name, initialValue, type, "") {
			}
			
			public Output(string id, string name, object initialValue, Type type, string description) : base(id, name, description) {
				OutputType = type;
				Value = initialValue;
			}
		}
		#endregion inner Output class
	}
}
