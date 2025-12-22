// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandlePrefab.cs
//
// Author: Mikael Danielsson
// Date Created: 25-05-2024
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public class EHandlePrefab
    {
        public static bool prefabStageOpen { get; private set; }
        public static bool prefabStageClosedLastFrame { get; private set; }
        public static bool prefabStageOpenedLastFrame { get; private set; }

        private static List<SplineObject> soContainer = new List<SplineObject>();

        public static void OnPrefabUpdate(GameObject go)
        {
            UpdatedPrefabDeformations(false);

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (source == null)
                return;

            SplineObject so = source.GetComponent<SplineObject>();
            if (so == null)
                return;

            EActionToUpdate.Add(() =>
            {
                if (EHandleEvents.undoActive)
                    return;

                Debug.LogError($"[Spline Architect] You should not apply changes to the prefab '{source.name}' when it is deformed along a spline that is not part of the same prefab. Please undo these changes.\n" +
                               $"If you want to modify this prefab '{source.name}', open it in Prefab Mode and make the changes there.");

            }, EActionToUpdate.Type.LATE, 33354);

        }

        public static void OnPrefabRevert(GameObject go)
        {
            UpdatedPrefabDeformations(false);

            Spline spline = SplineUtility.TryFindSpline(go.transform);
            SplineObject so = go.GetComponent<SplineObject>();

            if(spline != null && so == null)
            {
                Transform[] childs = go.GetComponentsInChildren<Transform>();
                foreach (Transform child in childs)
                {
                    ESplineObjectUtility.TryAttacheOnTransformEditor(spline, child.transform, true);
                }
            }
        }

        public static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            //Closing a prefab stage while discarding changes will trigger an OnPrefabStageOpened for some reason. We need to handle that case and skip deformations.
            if (!prefabStageOpen) UpdatedPrefabDeformations(false);
            prefabStageOpen = true;
            prefabStageOpenedLastFrame = true;
        }

        public static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            UpdatedPrefabDeformations(true);
            prefabStageOpen = false;
            prefabStageClosedLastFrame = true;
        }

        public static void UpdateGlobal()
        {
            prefabStageClosedLastFrame = false;
            prefabStageOpenedLastFrame = false;
        }

        public static bool IsPartOfAnyPrefab(GameObject go)
        {
            return PrefabUtility.IsPartOfAnyPrefab(go);
        }

        public static bool IsInPrefabHierarchy(GameObject go)
        {
            Transform transform = go.transform;

            for (int i = 0; i < 25; i++)
            {
                if(transform == null)
                    break;

                if (IsPartOfAnyPrefab(transform.gameObject))
                    return true;

                transform = transform.parent;
            }

            return false;
        }

        public static bool IsPrefabRoot(GameObject go)
        {
            return PrefabUtility.IsOutermostPrefabInstanceRoot(go);
        }

        public static PrefabStage GetCurrentPrefabStage()
        {
            return PrefabStageUtility.GetCurrentPrefabStage();
        }

        public static GameObject GetOutermostPrefabRoot(GameObject go)
        {
            return PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        }

        public static bool IsPrefabStageActive()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        public static PrefabAssetType GetPrefabAssetType(GameObject go)
        {
            return PrefabUtility.GetPrefabAssetType(go);
        }

        public static bool IsPartOfActivePrefabStage(GameObject go)
        {
            if (PrefabStageUtility.GetPrefabStage(go) != null)
                return true;

            //Will be true when creating new spline:s inside prefabs.
            if (go.transform != null && PrefabStageUtility.GetPrefabStage(go.transform.gameObject) && IsPrefabStageActive())
                return true;

            //Will be true when creating new spline:s inside prefabs.
            if (go.transform.parent != null && PrefabStageUtility.GetPrefabStage(go.transform.parent.gameObject) && IsPrefabStageActive())
                return true;

            //prefabStageClosing only runs during one frame. The frame after PrefabStage.prefabStageClosing += OnPrefabStageClosing.
            if (prefabStageClosedLastFrame)
                return true;

            return false;
        }

        private static void UpdatedPrefabDeformations(bool closing)
        {
            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                if (spline == null)
                    continue;

                soContainer.Clear();
                soContainer.AddRange(spline.allSplineObjects);
                EHandleEvents.MarkForInfoUpdate(spline);

                if (!IsPartOfAnyPrefab(spline.gameObject) && !IsPartOfActivePrefabStage(spline.gameObject))
                {
                    foreach (SplineObject so in soContainer)
                    {
                        if (so == null || so.transform == null)
                            continue;

                        if (closing && IsPartOfActivePrefabStage(so.gameObject))
                            continue;

                        TryAttachNewChildrenSplineObject(so);
                    }
                    continue;
                }

                foreach (SplineObject so in soContainer)
                {
                    if (so == null || so.transform == null)
                        continue;

                    //We dont want to deform deformations wihtin the prefab stage after its closed. We will get errors.
                    if (closing && IsPartOfActivePrefabStage(so.gameObject))
                        continue;

                    TryAttachNewChildrenSplineObject(so);

                    if (so.type == SplineObject.Type.DEFORMATION && so.meshContainers != null && so.meshContainers.Count > 0)
                    {
                        so.SyncInstanceMeshesFromCache();
                        HandleDeformation.Deform(so, DeformationWorker.Type.EDITOR, spline);
                    }
                    else if (so.type == SplineObject.Type.FOLLOWER)
                    {
                        //Need to sync MeshContainers becouse the follower can have an instaceMesh without a MeshContainer.
                        so.SyncMeshContainers();

                        foreach(MeshContainer mc in so.meshContainers)
                        {
                            Mesh mesh = mc.GetInstanceMesh();

                            if (mesh == null)
                                continue;

                            string assetPath = GeneralUtility.GetAssetPath(mesh);

                            if (assetPath == "")
                            {
                                Mesh originMesh = ESplineObjectUtility.GetOriginMeshFromMeshNameId(mesh);

                                if (originMesh == null)
                                    continue;

                                mc.SetOriginMesh(originMesh);
                                mc.SetInstanceMeshToOriginMesh();
                            }
                        }

                        spline.followerUpdateList.Add(so);
                    }
                }
            }

            void TryAttachNewChildrenSplineObject(SplineObject so)
            {
                if (so.type != SplineObject.Type.DEFORMATION)
                    return;

                //Need to delay one frame for some reason else the editor can crash after doing Undos 1 or 2 times.
                EActionDelayed.Add(() =>
                {
                    if (so == null)
                        return;

                    Transform[] childs = so.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childs)
                    {
                        SplineObject soChild = child.GetComponent<SplineObject>();

                        if (soChild != null)
                            continue;

                        ESplineObjectUtility.TryAttacheOnTransformEditor(so.splineParent, child, true);
                    }
                }, 0, 0, EActionDelayed.Type.FRAMES);
            }
        }
    }
}

#endif
