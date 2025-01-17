#define ASTAR_NO_JSON
using System;
using UnityEngine;
using Pathfinding.Serialization.JsonFx;
#if NETFX_CORE
using System.Reflection;
#endif
using System.Collections.Generic;
#if NETFX_CORE && !UNITY_EDITOR
//using MarkerMetro.Unity.WinLegacy.IO;
//using MarkerMetro.Unity.WinLegacy.Reflection;
#endif

#if !ASTAR_NO_JSON

namespace Pathfinding.Serialization
{
	
	public class UnityObjectConverter : JsonConverter {

		public override bool CanConvert (Type type) {
#if NETFX_CORE
			return typeof(UnityEngine.Object).GetTypeInfo().IsAssignableFrom (type.GetTypeInfo());
#else
			return typeof(UnityEngine.Object).IsAssignableFrom (type);
#endif
		}
		
		public override object ReadJson ( Type objectType, Dictionary<string,object> values) {
			
			if (values == null) return null;
			
			string name = (string)values["Name"];

			if ( name == null ) return null;

			string typename = (string)values["Type"];
			Type type = Type.GetType (typename);
			
			if (System.Type.Equals (type, null)) {
				Debug.LogError ("Could not find type '"+typename+"'. Cannot deserialize Unity reference");
				return null;
			}
			
			if (values.ContainsKey ("GUID")) {
				string guid = (string)values["GUID"];
				
				var helpers = UnityEngine.Object.FindObjectsOfType(typeof(UnityReferenceHelper)) as UnityReferenceHelper[];
				
				for (int i=0;i<helpers.Length;i++) {
					if (helpers[i].GetGUID () == guid) {
						if (System.Type.Equals ( type, typeof(GameObject) )) {
							return helpers[i].gameObject;
						} else {
							return helpers[i].GetComponent (type);
						}
					}
				}
				
			}
			
			//Try to load from resources
			UnityEngine.Object[] objs = Resources.LoadAll (name,type);
			
			for (int i=0;i<objs.Length;i++) {
				if (objs[i].name == name || objs.Length == 1) {
					return objs[i];
				}
			}
			
			return null;
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			var obj = (UnityEngine.Object)value;
			
			
			var dict = new Dictionary<string, object>();


			if ( value == null ) {
				dict.Add ("Name",null);
				return dict;
			}

			dict.Add ("Name",obj.name);

			dict.Add ("Type",obj.GetType().AssemblyQualifiedName);
			
			//Write scene path if the object is a Component or GameObject
			var component = value as Component;
			var go = value as GameObject;
			
			if (component != null || go != null) {
				if (component != null && go == null) {
					go = component.gameObject;
				}
				
				var helper = go.GetComponent<UnityReferenceHelper>();
				
				if (helper == null) {
					Debug.Log ("Adding UnityReferenceHelper to Unity Reference '"+obj.name+"'");
					helper = go.AddComponent<UnityReferenceHelper>();
				}
				
				//Make sure it has a unique GUID
				helper.Reset ();
				
				dict.Add ("GUID",helper.GetGUID ());
			}
			return dict;
		}
	}
	
