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
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Globalization;

using IGE;
using IGE.IO;

namespace IGE.Data.Articy {
	/// <summary>
	/// </summary>
	public class Project {
		protected Dictionary<ulong, IArticyObject> m_Objects = new Dictionary<ulong, IArticyObject>();
		protected Dictionary<string, Entity> m_EntitiesByExternalId = new Dictionary<string, Entity>();
		protected Dictionary<string, Asset> m_AssetsByExternalId = new Dictionary<string, Asset>();
		
		protected Dictionary<string, ArticyFlowObject> m_FlowObjects = new Dictionary<string, ArticyFlowObject>();
		protected List<ArticyFlowObject> m_RootFlowObjects = new List<ArticyFlowObject>();
		protected Dictionary<ulong, FlowObjectPin> m_FlowObjectPins = new Dictionary<ulong, FlowObjectPin>();
		protected List<ArticyFlowConnection> m_FlowConnections = new List<ArticyFlowConnection>();
		
		public IArticyObject this[ulong id] {
			get { return m_Objects[id]; }
		}
		
		public IList<ArticyFlowConnection> FlowConnections { get { return m_FlowConnections.AsReadOnly(); } }
		
		public IEnumerable<IArticyObject> EnumerateObjects() {
			return m_Objects.Values;
		}
		
		public Entity GetEntity(string externalId) {
			return m_EntitiesByExternalId[externalId];
		}
		
		public T GetEntity<T>(string externalId) where T : Entity {
			return (T)m_EntitiesByExternalId[externalId];
		}
		
		public Asset GetAsset(string externalId) {
			return m_AssetsByExternalId[externalId];
		}
		
		public T GetAsset<T>(string externalId) where T : Asset {
			return (T)m_AssetsByExternalId[externalId];
		}
		
		public ArticyFlowObject GetFlowObject(string externalId) {
			return m_FlowObjects[externalId];
		}
		
		/// <summary>
		/// Loads articy project from an exported XML file.
		/// 
		/// If the project has any templates then the executable file must have a namespace, which contains
		/// classes that will be instantiated for every template type. The class name is decided from template's
		/// technical name. For instance: you have a template "Main Character Template" with a technical name
		/// "DefaultMainCharacterTemplate", you also have a namespace MyGame.Templates, where all template classes
		/// are located, then when articy tries to add an entity which has that template assigned, it will
		/// try to create an instance of MyGame.Templates.DefaultMainCharacterTemplate (instead of
		/// IGE.Data.Articy.ArticyEntity) and store it in project's object dictionary. In this case
		/// MyGame.Templates.DefaultMainCharacterTemplate class MUST extend IGE.Data.Articy.ArticyEntity class or
		/// at least implement IGE.Data.Articy.IArticyObject interface, but in later case it won't be added to the entity
		/// dictionary, which means you won't be able to get that entity by it's external id using GetEntity() method. 
		/// </summary>
		/// <param name="projectFileName">Path to the XML to load</param>
		/// <param name="templateNamespace">Namespace FQN in entry assembly, where all classes corresponding to the templates in loaded project are located.</param>
		public Project(string projectFileName, string templateNamespace) {
			StructuredTextFile project = GameFile.LoadFile<StructuredTextFile>(projectFileName);
			if( !project.Root.Attributes["xmlns"].Equals("http://www.nevigo.com/schemas/articydraft/2.2/XmlContentExport_FullProject.xsd") )
				throw new UserFriendlyException(
					String.Format("Loaded file '{0}' is either not an articy full project export or it's version differs from supported. Currently only version 2.2 is supported.", projectFileName),
					"One of data files is either corupt or has a version that is not supported by the game engine."
				);
			
			foreach( DomNode node in project.Root ) {
				switch( node.Name.ToLower() ) {
					case "content": LoadContent(projectFileName, node, templateNamespace); break;
					case "hierarchy": LoadHierarchy(projectFileName, node); break;
				}
			}

			// Run OnLoadFinished() for all templated objects
			foreach( IArticyObject loadedObj in m_Objects.Values ) {
				loadedObj.OnLoadFinished();
			}
		}
		
