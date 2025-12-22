using UnityEngine;
using UnityEngine.SceneManagement;

using SplineArchitect;
using SplineArchitect.Objects;

namespace SplineArchitect.Examples
{
    public class SADebugTool : MonoBehaviour
    {
        private static bool gShowDebugInfo;

#if UNITY_6000_1_OR_NEWER
        private static bool ghasSet;
        public static bool gSwitchToNextScene;
        public bool autoSwitchToNextScene;
        public bool showDebugInfo;
#endif

        private float deltaTime;
        private float timer;
        string currentSceneName;
        Texture2D backgroundTex;
        GUIStyle style;

        private void Start()
        {
            backgroundTex = new Texture2D(1, 1);
            backgroundTex.SetPixel(0, 0, Color.white);
            backgroundTex.Apply();

            currentSceneName = SceneManager.GetActiveScene().name;

#if UNITY_6000_1_OR_NEWER
            if (!ghasSet)
            {
                ghasSet = true;
                gSwitchToNextScene = autoSwitchToNextScene;
                gShowDebugInfo = showDebugInfo;
            }
#endif
        }

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

#if UNITY_6000_1_OR_NEWER
            timer += Time.deltaTime;

            if (timer > 15)
            {
                timer = 0;
                if (gSwitchToNextScene)
                    LoadNextScene(true);
            }
#else
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Home))
                gShowDebugInfo = !gShowDebugInfo;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.PageDown))
                LoadNextScene(true);

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.PageUp))
                LoadNextScene(false);
#endif
        }

        public void LoadNextScene(bool forward)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int totalScenes = SceneManager.sceneCountInBuildSettings;

            int next = 1;
            if (!forward) next = -1;
            if (next < 0) next = totalScenes - 1;

            SceneManager.LoadScene((currentIndex + next) % totalScenes);
            currentSceneName = SceneManager.GetActiveScene().name;
        }

        void OnGUI()
        {
            if (!gShowDebugInfo)
                return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.richText = true;
                style.fontSize = 16;
                style.normal.textColor = Color.black;
            }

            float textHeight = 25;
            float textWidth = 300;
            float textPaddingHeight = 18;
            float textPaddingWidth = 20;
            float fps = 1.0f / deltaTime;

            int totalSplineObjects = 0;
            int totalRootSplineObjects = 0;

            foreach (Spline spline in HandleRegistry.GetSplines())
            {
                totalSplineObjects += spline.allSplineObjects.Count;
                totalRootSplineObjects += spline.rootSplineObjects.Count;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(10, 10, textPaddingHeight * 16, textPaddingHeight * 16), backgroundTex);

            GUI.color = Color.black;
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 1, textWidth, textHeight), $"<b>{currentSceneName}</b>", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 3, textWidth, textHeight), $"FPS: {fps:F1}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 4, textWidth, textHeight), $"Total splines length: {HandleRegistry.GetTotalLengthOfAllSplines()}", style);

            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 6, textWidth, textHeight), $"<b>Registry:</b>", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 7, textWidth, textHeight), $"Splines: {HandleRegistry.GetSplines().Count}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 8, textWidth, textHeight), $"Spline Objects: {totalSplineObjects}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 9, textWidth, textHeight), $"Spline Objects root: {totalRootSplineObjects}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 10, textWidth, textHeight), $"Spline Connectors: {HandleRegistry.GetSplineConnectors().Count}", style);

            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 12, textWidth, textHeight), $"<b>Cached resources:</b>", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 13, textWidth, textHeight), $"Instance meshes: {HandleCachedResources.GetInstanceMeshCount()}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 14, textWidth, textHeight), $"Origin vertices: {HandleCachedResources.GetOriginMeshVerticesCount()}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 15, textWidth, textHeight), $"New vertices containers: {HandleCachedResources.GetNewVerticesContainerCount()}", style);
        }
    }
}
