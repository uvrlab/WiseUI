using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ARRC.DigitalTwinGenerator
{
    [CustomEditor(typeof(ImageControllerMono)), CanEditMultipleObjects]
    public class ImageControllerMonoEditor : Editor
    {
        private void OnEnable()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var mono = targets[i] as ImageControllerMono;

                if (i == 0)
                    mono.OnSelectedOnly();
                else
                    mono.OnSelectedAdditionaly();
            }
        }

        private void OnDisable()
        {
            foreach (ImageControllerMono mono in targets)
                mono.OnUnselected();
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

    }
}