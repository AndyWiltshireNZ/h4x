// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: MonitorSplineConnector.cs
//
// Author: Mikael Danielsson
// Date Created: 23-05-2025
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;

using SplineArchitect.Objects;
using SplineArchitect.Utility;

namespace SplineArchitect.Monitor
{
    public class MonitorSplineConnector
    {
        public const int dataUsage = 24 + 
                                     16 + 12;

        private Quaternion rotation;
        private Vector3 position;
        private List<Vector3> segmentPosOffset = new List<Vector3>();
        private List<Vector3> segmentRotOffset = new List<Vector3>();
        private SplineConnector sc;

        public MonitorSplineConnector(SplineConnector sc)
        {
            this.sc = sc;
            Update();
        }

        public bool SegmentOffsetChange(bool forceUpdate = false) 
        {
            bool foundChange = false;

            if(segmentPosOffset.Count == sc.connections.Count && segmentRotOffset.Count == sc.connections.Count)
            {
                for(int i = 0; i < sc.connections.Count; i++)
                {
                    Segment s = sc.connections[i];

                    if (!GeneralUtility.IsEqual(s.connectorPosOffset, segmentPosOffset[i]))
                    {
                        foundChange = true; 
                        break;
                    }

                    if (!GeneralUtility.IsEqual(s.connectorRotOffset.eulerAngles, segmentRotOffset[i]))
                    {
                        foundChange = true;
                        break;
                    }
                }
            }
            else
            {
                foundChange = true;
            }

            if (forceUpdate && foundChange)
            {
                segmentPosOffset.Clear();
                segmentRotOffset.Clear();
                foreach (Segment s in sc.connections)
                {
                    segmentPosOffset.Add(s.connectorPosOffset);
                    segmentRotOffset.Add(s.connectorRotOffset.eulerAngles);
                }
            }

            return foundChange;
        }

        public bool PosChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(position, sc.transform.position)) foundChange = true;

            if (forceUpdate)
                position = sc.transform.position;

            return foundChange;
        }

        public bool RotChange(bool forceUpdate = false)
        {
            bool foundChange = false;
            if (!GeneralUtility.IsEqual(rotation.eulerAngles, sc.transform.rotation.eulerAngles)) foundChange = true;

            if(forceUpdate)
                rotation = sc.transform.rotation;

            return foundChange;
        }

        public void Update()
        {
            segmentPosOffset.Clear();
            segmentRotOffset.Clear();
            foreach (Segment s in sc.connections)
            {
                segmentPosOffset.Add(s.connectorPosOffset);
                segmentRotOffset.Add(s.connectorRotOffset.eulerAngles);
            }

            rotation = sc.transform.rotation;
            position = sc.transform.position;
        }

        public void ForceUpdate()
        {
            position.x++;
        }
    }
}
