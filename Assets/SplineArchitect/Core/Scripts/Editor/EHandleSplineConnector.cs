// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: EHandleSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 23-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect
{
    public static class EHandleSplineConnector
    {
        private static List<SplineConnector> markedForDeletion = new List<SplineConnector>();

        public static void UpdateGlobal()
        {
            markedForDeletion.Clear();

            foreach (SplineConnector sc in HandleRegistry.GetSplineConnectors())
            {
                if (sc == null)
                {
                    markedForDeletion.Add(sc);
                    continue;
                }

                bool scPosChange = sc.monitor.PosChange(true);
                bool scRotChange = sc.monitor.RotChange(true);
                bool segmentOffsetChange = sc.monitor.SegmentOffsetChange(true);

                for (int i = sc.connections.Count - 1; i >= 0; i--)
                {
                    Segment s = sc.connections[i];

                    if (s == null)
                        continue;

                    if (s.localSpace == null)
                        continue;

                    if (s.linkTarget != Segment.LinkTarget.SPLINE_CONNECTOR)
                    {
                        s.splineConnector.RemoveConnection(s);
                        continue;
                    }

                    bool splineTransformMoved = s.splineParent.monitor.PosRotChange();
                    if (splineTransformMoved) s.splineParent.monitor.MarkEditorDirty();

                    if (scPosChange || scRotChange || segmentOffsetChange || splineTransformMoved)
                    {
                        sc.AlignSegment(s);
                        EHandleEvents.InvokeSplineConnectorAlignSegment(sc, s);
                        s.splineParent.UpdateCachedData();
                        EHandleDeformation.TryDeform(s.splineParent);
                    }
                }
            }

            for(int i = 0; i < markedForDeletion.Count; i++)
            {
                HandleRegistry.RemoveSplineConnector(markedForDeletion[i]);
            }
        }

        public static SplineConnector CreatedForContext(GameObject go)
        {
            go.name = $"SplineConnector ({HandleRegistry.GetSplineConnectors().Count + 1})";
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                SceneManager.MoveGameObjectToScene(go, prefabStage.scene);
                EHandleUndo.RegisterCreatedObject(go, "Created SplineConnector");
                Undo.SetTransformParent(go.transform, prefabStage.prefabContentsRoot.transform, "Created SplineConnector");
            }
            else
            {
                EHandleUndo.RegisterCreatedObject(go, "Created SplineConnector");
            }

            return EHandleUndo.AddComponent<SplineConnector>(go);
        }
    }
}
