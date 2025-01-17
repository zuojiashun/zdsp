using System.Collections.Generic;
using Pathfinding.Util;

namespace Pathfinding {

	/** Contains useful functions for updating graphs.
	  * This class works a lot with the Node class, a useful function to get nodes is AstarPath.GetNearest.
	  *
	  * \see AstarPath.GetNearest
	  * \see Pathfinding.Utils.PathUtilities
	  *
	  * \since Added in 3.1
	  *
	  * \ingroup utils
	  */
	public static class GraphUpdateUtilities {

		/** Updates graphs and checks if all nodes are still reachable from each other.
		 * Graphs are updated, then a check is made to see if the nodes are still reachable from each other.
		 * If they are not, the graphs are reverted to before the update and \a false is returned.\n
		 * This is slower than a normal graph update.
		 * All queued graph updates and thread safe callbacks will be flushed during this function.
		 *
		 * \note This might return true for small areas even if there is no possible path if AstarPath.minAreaSize is greater than zero (0).
		 * So when using this, it is recommended to set AstarPath.minAreaSize to 0 (A* Inspector -> Settings -> Pathfinding)
		 *
		 * \param guo The GraphUpdateObject to update the graphs with
		 * \param node1 Node which should have a valid path to \a node2. All nodes should be walkable or \a false will be returned.
		 * \param node2 Node which should have a valid path to \a node1. All nodes should be walkable or \a false will be returned.
		 * \param alwaysRevert If true, reverts the graphs to the old state even if no blocking ocurred
		 *
		 * \returns True if the given nodes are still reachable from each other after the \a guo has been applied. False otherwise.
		 *
\code
var guo = new GraphUpdateObject (tower.GetComponent<Collider>.bounds);
var spawnPointNode = AstarPath.active.GetNearest (spawnPoint.position).node;
var goalNode = AstarPath.active.GetNearest (goalNode.position).node;
if (GraphUpdateUtilities.UpdateGraphsNoBlock (guo, spawnPointNode, goalNode, false)) {
	// Valid tower position
	// Since the last parameter (which is called "alwaysRevert") in the method call was false
	// The graph is now updated and the game can just continue
} else {
	// Invalid tower position. It blocks the path between the spawn point and the goal
	// The effect on the graph has been reverted
	Destroy (tower);
}
\endcode
		 */
		public static bool UpdateGraphsNoBlock (GraphUpdateObject guo, GraphNode node1, GraphNode node2, bool alwaysRevert = false) {
			List<GraphNode> buffer = ListPool<GraphNode>.Claim ();
			buffer.Add (node1);
			buffer.Add (node2);

			bool worked = UpdateGraphsNoBlock (guo, buffer, alwaysRevert);
			ListPool<GraphNode>.Release (buffer);
			return worked;
		}

		/** Updates graphs and checks if all nodes are still reachable from each other.
		 * Graphs are updated, then a check is made to see if the nodes are still reachable from each other.
		 * If they are not, the graphs are reverted to before the update and \a false is returned.
		 * This is slower than a normal graph update.
		 * All queued graph updates and thread safe callbacks will be flushed during this function.
		 *
		 * \note This might return true for small areas even if there is no possible path if AstarPath.minAreaSize is greater than zero (0).
		 * So when using this, it is recommended to set AstarPath.minAreaSize to 0. (A* Inspector -> Settings -> Pathfinding)
		 *
		 * \param guo The GraphUpdateObject to update the graphs with
		 * \param nodes Nodes which should have valid paths between them. All nodes should be walkable or \a false will be returned.
		 * \param alwaysRevert If true, reverts the graphs to the old state even if no blocking ocurred
		 *
		 * \returns True if the given nodes are still reachable from each other after the \a guo has been applied. False otherwise.
		 */
		public static bool UpdateGraphsNoBlock (GraphUpdateObject guo, List<GraphNode> nodes, bool alwaysRevert = false) {

			//Make sure all nodes are walkable
			for (int i=0;i<nodes.Count;i++) if (!nodes[i].Walkable) return false;

			//Track changed nodes to enable reversion of the guo
			guo.trackChangedNodes = true;
			bool worked = true;

            //AstarPath.RegisterSafeUpdate (delegate () {

            //    AstarPath.active.UpdateGraphs (guo);

            //    //Call thread safe callbacks, includes graph updates
            //    AstarPath.active.FlushGraphUpdates();

            //    //Check if all nodes are in the same area and that they are walkable, i.e that there are paths between all of them
            //    worked = worked && PathUtilities.IsPathPossible (nodes);

            //    //If it did not work, revert the GUO
            //    if (!worked || alwaysRevert) {
            //        guo.RevertFromBackup ();

            //        //The revert operation does not revert ALL nodes' area values, so we must flood fill again
            //        AstarPath.active.FloodFill ();
            //    }
            //});

			//Force the thread safe callback to be called
			AstarPath.active.FlushThreadSafeCallbacks();

			//Disable tracking nodes, not strictly necessary, but will slightly reduce the cance that some user causes errors
			guo.trackChangedNodes = false;

			return worked;
		}
	}
}
