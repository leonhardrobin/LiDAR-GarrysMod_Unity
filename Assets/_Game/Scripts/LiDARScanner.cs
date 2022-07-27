using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class LiDARScanner : MonoBehaviour
{
    private InputAction _fire;
    private bool canCreate = false;
    
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private GameObject _vfxPrefab;
    [SerializeField]
    private GameObject _vfxContainer;
    [SerializeField]
    private Transform _castPoint;
    [SerializeField]
    private float _range = 10f;

    [SerializeField] private int particleCount = 16000;

    private void Start()
    {
        _fire = playerInput.actions["Fire"];
    }
    
    private void Update()
    {
        if (!_fire.IsPressed()) return;

        Vector3[] hitPositions = new Vector3[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float number = i / (float)particleCount;
            Vector2 randomPoint = Random.insideUnitCircle * _range;
            Vector3 randomPoint3D = new (randomPoint.x + _castPoint.position.x, randomPoint.y + _castPoint.position.y, _castPoint.position.z);
            Vector3 dir = (randomPoint3D - transform.position).normalized;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit))
            {
                Debug.DrawRay(transform.position, dir * hit.distance, Color.red);
                hitPositions[i] = hit.point;
            }
        }
        CreateTexture(hitPositions);
    }

    private void CreateTexture(Vector3[] pos)
    {
        GameObject vfxObj = Instantiate(_vfxPrefab, _vfxContainer.transform.position,Quaternion.identity, _vfxContainer.transform);
        VisualEffect vfx = vfxObj.GetComponent<VisualEffect>();
        
        Vector3 vfxPos = _vfxPrefab.transform.position;

        vfx.SetUInt("ParticleCount", (uint)particleCount);
        Texture2D texture = new (particleCount, 1, TextureFormat.RGBAFloat, false);
        Color[] positions = new Color[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            positions[i] = new Color(pos[i].x, pos[i].y, pos[i].z, 0);
        }
        
        texture.SetPixels(positions);
        texture.Apply();
        
        vfx.SetTexture("PositionsTexture", texture);
        //_lidarVFX.SendEvent("Spawn");
        vfx.Reinit();
    }
}
