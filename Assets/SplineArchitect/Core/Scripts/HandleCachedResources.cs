// -----------------------------------------------------------------------------
// SplineArchitect
// Filename: HandleCachedResources.cs
//
// Author: Mikael Danielsson
// Date Created: 14-05-2023
// (C) 2023 Mikael Danielsson. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using SplineArchitect.Objects;

namespace SplineArchitect
{
    public class HandleCachedResources
    {
        private static Dictionary<string, (Mesh, string)> instanceMeshesRuntime = new Dictionary<string, (Mesh, string)>();
        private static Dictionary<string, (Vector3[], string)> originMeshVertices = new Dictionary<string, (Vector3[], string)>();
        private static Dictionary<string, (Vector3[], string)> originMeshNormals = new Dictionary<string, (Vector3[], string)>();
        private static Dictionary<string, (Vector4[], string)> originMeshTangents = new Dictionary<string, (Vector4[], string)>();
        private static Dictionary<string, (Vector3[], string)> verticeNormalContainer = new Dictionary<string, (Vector3[], string)>();
        private static Dictionary<string, (Vector4[], string)> tangentContainer = new Dictionary<string, (Vector4[], string)>();

        private static List<string> clearContainer = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            ClearScene(scene.name);
        }

        /// <summary>
        /// Checks whether a mesh instance with the given name is cached in memory.
        /// </summary>
        public static bool IsInstanceMeshCached(Mesh instanceMesh)
        {
            return instanceMeshesRuntime.ContainsKey(instanceMesh.name);
        }

        /// <summary>
        /// Retrieves or creates a runtime mesh instance based on the original mesh in the MeshContainer.
        /// Caches the result using a unique resource key.
        /// </summary>
        public static Mesh FetchInstanceMesh(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();

            if (originMesh == null)
                return null;

            string key = mc.GetResourceKey();

            if (instanceMeshesRuntime.ContainsKey(key))
            {
                if (instanceMeshesRuntime[key].Item1 != null)
                    return instanceMeshesRuntime[key].Item1;

                instanceMeshesRuntime.Remove(key);
            }

            Mesh instanceMesh = Object.Instantiate(originMesh);
            instanceMeshesRuntime.Add(key, (instanceMesh, mc.GetScene().name));

            return instanceMesh;
        }

        /// <summary>
        /// Gets the original mesh vertices from the cache if available.
        /// If not cached, fetches from the original mesh and stores them.
        /// </summary>
        public static Vector3[] FetchOriginVertices(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string key = mc.GetResourceKeyShort();

            if (originMeshVertices.ContainsKey(key))
                return originMeshVertices[key].Item1;

            Vector3[] vertices = originMesh.vertices;
            originMeshVertices.Add(key, (vertices, mc.GetScene().name));

            return vertices;
        }

        public static Vector3[] FetchOriginNormals(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string key = mc.GetResourceKeyShort();

            if (originMeshNormals.ContainsKey(key))
                return originMeshNormals[key].Item1;

            Vector3[] normals = originMesh.normals;
            originMeshNormals.Add(key, (normals, mc.GetScene().name));

            return normals;
        }

        public static Vector4[] FetchOriginTangents(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string key = mc.GetResourceKeyShort();

            if (originMeshTangents.ContainsKey(key))
                return originMeshTangents[key].Item1;

            Vector4[] normals = originMesh.tangents;
            originMeshTangents.Add(key, (normals, mc.GetScene().name));

            return normals;
        }

        /// <summary>
        /// Gets a reusable vertex array for deformation based on the original mesh.
        /// If not already cached, clones the original vertices and stores them.
        /// </summary>
        public static Vector3[] FetchVerticeNormalContainer(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string key = mc.GetResourceKeyShort();

            if (verticeNormalContainer.ContainsKey(key))
                return verticeNormalContainer[key].Item1;

            Vector3[] vertices = originMesh.vertices;
            verticeNormalContainer.Add(key, (vertices, mc.GetScene().name));

            return vertices;
        }

