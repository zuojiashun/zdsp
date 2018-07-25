#define ASTAR_NO_JSON

using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Util;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Serialization;

namespace Pathfinding {
	/**  Base class for all graphs */
	public abstract class NavGraph {

		/** Used to store the guid value
		 * \see NavGraph.guid
		 */
		public byte[] _sguid;

		/** Reference to the AstarPath object in the scene.
		 * Might not be entirely safe to use, it's better to use AstarPath.active
		 */
        public AstarPath active;

        public AstarData astarData;
		/** Used as an ID of the graph, considered to be unique.
		 * \note This is Pathfinding.Util.Guid not System.Guid. A replacement for System.Guid was coded for better compatibility with iOS
		 */
		[JsonMember]
		public Guid guid {
			get {
				if (_sguid == null || _sguid.Length != 16) {
					_sguid = Guid.NewGuid ().ToByteArray ();
				}
				return new Guid (_sguid);
			}
			set {
				_sguid = value.ToByteArray ();
			}
		}

		/** Default penalty to apply to all nodes */
		[JsonMember]
		public uint initialPenalty;

		/** Index of the graph, used for identification purposes */
		public uint graphIndex;

		/** Name of the graph.
		 * Can be set in the unity editor
		 */
		[JsonMember]
		public string name;		

		/** Count nodes in the graph.
		 * Note that this is, unless the graph type has overriden it, an O(n) operation.
		 *
		 * \todo GridGraph should override this
		 */
		public virtual int CountNodes () {
			int count = 0;
			GraphNodeDelegateCancelable del = node => {
				count++;
				return true;
			};
			GetNodes (del);
			return count;
		}

		/** Calls a delegate with all nodes in the graph.
		 * This is the primary way of "looping" through all nodes in a graph.
		 *
		 * This function should not change anything in the graph structure.
		 *
		 * \code
		 * myGraph.GetNodes ((node) => {
		 *     Debug.Log ("I found a node at position " + (Vector3)node.Position);
		 *     return true;
		 * });
		 * \endcode
		 */
		public abstract void GetNodes (GraphNodeDelegateCancelable del);

		/** A matrix for translating/rotating/scaling the graph.
		 * Not all graph generators sets this variable though.
		 *
		 * \note Do not set directly, use SetMatrix
		 *
		 * \note This value is not serialized. It is expected that graphs regenerate this
		 * field after deserialization has completed.
		 */
		public Matrix4x4 matrix = Matrix4x4.identity;

		/** Inverse of \a matrix.
		 *
		 * \note Do not set directly, use SetMatrix
		 *
		 * \see matrix
		 */
		public Matrix4x4 inverseMatrix = Matrix4x4.identity;

