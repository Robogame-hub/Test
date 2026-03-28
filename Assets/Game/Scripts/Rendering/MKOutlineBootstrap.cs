#if MK_URP
using MK.EdgeDetection;
using MK.EdgeDetection.PostProcessing.Generic;
using MK.EdgeDetection.UniversalVolumeComponents;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Rendering
{
    public static class MKOutlineBootstrap
    {
        private const float GlobalOutlineSize = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToSceneVolumes();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToSceneVolumes();
        }

        private static void ApplyToSceneVolumes()
        {
            Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Volume volume in volumes)
            {
                if (volume == null || volume.sharedProfile == null)
                    continue;

                VolumeProfile profile = volume.sharedProfile;
                if (!profile.TryGet(out MKEdgeDetection edgeDetection))
                    edgeDetection = profile.Add<MKEdgeDetection>(true);

                if (edgeDetection == null)
                    continue;

                edgeDetection.active = true;

                edgeDetection.inputData.overrideState = true;
                edgeDetection.inputData.value = new BitmaskProperty<InputData>(InputData.Depth | InputData.Normal);

                edgeDetection.globalLineSize.overrideState = true;
                edgeDetection.globalLineSize.value = new RangeProperty(GlobalOutlineSize, 0f, 3f);

                edgeDetection.depthLineSize.overrideState = true;
                edgeDetection.depthLineSize.value = new RangeProperty(1f, 0f, 2f);

                edgeDetection.normalLineSize.overrideState = true;
                edgeDetection.normalLineSize.value = new RangeProperty(1f, 0f, 2f);

                edgeDetection.lineHardness.overrideState = true;
                edgeDetection.lineHardness.value = new RangeProperty(0.5f, 0f, 1f);

                edgeDetection.lineColor.overrideState = true;
                edgeDetection.lineColor.value = new ColorProperty(0f, 0f, 0f, 1f, true, false);

                edgeDetection.overlayColor.overrideState = true;
                edgeDetection.overlayColor.value = new ColorProperty(1f, 1f, 1f, 0f, true, false);
            }
        }
    }
}
#endif
