﻿using UnityEditor;
using UnityEngine;
using CinemaDirector;
using CinemaDirectorControl.Utility;

/// <summary>
/// A custom inspector for a director group.
/// </summary>
[CustomEditor(typeof(DirectorGroup), true)]
public class DirectorGroupInspector : Editor
{
    private bool containerFoldout = true;
    private Texture inspectorIcon = null;
    private SerializedProperty optimizable;

    #region Language
    GUIContent addTrackContent = new GUIContent("Add New Track", "Add a new track to this actor track group.");
    //GUIContent ordinalContent = new GUIContent("Ordinal", "The ordinal value of this container, for sorting containers in the timeline.");

    GUIContent tracksContent = new GUIContent("Global Tracks", "The tracks associated with this Director Group.");
    #endregion

    /// <summary>
    /// Load texture assets on awake.
    /// </summary>
    private void Awake()
    {
        if (inspectorIcon == null)
        {
            inspectorIcon = Resources.Load<Texture>("Director_InspectorIcon");
        }
        if (inspectorIcon == null)
        {
            Debug.Log("Inspector icon missing from Resources folder.");
        }
    }

    /// <summary>
    /// On inspector enable, load the serialized properties
    /// </summary>
    private void OnEnable()
    {
        this.optimizable = serializedObject.FindProperty("canOptimize");
    }

    /// <summary>
    /// Draw the inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.serializedObject.Update();

        DirectorGroup directorGroup = base.serializedObject.targetObject as DirectorGroup;
        TimelineTrack[] tracks = directorGroup.GetTracks();

        EditorGUILayout.PropertyField(optimizable);
        if (tracks.Length > 0)
        {
            containerFoldout = EditorGUILayout.Foldout(containerFoldout, tracksContent);
            if (containerFoldout)
            {
                EditorGUI.indentLevel++;

                foreach (TimelineTrack track in tracks)
                {
                    EditorGUILayout.BeginHorizontal();
                    track.name = EditorGUILayout.TextField(track.name);
                    if (GUILayout.Button(inspectorIcon, GUILayout.Width(24)))
                    {
                        Selection.activeObject = track;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        if (GUILayout.Button(addTrackContent))
        {
            CutsceneControlHelper.ShowAddTrackContextMenu(directorGroup);
        }

        base.serializedObject.ApplyModifiedProperties();
    }
}