        private static Matrix4x4 GetInverse(Matrix4x4 mat)
        {
            var s0 = mat[0, 0] * mat[1, 1] - mat[1, 0] * mat[0, 1];
            var s1 = mat[0, 0] * mat[1, 2] - mat[1, 0] * mat[0, 2];
            var s2 = mat[0, 0] * mat[1, 3] - mat[1, 0] * mat[0, 3];
            var s3 = mat[0, 1] * mat[1, 2] - mat[1, 1] * mat[0, 2];
            var s4 = mat[0, 1] * mat[1, 3] - mat[1, 1] * mat[0, 3];
            var s5 = mat[0, 2] * mat[1, 3] - mat[1, 2] * mat[0, 3];

            var c5 = mat[2, 2] * mat[3, 3] - mat[3, 2] * mat[2, 3];
            var c4 = mat[2, 1] * mat[3, 3] - mat[3, 1] * mat[2, 3];
            var c3 = mat[2, 1] * mat[3, 2] - mat[3, 1] * mat[2, 2];
            var c2 = mat[2, 0] * mat[3, 3] - mat[3, 0] * mat[2, 3];
            var c1 = mat[2, 0] * mat[3, 2] - mat[3, 0] * mat[2, 2];
            var c0 = mat[2, 0] * mat[3, 1] - mat[3, 0] * mat[2, 1];

            // Should check for 0 determinant
            Matrix4x4 res = Matrix4x4.identity;
            var det = (s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0);
            if (det != 0)
            {
                var invdet = 1.0f / det;

                res[0, 0] = (mat[1, 1] * c5 - mat[1, 2] * c4 + mat[1, 3] * c3) * invdet;
                res[0, 1] = (-mat[0, 1] * c5 + mat[0, 2] * c4 - mat[0, 3] * c3) * invdet;
                res[0, 2] = (mat[3, 1] * s5 - mat[3, 2] * s4 + mat[3, 3] * s3) * invdet;
                res[0, 3] = (-mat[2, 1] * s5 + mat[2, 2] * s4 - mat[2, 3] * s3) * invdet;

                res[1, 0] = (-mat[1, 0] * c5 + mat[1, 2] * c2 - mat[1, 3] * c1) * invdet;
                res[1, 1] = (mat[0, 0] * c5 - mat[0, 2] * c2 + mat[0, 3] * c1) * invdet;
                res[1, 2] = (-mat[3, 0] * s5 + mat[3, 2] * s2 - mat[3, 3] * s1) * invdet;
                res[1, 3] = (mat[2, 0] * s5 - mat[2, 2] * s2 + mat[2, 3] * s1) * invdet;

                res[2, 0] = (mat[1, 0] * c4 - mat[1, 1] * c2 + mat[1, 3] * c0) * invdet;
                res[2, 1] = (-mat[0, 0] * c4 + mat[0, 1] * c2 - mat[0, 3] * c0) * invdet;
                res[2, 2] = (mat[3, 0] * s4 - mat[3, 1] * s2 + mat[3, 3] * s0) * invdet;
                res[2, 3] = (-mat[2, 0] * s4 + mat[2, 1] * s2 - mat[2, 3] * s0) * invdet;

                res[3, 0] = (-mat[1, 0] * c3 + mat[1, 1] * c1 - mat[1, 2] * c0) * invdet;
                res[3, 1] = (mat[0, 0] * c3 - mat[0, 1] * c1 + mat[0, 2] * c0) * invdet;
                res[3, 2] = (-mat[3, 0] * s3 + mat[3, 1] * s1 - mat[3, 2] * s0) * invdet;
                res[3, 3] = (mat[2, 0] * s3 - mat[2, 1] * s1 + mat[2, 2] * s0) * invdet;
            }
            return res;
        }

		/** Use to set both matrix and inverseMatrix at the same time */
		public void SetMatrix (Matrix4x4 m) {
			matrix = m;
			//inverseMatrix = m.inverse;
            inverseMatrix = GetInverse(m);
		}

		/** Relocates the nodes in this graph.
		 * Assumes the nodes are already transformed using the "oldMatrix", then transforms them
		 * such that it will look like they have only been transformed using the "newMatrix".
		 * The "oldMatrix" is not required by all implementations of this function though (e.g the NavMesh generator).
		 *
		 * The matrix the graph is transformed with is typically stored in the #matrix field, so the typical usage for this method is
		 * \code
		 * var myNewMatrix = Matrix4x4.TRS (...);
		 * myGraph.RelocateNodes (myGraph.matrix, myNewMatrix);
		 * \endcode
		 *
		 * So for example if you want to move all your nodes in e.g a point graph 10 units along the X axis from the initial position
		 * \code
		 * var graph = AstarPath.astarData.pointGraph;
		 * var m = Matrix4x4.TRS (new Vector3(10,0,0), Quaternion.identity, Vector3.one);
		 * graph.RelocateNodes (graph.matrix, m);
		 * \endcode
		 *
		 * \note For grid graphs it is recommended to use the helper method RelocateNodes which takes parameters for
		 * center and nodeSize (and additional parameters) instead since it is both easier to use and is less likely
		 * to mess up pathfinding.
		 *
		 * \warning This method is lossy, so calling it many times may cause node positions to lose precision.
		 * For example if you set the scale to 0 in one call, and then to 1 in the next call, it will not be able to
		 * recover the correct positions since when the scale was 0, all nodes were scaled/moved to the same point.
		 * The same thing happens for other - less extreme - values as well, but to a lesser degree.
		 *
		 * \version Prior to version 3.6.1 the oldMatrix and newMatrix parameters were reversed by mistake.
		 */
		public virtual void RelocateNodes (Matrix4x4 oldMatrix, Matrix4x4 newMatrix) {

			//Matrix4x4 inv = oldMatrix.inverse;
            Matrix4x4 inv = GetInverse(oldMatrix);

			Matrix4x4 m = newMatrix * inv;

			GetNodes (delegate (GraphNode node) {
				//Vector3 tmp = inv.MultiplyPoint3x4 ((Vector3)nodes[i].position);
				node.position = ((Int3)m.MultiplyPoint ((Vector3)node.position));
				return true;
			});
			SetMatrix (newMatrix);
		}

