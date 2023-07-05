/*
 * Author: Leonhard Robin Schnaitl
 * GitHub: https://github.com/leonhardrobin
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace LRS
{
    public class PointsData : ScriptableObject
    {
        public List<string> includedTags = new();
        public VisualEffect prefab;
        [HideInInspector] public VisualEffect currentVisualEffect;
        [HideInInspector] public List<VisualEffect> usedVisualEffects = new();
        [HideInInspector] public List<Vector3> positionsList = new();
        [HideInInspector] public Texture2D texture;
        [HideInInspector] public Color[] positionsAsColors;

        [ContextMenu("Clear Data")]
        private void ClearData()
        {
            currentVisualEffect = null;
            usedVisualEffects.Clear();
            positionsList.Clear();
            texture = null;
            positionsAsColors = null;
        }
    }
}

