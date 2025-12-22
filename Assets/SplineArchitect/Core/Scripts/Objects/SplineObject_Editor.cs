// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: SplineObject_Editor.cs
//
// Author: Mikael Danielsson
// Date Created: 28-03-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System;

using UnityEngine;

using SplineArchitect.Utility;

namespace SplineArchitect.Objects
{
    public partial class SplineObject : MonoBehaviour
    {
#if UNITY_EDITOR
        //General stored data
        [HideInInspector]
        public bool hasPrimitiveCollider = false;
        [HideInInspector]
        public bool autoType = false;
        [HideInInspector]
        public bool canUpdateSelection = true;
        //General runtime data
        [NonSerialized]
        public bool skipUndoOnNextAttache;
        [NonSerialized]
        public int deformedVertecies;
        [NonSerialized]
        public int deformations;
        [NonSerialized]
        public Vector3 activationPosition = Vector3.zero;
        [NonSerialized]
        public bool disableOnTransformChildrenChanged;
        [NonSerialized]
        public bool initalizedThisFrame = false;
        [NonSerialized]
        private bool readWriteWarningTriggered;
        [NonSerialized]
        private bool componentModeWarningTriggered;

        private void OnTransformChildrenChanged()
        {
            if (disableOnTransformChildrenChanged)
                return;

            if (splineParent == null)
                return;

            monitor.ChildCountChange(out int dif);
            monitor.UpdateChildCount();

            if (type != Type.DEFORMATION)
                return;

            if (dif > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    //Should not parent splines to deformations. However you can do that to followers.
                    if(type == Type.DEFORMATION)
                    {
                        Spline childSpline = child.GetComponent<Spline>();

                        if(childSpline != null)
                        {
                            Debug.LogWarning($"[Spline Architect] Can't parent spline to SplineObject with type Deformation.");
                            child.parent = null;
                            continue;
                        }
                    }

                    ESplineObjectUtility.TryAttacheOnTransformEditor(splineParent, child, false, skipUndoOnNextAttache);
                    SplineObject so = child.GetComponent<SplineObject>();

                    if (so == null)
                        continue;

                    if (so.type != Type.DEFORMATION)
                        continue;

                    foreach (Transform child2 in child.GetComponentsInChildren<Transform>())
                    {
                        if (child2 == child)
                            continue;

                        ESplineObjectUtility.TryAttacheOnTransformEditor(splineParent, child2, true, skipUndoOnNextAttache);
                    }
                }

                skipUndoOnNextAttache = false;
            }
        }

        public void InitalizeEditor()
        {
            initalizedThisFrame = true;

            //Check for invalid meshes
            if (meshMode == MeshMode.GENERATE)
            {
                bool foundInvalidMesh = false;
                foreach (MeshContainer mc in meshContainers)
                {
                    if (!mc.GetOriginMesh().isReadable)
                    {
                        Debug.LogError($"[Spline Architect] SplineObject \"{name}\" has an invalid mesh.\n Enable 'Read/Write Enabled' in the import settings to allow runtime deformation.");
                        foundInvalidMesh = true;
                        break;
                    }
                }

                if (foundInvalidMesh && (EHandlePrefab.IsPartOfAnyPrefab(gameObject) || EHandlePrefab.IsPrefabStageActive()))
                    Debug.LogError($"[Spline Architect] Read/Write access need to be enabled on all deformations within prefabs.");
            }

            //Deform mesh during build and store it in the built application.
            if (type == Type.DEFORMATION && meshMode == MeshMode.SAVE_IN_BUILD && EHandleEvents.buildRunning && meshContainers.Count > 0)
            {
                splineParent.Initalize();
                HandleDeformation.Deform(this, DeformationWorker.Type.EDITOR, splineParent);

                HandleDeformation.GetActiveWorkers(DeformationWorker.Type.EDITOR, HandleDeformation.activeWorkersList);
                foreach (DeformationWorker dw in HandleDeformation.activeWorkersList)
                {
                    dw.Start();
                    dw.Complete();
                }
            }

            activationPosition = localSplinePosition;

            if(type == Type.DEFORMATION)
            {
                //Initalizes meschContainers.
                foreach (MeshContainer mc in meshContainers)
                {
                    //1. Gets the correct time stamp. Is used for detecting asset modifications.
                    mc.TryUpdateTimestamp();
                    //2. Update resourceKey, the timestamps is part of the key.
                    mc.UpdateResourceKey();
                    //3. The instanceMeshs name is the resourceKey.
                    mc.UpdateInstanceMeshName();
                }
            }

            //If part of hierarchy this data needs to be the same for all spline objects in the hierarchy.
            if (soParent != null)
            {
                alignToEnd = soParent.alignToEnd;
                componentMode = soParent.componentMode;
            }
        }

        public bool ValidForRuntimeDeformation()
        {
            if(!componentModeWarningTriggered && !initalizedThisFrame && Application.isPlaying && (componentMode != ComponentMode.ACTIVE || 
                                                                                                   splineParent.componentMode != ComponentMode.ACTIVE))
            {
                componentModeWarningTriggered = true;
                if(componentMode != ComponentMode.ACTIVE)
                    Debug.LogWarning($"[Spline Architect] Component mode is not set to 'Active' on '{name}'! Animating this object will not work in your built game.");
                if (splineParent.componentMode != ComponentMode.ACTIVE)
                    Debug.LogWarning($"[Spline Architect] Component mode is not set to 'Active' on the spline '{splineParent.name}'! Animating this object will not work in your built game.");
            }
            else if (!componentModeWarningTriggered && Application.isPlaying && type == Type.DEFORMATION && componentMode == ComponentMode.REMOVE_FROM_BUILD)
            {
                componentModeWarningTriggered = true;
                Debug.LogWarning($"[Spline Architect] Component mode is not set to 'Active' or 'Inactivate after scene load' on '{name}'! Generating this object will not work in your built game.");
            }

            if(type == Type.DEFORMATION)
            {
                foreach (MeshContainer mc in meshContainers)
                {
                    Mesh instanceMesh = mc.GetInstanceMesh();
                    Mesh originMesh = mc.GetOriginMesh();

                    if (instanceMesh == null || originMesh == null)
                        return false;

                    if (originMesh == instanceMesh)
                        return false;

                    if (!instanceMesh.isReadable)
                    {
                        if (!readWriteWarningTriggered && Application.isPlaying && type == Type.DEFORMATION)
                        {
                            readWriteWarningTriggered = true;
                            Debug.LogWarning($"[Spline Architect] No read/write access on '{name}'! Generating or animating this object will not work in your built game.");
                        }

                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsParentTo(SplineObject so)
        {
            SplineObject parent = so.soParent;

            for (int i = 0; i < 25; i++)
            {
                if (parent == null)
                    return false;

                if (parent == this)
                    return true;

                parent = parent.soParent;
            }

            return false;
        }
#endif
    }
}