		/** Returns the nearest node to a position using the default NNConstraint.
		  * \param position The position to try to find a close node to
		  * \see Pathfinding.NNConstraint.None
		  */
		public NNInfo GetNearest (Vector3 position, Vector3? preferredDir) {
			return GetNearest (position, NNConstraint.None, preferredDir);
		}

		/** Returns the nearest node to a position using the specified NNConstraint.
		  * \param position The position to try to find a close node to
		  * \param constraint Can for example tell the function to try to return a walkable node. If you do not get a good node back, consider calling GetNearestForce. */
		public NNInfo GetNearest (Vector3 position, NNConstraint constraint, Vector3? preferredDir) {
			return GetNearest (position, constraint, null, preferredDir);
		}

		/** Returns the nearest node to a position using the specified NNConstraint.
		  * \param position The position to try to find a close node to
		  * \param hint Can be passed to enable some graph generators to find the nearest node faster.
		  * \param constraint Can for example tell the function to try to return a walkable node. If you do not get a good node back, consider calling GetNearestForce. */
		public virtual NNInfo GetNearest (Vector3 position, NNConstraint constraint, GraphNode hint, Vector3? preferredDir) {
			// This is a default implementation and it is pretty slow
			// Graphs usually override this to provide faster and more specialised implementations

			float maxDistSqr = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;            

			float minDist = float.PositiveInfinity;
			GraphNode minNode = null;

			float minConstDist = float.PositiveInfinity;
			GraphNode minConstNode = null;

			// Loop through all nodes and find the closest suitable node
			GetNodes (node => {
				//float dist = (position-(Vector3)node.position).sqrMagnitude;
                Vector3 nodeDir = (position - (Vector3)node.position);
                float dist = nodeDir.sqrMagnitude;

                if (preferredDir != null)
                {
                    nodeDir = nodeDir / dist;
                    if (Vector3.Dot(nodeDir, (Vector3)preferredDir) < 0) //opposite direction                        
                        dist += 100000;
                }

				if (dist < minDist) {
					minDist = dist;
					minNode = node;
				}

				if (dist < minConstDist && dist < maxDistSqr && constraint.Suitable (node)) {
					minConstDist = dist;
					minConstNode = node;
				}
				return true;
			});

			var nnInfo = new NNInfo (minNode);

			nnInfo.constrainedNode = minConstNode;

			if (minConstNode != null) {
				nnInfo.constClampedPosition = (Vector3)minConstNode.position;
			} else if (minNode != null) {
				nnInfo.constrainedNode = minNode;
				nnInfo.constClampedPosition = (Vector3)minNode.position;
			}

			return nnInfo;
		}

		/**
		 * Returns the nearest node to a position using the specified \link Pathfinding.NNConstraint constraint \endlink.
		 * \returns an NNInfo. This method will only return an empty NNInfo if there are no nodes which comply with the specified constraint.
		 */
		public virtual NNInfo GetNearestForce (Vector3 position, NNConstraint constraint, Vector3? preferredDir) {
			return GetNearest (position, constraint, preferredDir);
		}

		/**
		 * This will be called on the same time as Awake on the gameObject which the AstarPath script is attached to. (remember, not in the editor)
		 * Use this for any initialization code which can't be placed in Scan
		 */        
		public virtual void Awake () {            
		}

		/** Function for cleaning up references.
		 * This will be called on the same time as OnDisable on the gameObject which the AstarPath script is attached to (remember, not in the editor).
		 * Use for any cleanup code such as cleaning up static variables which otherwise might prevent resources from being collected.
		 * Use by creating a function overriding this one in a graph class, but always call base.OnDestroy () in that function.
		 * All nodes should be destroyed in this function otherwise a memory leak will arise.
		 */
		public virtual void OnDestroy () {
			//Destroy all nodes
			GetNodes(delegate(GraphNode node) {
				node.Destroy(node.level);
				return true;
			});
		}		

