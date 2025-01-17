﻿// Cinema Suite
using CinemaDirector.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// The character track group is a type of actor group, specialized for humanoid characters.
    /// </summary>
    [TrackGroupAttribute("Character Track Group", TimelineTrackGenre.CharacterTrack)]
    public class CharacterTrackGroup : ActorTrackGroup, IRevertable, IBakeable
    {
        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        // Has a bake been called on this track group?
        private bool hasBeenBaked = false;

        /// <summary>
        /// Bake the Mecanim preview data.
        /// </summary>
        public void Bake()
        {
            if (Actor == null || Application.isPlaying) return;
            Animator animator = Actor.GetComponent<Animator>();
            if (animator == null)
            { return; }

            List<RevertInfo> revertCache = new List<RevertInfo>();

            // Build the cache of revert info.
            foreach (MonoBehaviour mb in this.GetComponentsInChildren<MonoBehaviour>())
            {
                IRevertable revertable = mb as IRevertable;
                if (revertable != null)
                {
                    revertCache.AddRange(revertable.CacheState());
                }
            }

            Vector3 position = Actor.transform.localPosition;
            Quaternion rotation = Actor.transform.localRotation;
            Vector3 scale = Actor.transform.localScale;

            float frameRate = 30;
            int frameCount = (int)((Cutscene.Duration * frameRate) + 2);
            animator.StopPlayback();
            animator.recorderStartTime = 0;
            animator.StartRecording(frameCount);

            base.SetRunningTime(0);

            for (int i = 0; i < frameCount-1; i++)
            {
                foreach (TimelineTrack track in GetTracks())
                {
                    if (!(track is DialogueTrack))
                    {
                        track.UpdateTrack(i * (1.0f / frameRate), (1.0f / frameRate));
                    }
                }
                animator.Update(1.0f / frameRate);
            }
            animator.recorderStopTime = frameCount * (1.0f / frameRate);
            animator.StopRecording();
            animator.StartPlayback();

            hasBeenBaked = true;

            // Return the Actor to his initial position.
            Actor.transform.localPosition = position;
            Actor.transform.localRotation = rotation;
            Actor.transform.localScale = scale;

            foreach (RevertInfo revertable in revertCache)
            {
                if (revertable != null)
                {
                    if ((revertable.EditorRevert == RevertMode.Revert && !Application.isPlaying) ||
                        (revertable.RuntimeRevert == RevertMode.Revert && Application.isPlaying))
                    {
                        revertable.Revert();
                    }
                }
            }

            base.Initialize();
        }

        /// <summary>
        /// Cache the Actor Transform.
        /// </summary>
        /// <returns>The revert info for the Actor's transform.</returns>
        public RevertInfo[] CacheState()
        {
            RevertInfo[] reverts = new RevertInfo[3];
            if (Actor == null) return new RevertInfo[0];
            reverts[0] = new RevertInfo(this, Actor.transform, "localPosition", Actor.transform.localPosition);
            reverts[1] = new RevertInfo(this, Actor.transform, "localRotation", Actor.transform.localRotation);
            reverts[2] = new RevertInfo(this, Actor.transform, "localScale", Actor.transform.localScale);
            return reverts;
        }

        /// <summary>
        /// Initialize the Track Group as normal and initialize the Animator if in Editor Mode.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (!Application.isPlaying)
            {
                if (Actor == null) return;
                Animator animator = Actor.GetComponent<Animator>();
                if (animator == null)
                {
                    return;
                }
                animator.StartPlayback();
            }
        }

        /// <summary>
        /// Update the Track Group over time. If in editor mode, play the baked animator data.
        /// </summary>
        /// <param name="time">The new running time.</param>
        /// <param name="deltaTime">the deltaTime since last update.</param>
        public override void UpdateTrackGroup(float time, float deltaTime)
        {
            if (Application.isPlaying)
            {
                base.UpdateTrackGroup(time, deltaTime);
            }
            else
            {
                foreach (TimelineTrack track in GetTracks())
                {
                    if (!(track is MecanimTrack))
                    {
                        track.UpdateTrack(time, deltaTime);
                    } 
                }

                if (Actor == null) return;
                Animator animator = Actor.GetComponent<Animator>();
                if (animator == null)
                {
                    return;
                }

                if (Actor.gameObject.activeInHierarchy)
                {
                    animator.playbackTime = time;
                    animator.Update(0);
                }
            }
        }

        public override void SetRunningTime(float time)
        {
            if (Application.isPlaying)
            {
                foreach (TimelineTrack track in GetTracks())
                {
                    track.SetTime(time);
                }
            }
            else
            {
                foreach (TimelineTrack track in GetTracks())
                {
                    if (!(track is MecanimTrack))
                    {
                        track.SetTime(time);
                    }
                }

                if (Actor == null) return;
                Animator animator = Actor.GetComponent<Animator>();
                if (animator == null)
                {
                    return;
                }
                if (Actor.gameObject.activeInHierarchy)
                {
                    animator.playbackTime = time;
                    animator.Update(0);
                }
            }
        }

        /// <summary>
        /// Stop this track group and stop playback on animator.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (!Application.isPlaying)
            {
                if (hasBeenBaked)
                {
                    hasBeenBaked = false;
                    Animator animator = Actor.GetComponent<Animator>();
                    if (animator == null)
                    {
                        return;
                    }

                    if (animator.recorderStopTime > 0)
                    {
                        if (Actor.gameObject.activeInHierarchy)
                        {
                            animator.StartPlayback();
                            animator.playbackTime = 0;


                            animator.Update(0);

                            animator.StopPlayback();

                            animator.Rebind();
                        }
                    }
                    
                }
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