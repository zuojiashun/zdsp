﻿using CinemaDirector.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemAttribute("Effects", "Stop Xffect", CutsceneItemGenre.ActorItem)]
    public class StopXffect : CinemaActorEvent, IRevertable
    {
        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        /// <summary>
        /// Cache the state of all actors related to this event.
        /// </summary>
        /// <returns></returns>
        public RevertInfo[] CacheState()
        {
            List<Transform> actors = new List<Transform>(GetActors());
            List<RevertInfo> reverts = new List<RevertInfo>();
            foreach (Transform go in actors)
            {
                if (go != null)
                {
                    reverts.Add(new RevertInfo(this, go.gameObject, "SetActive", go.gameObject.activeSelf));
                }
            }

            return reverts.ToArray();
        }

        /// <summary>
        /// Trigger this event and disable the given actor.
        /// </summary>
        /// <param name="actor">The actor to be disabled.</param>
        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                var emitterRef = actor.GetComponent<EmitterRef>();
                if (emitterRef != null)
                    emitterRef.Deactive();
            }
        }

        /// <summary>
        /// Reverse the event by settings the game object to the previous state.
        /// </summary>
        /// <param name="actor">The actor whose active state will be reversed.</param>
        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                //actor.SetActive(true); // ToDo: Implement reversing with cacheing previous state.
            }
        }

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Editor.
        /// </summary>
        public RevertMode EditorRevertMode
        {
            get { return editorRevertMode; }
            set { editorRevertMode = value; }
        }

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Runtime.
        /// </summary>
        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}