		/** Serializes graph type specific node data.
		 * This function can be overriden to serialize extra node information (or graph information for that matter)
		 * which cannot be serialized using the standard serialization.
		 * Serialize the data in any way you want and return a byte array.
		 * When loading, the exact same byte array will be passed to the DeserializeExtraInfo function.\n
		 * These functions will only be called if node serialization is enabled.\n
		 */
		public virtual void SerializeExtraInfo (GraphSerializationContext ctx) {
		}

		/** Deserializes graph type specific node data.
		 * \see SerializeExtraInfo
		 */
		public virtual void DeserializeExtraInfo (GraphSerializationContext ctx) {
		}

		/** Called after all deserialization has been done for all graphs.
		 * Can be used to set up more graph data which is not serialized
		 */
		public virtual void PostDeserialization () {
		}

#if ASTAR_NO_JSON
		public virtual void SerializeSettings ( GraphSerializationContext ctx ) {

			ctx.writer.Write (guid.ToByteArray());
			ctx.writer.Write (initialPenalty);
			ctx.writer.Write (name ?? "");			

			for ( int i = 0; i < 4; i++ ) {
				for ( int j = 0; j < 4; j++ ) {
					ctx.writer.Write (matrix.GetRow(i)[j]);
				}
			}
		}

		public virtual void DeserializeSettings ( GraphSerializationContext ctx ) {

			guid = new Guid(ctx.reader.ReadBytes (16));
			initialPenalty = ctx.reader.ReadUInt32 ();
            bool open = ctx.reader.ReadBoolean(); //no use
			name = ctx.reader.ReadString();
            bool drawGizmos = ctx.reader.ReadBoolean(); //no use
            bool infoScreenOpen = ctx.reader.ReadBoolean(); //no use

			for ( int i = 0; i < 4; i++ ) {
				Vector4 row = Vector4.zero;
				for ( int j = 0; j < 4; j++ ) {
					row[j] = ctx.reader.ReadSingle ();
				}
				matrix.SetRow (i, row);
			}
		}
#endif	
	}


	/** Handles collision checking for graphs.
	  * Mostly used by grid based graphs */
	[System.Serializable]
	public class GraphCollision {

		/** Collision shape to use.
		  * Pathfinding.ColliderType */
		public ColliderType type = ColliderType.Capsule;

		/** Diameter of capsule or sphere when checking for collision.
		 * 1 equals \link Pathfinding.GridGraph.nodeSize nodeSize \endlink.
		 * If #type is set to Ray, this does not affect anything */
		public float diameter = 1F;

		/** Height of capsule or length of ray when checking for collision.
		 * If #type is set to Sphere, this does not affect anything
		 */
		public float height = 2F;
		public float collisionOffset;

		/** Direction of the ray when checking for collision.
		 * If #type is not Ray, this does not affect anything
		 * \note This variable is not used currently, it does not affect anything
		 */
		public RayDirection rayDirection = RayDirection.Both;

		/** Layer mask to use for collision check.
		 * This should only contain layers of objects defined as obstacles */
		public LayerMask mask;

		/** Layer mask to use for height check. */
		public LayerMask heightMask = -1;

		/** The height to check from when checking height */
		public float fromHeight = 100;

		/** Toggles thick raycast */
		public bool thickRaycast;

		/** Diameter of the thick raycast in nodes.
		 * 1 equals \link Pathfinding.GridGraph.nodeSize nodeSize \endlink */
		public float thickRaycastDiameter = 1;

		/** Make nodes unwalkable when no ground was found with the height raycast. If height raycast is turned off, this doesn't affect anything. */
		public bool unwalkableWhenNoGround = true;

		/** Use Unity 2D Physics API.
		 * \see http://docs.unity3d.com/ScriptReference/Physics2D.html
		 */
		public bool use2D;

		/** Toggle collision check */
		public bool collisionCheck = true;

		/** Toggle height check. If false, the grid will be flat */
		public bool heightCheck = true;

		/** Direction to use as \a UP.
		 * \see Initialize */
		public Vector3 up;

		/** #up * #height.
		 * \see Initialize */
		private Vector3 upheight;

