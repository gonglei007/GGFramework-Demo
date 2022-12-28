using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace MarcXSK {

    public class SceneLightsManager : MonoBehaviour
    {
        [SerializeField]
        [Range(0.01f, 0.9f)]
        private float mfDampenFactor = 0.1f;
        private float mfAmplyFactor = 10f;
        [SerializeField]
        [Range(0f, 100f)]
        private float mfTuneTime = 1f;
        private void Start()
        {
            if(mfDampenFactor > 0f)
            {
                mfAmplyFactor = 1f / mfDampenFactor;
            }
        }
        // Start is called before the first frame update
        public void OnFullScreenUIShow(bool bShow)
        {
            if (bShow)
            {
                TurnLightsOFF();
            }
            else
            {
                TurnLightsON();
            }
        }

        private void TurnLightsON()
        {
            Light[] lights = GetComponentsInChildren<Light>();
            foreach (Light light in lights) {
                if (true)//light.type == LightType.Directional)
                {
                    float tgtIntensity = light.intensity * mfAmplyFactor;
                    DOTween.To(() => light.intensity, x => light.intensity = x, tgtIntensity, mfTuneTime);
                }
            }
        }

        private void TurnLightsOFF()
        {
            Light[] lights = GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (true)//light.type == LightType.Directional)
                {
                    float tgtIntensity = light.intensity * mfDampenFactor;
                    DOTween.To(() => light.intensity, x => light.intensity = x, tgtIntensity, mfTuneTime);
                }
            }
        }
    }
}