		private void LoadContent(string projectFileName, DomNode content, string templateNamespace) {
			IArticyObject obj;
			templateNamespace = templateNamespace.TrimEnd('.');
			int step;
			List<ObjectAndNode>[] stepData = new List<ObjectAndNode>[3];
			for( step = stepData.Length - 1; step >= 0; step-- )
				stepData[step] = new List<ObjectAndNode>();
			
			// First pass
			// Load all objects but only with their basic information: Id, TechnicalName and ExternalId
			foreach( DomNode node in content ) {
				obj = null;
				switch( node.Name.ToLower() ) {
					// First step
					case "featuredefinition": step = 0; obj = new FeatureDefinition(node.Attributes["Id"]); break;
					case "enumerationpropertydefinition": step = 0; obj = new EnumPropertyDefinition(node.Attributes["Id"]); break;

					// Second step
					case "asset": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(Asset)); break;
					case "entity": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(Entity)); break;
					case "flowfragment": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(ArticyFlowFragment)); break;
					case "dialogue": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(ArticyDialogue)); break;
					case "dialoguefragment": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(ArticyDialogueFragment)); break;
					case "hub": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(ArticyHub)); break;
					case "jump": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(Jump)); break;
					case "instruction": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(ArticyInstruction)); break;
					case "condition": step = 1; obj = CreateTemplatedObjectInstance(projectFileName, node, templateNamespace, typeof(ArticyCondition)); break;
					
					// Third step
					case "connection": step = 2; obj = new ArticyFlowConnection(node.Attributes["Id"]); break;
				}
				
				if( obj != null ) {
					LoadBasicObjectInformation(obj, node, projectFileName);
					m_Objects.Add(obj.Id, obj);
					if( obj is ArticyFlowObject && !string.IsNullOrEmpty(obj.ExternalId) )
						m_FlowObjects.Add(obj.ExternalId, (ArticyFlowObject)obj);
					if( obj is ArticyFlowConnection )
						m_FlowConnections.Add((ArticyFlowConnection)obj);
					stepData[step].Add(new ObjectAndNode { Object = obj, Node = node });
				}
			}

			// Second pass
			// Load all objects according to their load priority
			foreach( List<ObjectAndNode> stepList in stepData ) {
				foreach( ObjectAndNode objectAndNode in stepList ) {
					obj = objectAndNode.Object;
					DomNode node = objectAndNode.Node;
					switch( node.Name.ToLower() ) {
						case "featuredefinition": LoadFeatureDefinition((FeatureDefinition)obj, node); break;
						case "enumerationpropertydefinition": LoadEnumPropertyDefinition((EnumPropertyDefinition)obj, node); break;
						
						case "entity": LoadEntity((Entity)obj, node, projectFileName); break;
						case "asset": LoadAsset((Asset)obj, node, projectFileName); break;
						case "flowfragment": LoadFlowFragment((ArticyFlowFragment)obj, node, projectFileName); break;
						case "dialogue": LoadDialogue((ArticyDialogue)obj, node, projectFileName); break;
						case "dialoguefragment": LoadDialogueFragment((ArticyDialogueFragment)obj, node, projectFileName); break;
						case "hub": LoadHub((ArticyHub)obj, node, projectFileName); break;
						case "jump": LoadJump((Jump)obj, node, projectFileName); break;
						case "instruction": LoadInstruction((ArticyInstruction)obj, node, projectFileName); break;
						case "condition": LoadCondition((ArticyCondition)obj, node, projectFileName); break;

						case "connection": LoadConnection((ArticyFlowConnection)obj, node, projectFileName); break;
					}
				}
			}
		}
		
		private void LoadHierarchy(string projectFileName, DomNode data) {
			IArticyObject parent = null;
			IArticyObject flowObject;
			bool loadChildren;
			
			if( data.Name.Equals("Node", StringComparison.OrdinalIgnoreCase) ) {
				switch( data.Attributes["Type"].ToLower() ) {
					case "dialogue": goto case "flowfragment";
					case "flowfragment":
						if( !m_Objects.TryGetValue(ParseHexValue(data.Attributes["Id"]), out parent) )
							throw new UserFriendlyException(
								String.Format("Cannot load articy project '{0}': object hierarchy contains a '{1}' node that references a non existing object id '{2}'.", projectFileName, data.Attributes["Type"], data.Attributes["Id"]),
								"One of data files is either corupt or has a version that is not supported by the game engine."
							);
						break;
				}
			}
			else if( !data.Name.Equals("Hierarchy", StringComparison.OrdinalIgnoreCase) )
				throw new UserFriendlyException(
					String.Format("Cannot load articy project '{0}': object hierarchy contains an unrecognized node '{1}'.", projectFileName, data.Name),
					"One of data files is either corupt or has a version that is not supported by the game engine."
				);
			
			foreach( DomNode child in data ) {
				if( !child.Name.Equals("Node", StringComparison.OrdinalIgnoreCase) )
					throw new UserFriendlyException(
						String.Format("Cannot load articy project '{0}': object hierarchy contains an unrecognized node '{1}'.", projectFileName, child.Name),
						"One of data files is either corupt or has a version that is not supported by the game engine."
					);
				
				loadChildren = false;
				flowObject = null;
				
				switch( child.Attributes["Type"].ToLower() ) {
					case "project": loadChildren = true; break;
					case "flow": loadChildren = true; break;
				
					case "dialogue": goto case "jump";
					case "dialoguefragment": goto case "jump";
					case "flowfragment": goto case "jump";
					case "condition": goto case "jump";
					case "instruction": goto case "jump";
					case "hub": goto case "jump";
					case "jump":
						if( !m_Objects.TryGetValue(ParseHexValue(child.Attributes["Id"]), out flowObject) )
							throw new UserFriendlyException(
								String.Format("Cannot load articy project '{0}': object hierarchy contains a '{1}' node that references a non existing object id '{2}'.", projectFileName, child.Attributes["Type"], child.Attributes["Id"]),
								"One of data files is either corupt or has a version that is not supported by the game engine."
							);
						break;
				}
				
				if( flowObject != null ) {
					if( !(flowObject is ArticyFlowObject) )
						throw new UserFriendlyException(
							String.Format("Cannot load articy project '{0}': object hierarchy contains a '{1}' node that references an object '{2}' which is not compatible with articy flow objects.", projectFileName, child.Attributes["Type"], child.Attributes["Id"]),
							"One of data files is either corupt or has a version that is not supported by the game engine."
						);

					if( parent != null )
						((ArticyFlowObject)parent).AddChild((ArticyFlowObject)flowObject);
					else
						m_RootFlowObjects.Add((ArticyFlowObject)flowObject);
					
					loadChildren = true;
				}
				
				if( loadChildren )
					LoadHierarchy(projectFileName, child);
			}
		}
		
		protected IArticyObject CreateTemplatedObjectInstance(string projectFileName, DomNode node, string templateNamespace, Type baseTemplateType ) {
			Type entityTemplate;
			if( node.Attributes["ObjectTemplateReferenceName"].Equals("") ) {
				/* throw new UserFriendlyException(
					String.Format("Cannot load articy project '{0}': object '{1}' has no 'ObjectTemplateReferenceName' attribute.", projectFileName, node.Attributes["Id"]),
					"One of data files is either corupt or has a version that is not supported by the game engine."
				); */
				entityTemplate = baseTemplateType;
			}
			else
				entityTemplate = Assembly.GetEntryAssembly().GetType(String.Format("{0}.{1}", templateNamespace, node.Attributes["ObjectTemplateReferenceName"]), false, true);
			
			if( entityTemplate == null )
				throw new UserFriendlyException(
					String.Format("Cannot load articy project '{0}': object '{1}' has an 'ObjectTemplateReferenceName' attribute set to class name '{2}' that does not exist in '{3}' namespace.", projectFileName, node.Attributes["Id"], node.Attributes["ObjectTemplateReferenceName"], templateNamespace),
					"One of data files is either corupt or has a version that is not supported by the game engine."
				);
			// asy.CreateInstance(String.Format("{0}.{1}", templateNamespace, node.Attributes["ObjectTemplateReferenceName"]), true, BindingFlags.Default, null, new object[] {node.Attributes["Id"]},
			return (IArticyObject)Activator.CreateInstance(entityTemplate, node.Attributes["Id"]);
		}

		protected void LoadBasicObjectInformation(IArticyObject obj, DomNode data, string projectFileName) {
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "technicalname":
						obj.TechnicalName = node.Value;
						break;
					
					case "externalid":
						obj.ExternalId = node.Value;
						if( (obj is Entity) && !m_EntitiesByExternalId.ContainsKey(obj.ExternalId) )
							m_EntitiesByExternalId.Add(obj.ExternalId, (Entity)obj);
						else if( (obj is Asset) && !m_AssetsByExternalId.ContainsKey(obj.ExternalId) )
							m_AssetsByExternalId.Add(obj.ExternalId, (Asset)obj);
						break;
				}
			}
		}

		
		protected void LoadEntity(Entity obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
		}
		
		protected void LoadAsset(Asset obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			// TODO: load paths?
		}
		
		protected void LoadFlowFragment(ArticyFlowFragment obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "text":
						obj.Text = LoadString(node);
						break;
				}
			}
		}
		
		protected void LoadDialogue(ArticyDialogue obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "text":
						obj.Text = LoadString(node);
						break;
				}
			}
		}
		
		protected void LoadDialogueFragment(ArticyDialogueFragment obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "text":
						obj.Text = LoadString(node);
						break;
						
					case "menutext":
						obj.MenuText = LoadString(node);
						break;
						
					case "stagedirections":
						obj.StageDirections = LoadString(node);
						break;
						
					case "speaker":
						if( !node.Attributes["IdRef"].Equals("") ) {
							if( !m_Objects.TryGetValue(ParseHexValue(node.Attributes["IdRef"]), out obj.Speaker) )
								throw new UserFriendlyException(
									String.Format("Cannot load articy project '{0}': dialogue fragment '0x{1:X16}' contains a '{2}' IdRef to an object '0x{3:X16}' that does not exist.", projectFileName, obj.Id, node.Name, node.Attributes["IdRef"]),
									"One of data files is either corupt or has a version that is not supported by the game engine."
								);
						}
						break;
				}
			}
		}
		
		protected void LoadHub(ArticyHub obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
		}
		
		protected void LoadJump(Jump obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "target":
						if( !node.Attributes["IdRef"].Equals("") ) {
							IArticyObject target;
							if( !m_Objects.TryGetValue(ParseHexValue(node.Attributes["IdRef"]), out target) )
								throw new UserFriendlyException(
									String.Format("Cannot load articy project '{0}': jump '0x{1:X16}' contains a '{2}' IdRef to an object '0x{3:X16}' that does not exist.", projectFileName, obj.Id, node.Name, node.Attributes["IdRef"]),
									"One of data files is either corupt or has a version that is not supported by the game engine."
								);
							if( !(target is ArticyFlowObject) )
								throw new UserFriendlyException(
									String.Format("Cannot load articy project '{0}': jump '0x{1:X16}' contains a '{2}' IdRef to an object '0x{3:X16}' that is not a flow object.", projectFileName, obj.Id, node.Name, node.Attributes["IdRef"]),
									"One of data files is either corupt or has a version that is not supported by the game engine."
								);
								
							obj.Target = (ArticyFlowObject)target;
							if( !node.Attributes["PinRef"].Equals("") ) {
								if( !m_FlowObjectPins.TryGetValue(ParseHexValue(node.Attributes["PinRef"]), out obj.TargetPin) )
									throw new UserFriendlyException(
										String.Format("Cannot load articy project '{0}': jump '0x{1:X16}' contains a '{2}' PinRef to a pin '0x{3:X16}' that does not exist.", projectFileName, obj.Id, node.Name, node.Attributes["PinRef"]),
										"One of data files is either corupt or has a version that is not supported by the game engine."
									);
							}
						}
						break;
				}
			}
		}
		
		protected void LoadCondition(ArticyCondition obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "expression":
						obj.Script = LoadString(node);
						break;
				}
			}
		}
		
		protected void LoadInstruction(ArticyInstruction obj, DomNode data, string projectFileName) {
			LoadTemplatedObject(obj, data, projectFileName);
			LoadFlowObject(obj, data, projectFileName);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "expression":
						obj.Script = LoadString(node);
						break;
				}
			}
		}
		
		protected void LoadConnection(ArticyFlowConnection obj, DomNode data, string projectFileName) {
			FlowObjectPin pin;
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "label":
						obj.Label = LoadString(node);
						break;
					
					case "source": goto case "target";
					case "target":
						if( !m_FlowObjectPins.TryGetValue(ParseHexValue(node.Attributes["PinRef"]), out pin) )
							throw new UserFriendlyException(
								String.Format("Cannot load articy project '{0}': connection '0x{1:X16}' contains a '{2}' PinRef to a pin '0x{3:X16}' that does not exist.", projectFileName, obj.Id, node.Name, node.Attributes["PinRef"]),
								"One of data files is either corupt or has a version that is not supported by the game engine."
							);

						if( node.Name.Equals("source", StringComparison.OrdinalIgnoreCase) )
							obj.Source = pin; 
						else
							obj.Target = pin;
						break;
				}
			}
			if( obj.Source == null )
				throw new UserFriendlyException(
					String.Format("Cannot load articy project '{0}': connection '0x{1:X16}' does not have a source.", projectFileName, obj.Id),
					"One of data files is either corupt or has a version that is not supported by the game engine."
				);
			if( obj.Target == null )
				throw new UserFriendlyException(
					String.Format("Cannot load articy project '{0}': connection '0x{1:X16}' does not have a target.", projectFileName, obj.Id),
					"One of data files is either corupt or has a version that is not supported by the game engine."
				);
		}
		
		protected string LoadString(DomNode node) {
			if( node.Children.Count == 0 )
				return node.Value;
			
			string result = "";
			foreach( DomNode langNode in node ) {
				result = langNode.Value;
				break;
			}
			if( !result.Equals("") && node.Attributes["HasMarkup"].ToBoolean(false) ) {
				// TODO: parse markup into the text
			}
			return result;
		}
		
		protected List<IArticyObject> LoadReferences(DomNode node) {
			ulong id;
			IArticyObject refObj;
			List<IArticyObject> references = new List<IArticyObject>();
			foreach( DomNode refNode in node ) {
				if( !refNode.Name.Equals("reference", StringComparison.OrdinalIgnoreCase) )
					continue;
				id = ParseHexValue(refNode.Attributes["IdRef"]);
				if( !m_Objects.TryGetValue(id, out refObj) )
					throw new UserFriendlyException(
						String.Format("Could not resolve a reference to an object '0x{0:X16}'.", id),
						"One of data files is either corupt or has a version that is not supported by the game engine."
					);
				references.Add(refObj);
			}
			return references;
		}
		
		protected void LoadTemplatedObject(IArticyTemplatedObject obj, DomNode data, string projectFileName) {
			Type type;
			MemberInfo[] members;
			MemberInfo member, featureMember;
			bool isPropertyMember;
			bool isSubMember;
			ulong id;
			IArticyObject refObj;
			object targetObject;
			string propertyName;
			string featureName;
			string propertyFullName;
			
			// members = type.GetMember("", MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty);
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "displayname":
						obj.DisplayName = LoadString(node);
						break;
					
					case "references":
						if( obj is IHasReferences )
							((IHasReferences)obj).References = LoadReferences(node);
						break;
					
					case "features":
						foreach( DomNode feature in node ) {
							if( !feature.Name.Equals("Feature", StringComparison.OrdinalIgnoreCase) )
								continue;
							// Feature["IdRef"] points to FeatureDefinition
							FeatureDefinition featureDef = (FeatureDefinition)m_Objects[ParseHexValue(feature.Attributes["IdRef"])];

							targetObject = obj;
							type = targetObject.GetType();
							isSubMember = false;

							featureName = feature.Attributes["Name"];
							members = type.GetMember(featureName, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.GetField | BindingFlags.GetProperty); // | BindingFlags.FlattenHierarchy
							featureMember = null;
							isPropertyMember = false;
							foreach (MemberInfo m in members) {
								if( featureMember == null || (!isPropertyMember && m.MemberType == MemberTypes.Property) ) {
									featureMember = m;
									isPropertyMember = (m.MemberType == MemberTypes.Property);
								}
							}
							
							if( featureMember != null ) {
								isSubMember = true;
								targetObject = GetPropertyOrField(obj, featureMember);
								type = targetObject.GetType();
							}
							else
								isSubMember = false;
							
							foreach( DomNode properties in feature ) {
								if( !properties.Name.Equals("Properties", StringComparison.OrdinalIgnoreCase) )
									continue;
								foreach( DomNode property in properties ) {
									propertyName = property.Attributes["Name"];
									propertyFullName = String.Format("{0}.{1}", featureName, propertyName);
										
									members = type.GetMember(propertyName, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty); // | BindingFlags.FlattenHierarchy
									member = null;
									isPropertyMember = false;
									foreach (MemberInfo m in members) {
										if( member == null || (!isPropertyMember && m.MemberType == MemberTypes.Property) ) {
											member = m;
											isPropertyMember = (m.MemberType == MemberTypes.Property);
										}
									}
									
									if( member == null ) {
										GameDebugger.Log(LogLevel.Warning, "Could not find property/field '{0}' or '{1}' in '{2}' while loading articy object '0x{3:X16}'.", propertyFullName, propertyName, type.FullName, obj.Id);
										continue;
									}
									
									switch( property.Name.ToLower() ) {
										case "boolean": // value is property.Value (0, 1 or "")
											if( !property.Value.Equals("") )
												SetPropertyOrField(targetObject, member, property.Value);
											break;
										case "number": // value is property.Value
											if( !property.Value.Equals("") )
												SetPropertyOrField(targetObject, member, property.Value);
											break;
										case "string": // both - text and script are treated as a string. strings may be multilingual
											SetPropertyOrField(targetObject, member, property.Value);
											break;
										case "namedreference": // attribute "IdRef"
											id = property.Attributes["IdRef"].Equals("") ? 0 : ParseHexValue(property.Attributes["IdRef"]);
											if( id != 0 ) {
												if( !m_Objects.TryGetValue(id, out refObj) )
													throw new UserFriendlyException(
														String.Format("Cannot load articy project '{0}': object '0x{1:X16}' contains a reference to another object '0x{3:X16}' that does not exist in property '{2}'.", projectFileName, obj.Id, propertyFullName, id),
														"One of data files is either corupt or has a version that is not supported by the game engine."
													);
											}
											else
												refObj = null;
											SetPropertyOrField(targetObject, member, refObj);
											break;
										case "references": // has subnodes with "IdRef" attributes
											List<IArticyObject> references;
											try {
												references = LoadReferences(property);
											}
											catch(Exception ex) {
												throw new UserFriendlyException(
													String.Format("Cannot load articy project '{0}': object '0x{1:X16}' contains one or more invalid references in property '{2}'.", projectFileName, obj.Id, propertyFullName),
													"One of data files is either corupt or has a version that is not supported by the game engine.",
													ex
												);
											}
											try {
												SetPropertyOrField(targetObject, member, references);
											}
											catch(ArgumentException) {
												try {
													List<ulong> idReferences = new List<ulong>();
													foreach( IArticyObject reference in references )
														idReferences.Add(reference.Id);
													SetPropertyOrField(targetObject, member, idReferences);
												}
												catch(ArgumentException) {
													List<string> stringReferences = new List<string>();
													foreach( IArticyObject reference in references ) {
														if( string.IsNullOrWhiteSpace(reference.ExternalId) ) {
															throw new UserFriendlyException(
																String.Format("Cannot load articy project '{0}': object '0x{1:X16}' contains a reference in property '{2}' to an object '0x{3:X16}' which has no external Id.", projectFileName, obj.Id, propertyFullName, reference.Id),
																"One of data files is either corupt or has a version that is not supported by the game engine."
															);
														}
														stringReferences.Add(reference.ExternalId);
													}
													SetPropertyOrField(targetObject, member, stringReferences);
												}
											}
											break;
										case "enum": // value is numeric, not the technical name set for drop down option. must get list of options from the property definition.
											if( property.Value.Equals("") )
												break;
											PropertyDefinition propertyDef = null;
											if( !featureDef.Properties.TryGetValue(propertyName, out propertyDef) )
												throw new UserFriendlyException(
													String.Format("Cannot load articy project '{0}': object '0x{1:X16}' contains a reference to a enum property '{2}' that does not exist.", projectFileName, obj.Id, propertyFullName),
													"One of data files is either corupt or has a version that is not supported by the game engine."
												);
											EnumPropertyDefinition enumPropertyDef = (EnumPropertyDefinition)propertyDef;
											int val = int.Parse(property.Value);
											string option = null;
											bool valueSet = false;
											while( enumPropertyDef != null ) {
												if( enumPropertyDef.Options.TryGetValue(val, out option) ) {
													SetPropertyOrField(targetObject, member, property.Value, option);
													valueSet = true;
													break;
												}
												enumPropertyDef = enumPropertyDef.BasedOn;
											}
											if( !valueSet )
												throw new UserFriendlyException(
													String.Format("Cannot load articy project '{0}': object '0x{1:X16}' has a value '{3}' that is not among options of enum property '{2}'.", projectFileName, obj.Id, propertyFullName, property.Value),
													"One of data files is either corupt or has a version that is not supported by the game engine."
												);
											break;
									}
								}
							}
							
							if( isSubMember && !type.IsByRef ) // This was a structure and we got a copy of it. This means we have to put it back, otherwise data won't be updated. 
								SetPropertyOrField(obj, featureMember, targetObject);
						}
						break;
				}
			}
		}

		protected void LoadFlowObject(ArticyFlowObject obj, DomNode data, string projectFileName) {
			foreach( DomNode node in data ) {
				switch( node.Name.ToLower() ) {
					case "pins":
						GameDebugger.Log(LogLevel.Debug, "Flow object '0x{0:X16}' has {1} pin nodes", obj.Id, node.Children.Count);
						GameDebugger.Log(LogLevel.Debug, "Before loading flow object has {0} input pins and {1} output pins", obj.Inputs.Count, obj.Outputs.Count);
						foreach( DomNode pin in node ) {
							if( !pin.Name.Equals("pin", StringComparison.OrdinalIgnoreCase) )
								continue;
							FlowObjectPin graphPin;
							if( pin.Attributes["semantic"].Equals("input", StringComparison.OrdinalIgnoreCase) ) {
								graphPin = new FlowObjectPin(pin.Attributes["id"], GraphPin.PinType.Input);
							}
							else if( pin.Attributes["semantic"].Equals("output", StringComparison.OrdinalIgnoreCase) ) {
								graphPin = new FlowObjectPin(pin.Attributes["id"], GraphPin.PinType.Output);
							}
							else
								throw new UserFriendlyException(
									String.Format("Cannot load articy project '{0}': object '0x{1:X16}' has a pin '0x{2:X16}' of unrecognized type '{3}'.", projectFileName, obj.Id, pin.Attributes["id"], pin.Attributes["semantic"]),
									"One of data files is either corupt or has a version that is not supported by the game engine."
								);
							
							foreach( DomNode pinChildNode in pin ) {
								if( !pinChildNode.Name.Equals("expression", StringComparison.OrdinalIgnoreCase) )
									continue;
								graphPin.Script = LoadString(pinChildNode);
							}
							
							if( graphPin.Type == GraphPin.PinType.Input) {
								GameDebugger.Log(LogLevel.Debug, "Adding input pin '{0}' of type '{1}'", pin.Attributes["id"], graphPin.GetType().Name);
								obj.AddInput(graphPin);
							}
							else {
								GameDebugger.Log(LogLevel.Debug, "Adding output pin '{0}' of type '{1}'", pin.Attributes["id"], graphPin.GetType().Name);
								obj.AddOutput(graphPin);
							}
							
							m_FlowObjectPins.Add(graphPin.Id, graphPin);
						}
						GameDebugger.Log(LogLevel.Debug, "After loading flow object has {0} input pins and {1} output pins", obj.Inputs.Count, obj.Outputs.Count);
						break;
				}
			}
		}
		
		protected void LoadFeatureDefinition(FeatureDefinition feature, DomNode data) {
			// FeatureDefinition->PropertyDefinitions->PropertyDefinitionRef["IdRef"] points to the property

			foreach( DomNode fchild in data ) {
				switch( fchild.Name.ToLower() ) {
					case "propertydefinitions":
						ulong propertyId;
						PropertyDefinition property;
						
						foreach( DomNode definition in fchild ) {
							if( !definition.Name.Equals("propertydefinitionref", StringComparison.OrdinalIgnoreCase) )
								continue;
							propertyId = ParseHexValue(definition.Attributes["IdRef"]);
							if( !m_Objects.ContainsKey(propertyId) )
								continue; // ignore all properties are not loaded since we don't support all of them 
							property = (PropertyDefinition)m_Objects[propertyId];
							feature.Properties.Add(property.TechnicalName, property);
						}
						break;
				}
			}
		}
		
		protected void LoadEnumPropertyDefinition(EnumPropertyDefinition property, DomNode data) {
			// Same property copies in the template are created as "virtual" properties that are based on the property that is being cloned
			// EnumerationPropertyDefinition["BasedOn"] points to EnumerationPropertyDefinition that the current property extends
			// EnumerationPropertyDefinition->TechnicalName.Value contains name that is used in entity
			// EnumerationPropertyDefinition->Values->EnumValue is a definition of an option in drop down list
			//     ->Value.Value contains numeric value of the option
			//     ->TechnicalName.Value contains technical name assigned to the option
			//     ->DisplayName->LocalizedString.Value contains text of an option
			
			if( !data.Attributes["BasedOn"].Equals("") )
				property.BasedOn = (EnumPropertyDefinition)m_Objects[ParseHexValue(data.Attributes["BasedOn"])];
			  
			foreach( DomNode epdchild in data ) {
				switch( epdchild.Name.ToLower() ) {
					case "values":
						foreach( DomNode enumvalue in epdchild ) {
							if( !enumvalue.Name.Equals("enumvalue", StringComparison.OrdinalIgnoreCase) )
								continue;
							int key = 0;
							string val = null;
							foreach( DomNode evchild in enumvalue ) {
								switch( evchild.Name.ToLower() ) {
									case "value": key = int.Parse(evchild.Value); break;
									case "technicalname": val = evchild.Value; break;
								}
							}
							property.Options.Add(key, val);
						}
						break;
				}
			}
		}
		
		#region Helpers
		
		public static ulong ParseHexValue(string val) {
			if( val.StartsWith("0x") )
				return ulong.Parse(val.Substring(2), NumberStyles.HexNumber);
			return ulong.Parse(val, NumberStyles.HexNumber);
		}
		
		public static bool TryParseHexValue(string val, out ulong result) {
			if( val.StartsWith("0x") )
				return ulong.TryParse(val.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
			return ulong.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
		}
		
		protected object ConvertToType(object val, Type type) {
			return ConvertToType(val, type, null);
		}
		
		protected object ConvertToType(object val, Type type, string enumMemberName) {
			if( val != null && !type.IsAssignableFrom(val.GetType()) ) {
				if( val is string ) {
					string v = (string)val;
					if( type.IsEnum ) {
						if( enumMemberName != null ) {
							try {
								return Enum.Parse(type, enumMemberName);
							}
							catch {}
						}
						object vi = ConvertToType(val, type.GetEnumUnderlyingType());
						return Enum.ToObject(type, vi);
					}
					if( type.Equals(typeof(bool)) )
						return !v.Equals("0");
					if( type.Equals(typeof(byte)) )
						return byte.Parse(v);
					if( type.Equals(typeof(sbyte)) )
						return sbyte.Parse(v);
					if( type.Equals(typeof(ushort)) )
						return ushort.Parse(v);
					if( type.Equals(typeof(short)) )
						return short.Parse(v);
					if( type.Equals(typeof(uint)) )
						return uint.Parse(v);
					if( type.Equals(typeof(int)) )
						return int.Parse(v);
					if( type.Equals(typeof(ulong)) )
						return ulong.Parse(v);
					if( type.Equals(typeof(long)) )
						return long.Parse(v);
					if( type.Equals(typeof(float)) )
						return float.Parse(v);
					if( type.Equals(typeof(double)) )
						return double.Parse(v);
					if( type.Equals(typeof(decimal)) )
						return decimal.Parse(v);
					if( type.Equals(typeof(DateTime)) )
						return DateTime.Parse(v);
					if( type.Equals(typeof(TimeSpan)) )
						return TimeSpan.Parse(v);
				}
			}
			return val;
		}
		
		protected void SetPropertyOrField(object instance, MemberInfo member, object val) {
			SetPropertyOrField(instance, member, val, null);
		}
		
		protected void SetPropertyOrField(object instance, MemberInfo member, object val, string enumMemberName) {
			GameDebugger.Log(LogLevel.Debug, "Seting property or field {0}.{1} to value of type {2}", instance.GetType().Name, member.Name, (val == null) ? "NULL" : val.GetType().FullName);
			if( member is PropertyInfo ) {
				PropertyInfo pi = (PropertyInfo)member;
				if( !pi.CanWrite )
					throw new UserFriendlyException(
						String.Format("'{1}' property in '{0}' must not be read only to load an articy project.", instance.GetType().FullName, pi.Name),
						"Game has some bad coding which prevents data from getting loaded."
					);
				pi.SetValue(instance, ConvertToType(val, pi.PropertyType, enumMemberName), BindingFlags.SetProperty, null, null, CultureInfo.CurrentCulture);
			}
			else if( member is FieldInfo ) {
				FieldInfo fi = (FieldInfo)member; 
				if( fi.IsInitOnly )
					throw new UserFriendlyException(
						String.Format("'{1}' field in '{0}' must not be read only to load an articy project.", instance.GetType().FullName, fi.Name),
						"Game has some bad coding which prevents data from getting loaded."
					);
				fi.SetValue(instance, ConvertToType(val, fi.FieldType, enumMemberName));
			}
		}
		
		protected object GetPropertyOrField(object instance, MemberInfo member) {
			if( member is PropertyInfo ) {
				PropertyInfo pi = (PropertyInfo)member;
				if( !pi.CanRead )
					throw new UserFriendlyException(
						String.Format("'{1}' property in '{0}' must not be write only to load an articy project.", instance.GetType().FullName, pi.Name),
						"Game has some bad coding which prevents data from getting loaded."
					);
				return pi.GetValue(instance, BindingFlags.GetProperty, null, null, CultureInfo.CurrentCulture);
			}
			else if( member is FieldInfo ) {
				FieldInfo fi = (FieldInfo)member; 
				return fi.GetValue(instance);
			}
			throw new UserFriendlyException(
				String.Format("Member '{0}' in '{1}' must be either a field or a property", member.Name, instance.GetType().FullName),
				"Game engine has some bad coding which prevents data from getting loaded."
			);
		}
		
		#endregion
		
		private struct ObjectAndNode {
			public IArticyObject Object;
			public DomNode Node;
		}
	}
}