		/** #diameter * scale * 0.5.
		 * Where \a scale usually is \link Pathfinding.GridGraph.nodeSize nodeSize \endlink
		 * \see Initialize */
		private float finalRadius;

		/** #thickRaycastDiameter * scale * 0.5. Where \a scale usually is \link Pathfinding.GridGraph.nodeSize nodeSize \endlink \see Initialize */
		private float finalRaycastRadius;

		/** Offset to apply after each raycast to make sure we don't hit the same point again in CheckHeightAll */
		public const float RaycastErrorMargin = 0.005F;

		/** Sets up several variables using the specified matrix and scale.
		  * \see GraphCollision.up
		  * \see GraphCollision.upheight
		  * \see GraphCollision.finalRadius
		  * \see GraphCollision.finalRaycastRadius
		  */
		public void Initialize (Matrix4x4 matrix, float scale) {
			up = matrix.MultiplyVector (Vector3.up);
			upheight = up*height;
			finalRadius = diameter*scale*0.5F;
			finalRaycastRadius = thickRaycastDiameter*scale*0.5F;
		}

		/** Returns if the position is obstructed.
		 * If #collisionCheck is false, this will always return true.\n
		 */
		public bool Check (Vector3 position) {

			if (!collisionCheck) {
				return true;
			}

			if ( use2D ) {
				switch (type) {
					case ColliderType.Capsule:
						throw new System.Exception ("Capsule mode cannot be used with 2D since capsules don't exist in 2D. Please change the Physics Testing -> Collider Type setting.");
					case ColliderType.Sphere:
						return Physics2D.OverlapCircle (position, finalRadius, mask) == null;
					default:
						return Physics2D.OverlapPoint ( position, mask ) == null;
				}
			}

			position += up*collisionOffset;
			switch (type) {
				case ColliderType.Capsule:
					return !Physics.CheckCapsule (position, position+upheight,finalRadius,mask);
				case ColliderType.Sphere:
					return !Physics.CheckSphere (position, finalRadius,mask);
				default:
					switch (rayDirection) {
						case RayDirection.Both:
							return !Physics.Raycast (position, up, height, mask) && !Physics.Raycast (position+upheight, -up, height, mask);
						case RayDirection.Up:
							return !Physics.Raycast (position, up, height, mask);
						default:
							return !Physics.Raycast (position+upheight, -up, height, mask);
					}
			}
		}

		/** Returns the position with the correct height. If #heightCheck is false, this will return \a position.\n */
		public Vector3 CheckHeight (Vector3 position) {
			RaycastHit hit;
			bool walkable;
			return CheckHeight (position,out hit, out walkable);
		}

		/** Returns the position with the correct height.
		 * If #heightCheck is false, this will return \a position.\n
		  * \a walkable will be set to false if nothing was hit.
		  * The ray will check a tiny bit further than to the grids base to avoid floating point errors when the ground is exactly at the base of the grid */
		public Vector3 CheckHeight (Vector3 position, out RaycastHit hit, out bool walkable) {
			walkable = true;

			if (!heightCheck || use2D ) {
				hit = new RaycastHit ();
				return position;
			}

			if (thickRaycast) {
				var ray = new Ray (position+up*fromHeight,-up);
				if (Physics.SphereCast (ray, finalRaycastRadius,out hit, fromHeight+0.005F, heightMask)) {
					return AstarMath.NearestPoint (ray.origin,ray.origin+ray.direction,hit.point);
				}

				walkable &= !unwalkableWhenNoGround;
			} else {
				// Cast a ray from above downwards to try to find the ground
				if (Physics.Raycast (position+up*fromHeight, -up,out hit, fromHeight+0.005F, heightMask)) {
					return hit.point;
				}

				walkable &= !unwalkableWhenNoGround;
			}
			return position;
		}