	public class GuidConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return System.Type.Equals ( type, typeof(Pathfinding.Util.Guid) );
		}
		
		public override object ReadJson ( Type objectType, Dictionary<string,object> values) {
			var s = (string)values["value"];
			return new Pathfinding.Util.Guid(s);
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			var m = (Pathfinding.Util.Guid)value;
			return new Dictionary<string, object> {{"value",m.ToString()}};
		}
	}
	
	public class MatrixConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return System.Type.Equals ( type, typeof(Matrix4x4) );
		}
		
		public override object ReadJson ( Type objectType, Dictionary<string,object> values) {
			var m = new Matrix4x4();
			
			var arr = (Array)values["values"];
			if (arr.Length != 16) {
				Debug.LogError ("Number of elements in matrix was not 16 (got "+arr.Length+")");
				return m;
			}

			for (int i=0;i<16;i++) m[i] = System.Convert.ToSingle (arr.GetValue(new [] {i}));
			
			return m;
		}
		
		/** Just a temporary array of 16 floats.
		 * Stores the elements of the matrices temporarily just to avoid
		 * allocating memory for it each time.
		 */
		readonly float[] values = new float[16];
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			var m = (Matrix4x4)value;
			for (int i=0;i<values.Length;i++) values[i] = m[i];
			
			return new Dictionary<string, object> {
				{"values",values}
			};
		}
	}
	
	public class BoundsConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return System.Type.Equals ( type, typeof(Bounds) );
		}
		
		public override object ReadJson ( Type objectType, Dictionary<string,object> values) {
			var b = new Bounds();
			b.center = new Vector3(	CastFloat(values["cx"]),CastFloat(values["cy"]),CastFloat(values["cz"]));
			b.extents = new Vector3(CastFloat(values["ex"]),CastFloat(values["ey"]),CastFloat(values["ez"]));
			return b;
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			var b = (Bounds)value;
			return new Dictionary<string, object> {
				{"cx",b.center.x},
				{"cy",b.center.y},
				{"cz",b.center.z},
				{"ex",b.extents.x},
				{"ey",b.extents.y},
				{"ez",b.extents.z}
			};
		}
	}
	
	public class LayerMaskConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return System.Type.Equals ( type, typeof(LayerMask) );
		}
		
		public override object ReadJson (Type type, Dictionary<string,object> values) {
			return (LayerMask)(int)values["value"];
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			return new Dictionary<string, object> {{"value",((LayerMask)value).value}};
		}
	}
	
	public class VectorConverter : JsonConverter
	{
		public override bool CanConvert (Type type) {
			return System.Type.Equals ( type, typeof(Vector2) ) || System.Type.Equals ( type, typeof(Vector3) )||System.Type.Equals ( type, typeof(Vector4) );
		}
		
		public override object ReadJson (Type type, Dictionary<string,object> values) {
			if (System.Type.Equals ( type, typeof(Vector2) )) {
				return new Vector2(CastFloat(values["x"]),CastFloat(values["y"]));
			} else if (System.Type.Equals ( type, typeof(Vector3) )) {
				return new Vector3(CastFloat(values["x"]),CastFloat(values["y"]),CastFloat(values["z"]));
			} else if (System.Type.Equals ( type, typeof(Vector4) )) {
				return new Vector4(CastFloat(values["x"]),CastFloat(values["y"]),CastFloat(values["z"]),CastFloat(values["w"]));
			} else {
				throw new NotImplementedException ("Can only read Vector2,3,4. Not objects of type "+type);
			}
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value) {
			if (System.Type.Equals ( type, typeof(Vector2) )) {
				var v = (Vector2)value;
				return new Dictionary<string, object> {
					{"x",v.x},
					{"y",v.y}
				};
			} else if (System.Type.Equals ( type, typeof(Vector3) )) {
				var v = (Vector3)value;
				return new Dictionary<string, object> {
					{"x",v.x},
					{"y",v.y},
					{"z",v.z}
				};
			} else if (System.Type.Equals ( type, typeof(Vector4) )) {
				var v = (Vector4)value;
				return new Dictionary<string, object> {
					{"x",v.x},
					{"y",v.y},
					{"z",v.z},
					{"w",v.w}
				};
			}
			throw new NotImplementedException ("Can only write Vector2,3,4. Not objects of type "+type);
		}
	}
	
	/** Enables json serialization of dictionaries with integer keys.
	 */
	public class IntKeyDictionaryConverter : JsonConverter {
		public override bool CanConvert (Type type) {
			return ( System.Type.Equals (type, typeof(Dictionary<int,int>)) || System.Type.Equals (type, typeof(SortedDictionary<int,int>)) );
		}
		
		public override object ReadJson (Type type, Dictionary<string,object> values) {
			var holder = new Dictionary<int, int>();
			
			foreach ( KeyValuePair<string, object> val in values ) {
				holder.Add( System.Convert.ToInt32(val.Key), System.Convert.ToInt32(val.Value) );
			}
			return holder;
		}
		
		public override Dictionary<string,object> WriteJson (Type type, object value ) {
			var holder = new Dictionary<string, object>();
			var d = (Dictionary<int,int>)value;
			
			foreach ( KeyValuePair<int, int> val in d ) {
				holder.Add( val.Key.ToString(), val.Value );
			}
			return holder;
		}
	}
}

#endif