        public static Vector4[] FetchOriginTangentsContainer(MeshContainer mc)
        {
            Mesh originMesh = mc.GetOriginMesh();
            string key = mc.GetResourceKeyShort();

            if (tangentContainer.ContainsKey(key))
                return tangentContainer[key].Item1;

            Vector4[] normals = originMesh.tangents;
            tangentContainer.Add(key, (normals, mc.GetScene().name));

            return normals;
        }

        /// <summary>
        /// Updates or adds all cached data related to the MeshContainer,
        /// including the instance mesh, origin vertices, and vertex container,
        /// replacing any existing entries.
        /// </summary>
        public static void AddOrUpdateInstanceMesh(MeshContainer mc)
        {
            Mesh instanceMesh = mc.GetInstanceMesh();
            string resourceKey = mc.GetResourceKey();

            if(instanceMeshesRuntime.ContainsKey(resourceKey))
                instanceMeshesRuntime.Remove(resourceKey);
            instanceMeshesRuntime.Add(resourceKey, (instanceMesh, mc.GetScene().name));

            Mesh originMesh = mc.GetOriginMesh();
            string resourceKeyShort = mc.GetResourceKeyShort();

            if (originMeshVertices.ContainsKey(resourceKeyShort))
                originMeshVertices.Remove(resourceKeyShort);
            Vector3[] vertices = originMesh.vertices;
            originMeshVertices.Add(resourceKeyShort, (vertices, mc.GetScene().name));

            if (originMeshNormals.ContainsKey(resourceKeyShort))
                originMeshNormals.Remove(resourceKeyShort);
            Vector3[] normals = originMesh.normals;
            originMeshNormals.Add(resourceKeyShort, (normals, mc.GetScene().name));

            if (originMeshTangents.ContainsKey(resourceKeyShort))
                originMeshTangents.Remove(resourceKeyShort);
            Vector4[] tangents = originMesh.tangents;
            originMeshTangents.Add(resourceKeyShort, (tangents, mc.GetScene().name));

            if (verticeNormalContainer.ContainsKey(resourceKeyShort))
                verticeNormalContainer.Remove(resourceKeyShort);
            Vector3[] verticesContainer = originMesh.vertices;
            verticeNormalContainer.Add(resourceKeyShort, (verticesContainer, mc.GetScene().name));

            if (tangentContainer.ContainsKey(resourceKeyShort))
                tangentContainer.Remove(resourceKeyShort);
            Vector4[] tangentsContainer = originMesh.tangents;
            tangentContainer.Add(resourceKeyShort, (tangentsContainer, mc.GetScene().name));
        }

        /// <summary>
        /// Returns the number of currently cached mesh instances.
        /// </summary>
        public static int GetInstanceMeshCount()
        {
            return instanceMeshesRuntime.Count;
        }

        /// <summary>
        /// Returns the number of cached original vertex arrays.
        /// </summary>
        public static int GetOriginMeshVerticesCount()
        {
            return originMeshVertices.Count;
        }

        public static int GetOriginMeshNormalsCount()
        {
            return originMeshNormals.Count;
        }

        /// <summary>
        /// Returns the number of cached vertex containers used for mesh deformation.
        /// </summary>
        public static int GetNewVerticesContainerCount()
        {
            return verticeNormalContainer.Count;
        }

        /// <summary>
        /// Removes all cached mesh data (instances, origin vertices, vertex containers)
        /// associated with a specific scene name.
        /// </summary>
        public static void ClearScene(string name)
        {
            clearContainer.Clear();

            foreach (KeyValuePair<string, (Vector3[], string)> item in originMeshVertices)
            {
                if (item.Value.Item2 == name)
                    clearContainer.Add(item.Key);
            }

            foreach (string s in clearContainer)
            {
                originMeshVertices.Remove(s);
                originMeshNormals.Remove(s);
                originMeshTangents.Remove(s);
                verticeNormalContainer.Remove(s);
                tangentContainer.Remove(s);
            }

            clearContainer.Clear();

            foreach (KeyValuePair<string, (Mesh, string)> item in instanceMeshesRuntime)
            {
                if (item.Value.Item2 == name)
                    clearContainer.Add(item.Key);
            }

            foreach (string s in clearContainer)
            {
                instanceMeshesRuntime.Remove(s);
            }
        }
    }
}