		/** Same as #CheckHeight, except that the raycast will always start exactly at \a origin.
		  * \a walkable will be set to false if nothing was hit.
		  * The ray will check a tiny bit further than to the grids base to avoid floating point errors when the ground is exactly at the base of the grid */
		public Vector3 Raycast (Vector3 origin, out RaycastHit hit, out bool walkable) {
			walkable = true;

			if (!heightCheck || use2D ) {
				hit = new RaycastHit ();
				return origin -up*fromHeight;
			}

			if (thickRaycast) {
				var ray = new Ray (origin,-up);
				if (Physics.SphereCast (ray, finalRaycastRadius,out hit, fromHeight+0.005F, heightMask)) {
					return AstarMath.NearestPoint (ray.origin,ray.origin+ray.direction,hit.point);
				}

				walkable &= !unwalkableWhenNoGround;
			} else {
				if (Physics.Raycast (origin, -up,out hit, fromHeight+0.005F, heightMask)) {
					return hit.point;
				}

				walkable &= !unwalkableWhenNoGround;
			}
			return origin -up*fromHeight;
		}

		/** Returns all hits when checking height for \a position.
		  * \warning Does not work well with thick raycast, will only return an object a single time
		  */
		public RaycastHit[] CheckHeightAll (Vector3 position) {

			if (!heightCheck || use2D) {
				var hit = new RaycastHit ();
				hit.point = position;
				hit.distance = 0;
				return new [] {hit};
			}

			if (thickRaycast) {
				return new RaycastHit[0];
			}

			var hits = new List<RaycastHit>();

			bool walkable;
			Vector3 cpos = position + up*fromHeight;
			Vector3 prevHit = Vector3.zero;

			int numberSame = 0;
			while (true) {
				RaycastHit hit;
				Raycast (cpos, out hit, out walkable);
				if (hit.transform == null) { //Raycast did not hit anything
					break;
				}

				//Make sure we didn't hit the same position
				if (hit.point != prevHit || hits.Count == 0) {
					cpos = hit.point - up*RaycastErrorMargin;
					prevHit = hit.point;
					numberSame = 0;

					hits.Add (hit);
				} else {
					cpos -= up*0.001F;
					numberSame++;
					//Check if we are hitting the same position all the time, even though we are decrementing the cpos variable
					if (numberSame > 10) {
						//Debug.LogError ("Infinite Loop when raycasting. Please report this error (arongranberg.com)\n"+cpos+" : "+prevHit);
						break;
					}
				}
			}
			return hits.ToArray ();
		}

		public void SerializeSettings ( GraphSerializationContext ctx ) {
			ctx.writer.Write ((int)type);
			ctx.writer.Write (diameter);
			ctx.writer.Write (height);
			ctx.writer.Write (collisionOffset);
			ctx.writer.Write ((int)rayDirection);
			ctx.writer.Write ((int)mask);
			ctx.writer.Write ((int)heightMask);
			ctx.writer.Write (fromHeight);
			ctx.writer.Write (thickRaycast);
			ctx.writer.Write (thickRaycastDiameter);

			ctx.writer.Write (unwalkableWhenNoGround);
			ctx.writer.Write (use2D);
			ctx.writer.Write (collisionCheck);
			ctx.writer.Write (heightCheck);
		}

		public void DeserializeSettings ( GraphSerializationContext ctx ) {
			type = (ColliderType)ctx.reader.ReadInt32();
			diameter = ctx.reader.ReadSingle ();
			height = ctx.reader.ReadSingle ();
			collisionOffset = ctx.reader.ReadSingle ();
			rayDirection = (RayDirection)ctx.reader.ReadInt32 ();
			mask = (LayerMask)ctx.reader.ReadInt32 ();
			heightMask = (LayerMask)ctx.reader.ReadInt32 ();
			fromHeight = ctx.reader.ReadSingle ();
			thickRaycast = ctx.reader.ReadBoolean ();
			thickRaycastDiameter = ctx.reader.ReadSingle ();

			unwalkableWhenNoGround = ctx.reader.ReadBoolean();
			use2D = ctx.reader.ReadBoolean();
			collisionCheck = ctx.reader.ReadBoolean();
			heightCheck = ctx.reader.ReadBoolean();
		}
	}


	/** Determines collision check shape */
	public enum ColliderType {
		Sphere,		/**< Uses a Sphere, Physics.CheckSphere */
		Capsule,	/**< Uses a Capsule, Physics.CheckCapsule */
		Ray			/**< Uses a Ray, Physics.Linecast */
	}

	/** Determines collision check ray direction */
	public enum RayDirection {
		Up,	 	/**< Casts the ray from the bottom upwards */
		Down,	/**< Casts the ray from the top downwards */
		Both	/**< Casts two rays in both directions */
	}
}