using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// Attaches all objects as children of actor in hierarchy
    /// </summary>
    [CutsceneItemAttribute("Transform", "Attach Children", CutsceneItemGenre.ActorItem)]
    public class AttachChildrenEvent : CinemaActorEvent
    {
        public GameObject[] Children;
        public override void Trigger(GameObject actor)
        {
            if (actor != null && Children != null)
            {
                foreach (GameObject child in Children)
                {
                    child.transform.parent = actor.transform;
                }
            }
        }

        public override void Reverse(GameObject actor)
        {
        }
    }
}