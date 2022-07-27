using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class LiDARScanner1 : MonoBehaviour
{
    private InputAction _fire;
    private List<Vector3> _positionsList = new();
    private List<VisualEffect> _vfxList = new();
    private VisualEffect _currentVFX;
    private Texture2D _texture;
    private Color[] _positions;
    private bool createNewVFX;
    
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private GameObject _vfxPrefab;
    [SerializeField]
    private GameObject _vfxContainer;
    [SerializeField]
    private Transform _castPoint;
    [SerializeField]
    private float _radius = 10f;
    [SerializeField]
    private int _pointsPerScan = 100;
    [SerializeField]
    private float _range = 10f;

    [SerializeField] private int particleCount = 16000;

    private void Start()
    {
        _fire = playerInput.actions["Fire"];
        createNewVFX = true;
        NewVisualEffect();
    }
    
    private void Update()
    {
        if (!_fire.IsPressed()) return;
        
        for (int i = 0; i < _pointsPerScan; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * _radius;
            Vector3 randomPoint3D = new (randomPoint.x + _castPoint.position.x, randomPoint.y + _castPoint.position.y, _castPoint.position.z);
            Vector3 dir = (randomPoint3D - transform.position).normalized;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, _range))
            {
                Debug.DrawRay(transform.position, dir * hit.distance, Color.green);
                if (_positionsList.Count < particleCount)
                {
                    _positionsList.Add(hit.point);
                }
                else
                {
                    createNewVFX = true;
                    NewVisualEffect();
                    break;
                }
            }
            else
            {
                Debug.DrawRay(transform.position, dir * _range, Color.red);
            }
        }
        CreateTexture();
    }

    private void CreateTexture()
    {
        Vector3[] pos = _positionsList.ToArray();
        Vector3 vfxPos = _currentVFX.transform.position;
        
        for (int i = 0; i < particleCount; i++)
        {
            if (i < pos.Length - 1)
            {
                _positions[i] = new Color(pos[i].x - vfxPos.x, pos[i].y - vfxPos.y, pos[i].z - vfxPos.z, 0);
            }
            else
            {
                _positions[i] = new Color(0, 0, 0, 0);
            }
        }
        
        _texture.SetPixels(_positions);
        _texture.Apply();
        
        _currentVFX.SetTexture("PositionsTexture", _texture);
        //_lidarVFX.SendEvent("Spawn");
        _currentVFX.Reinit();
    }

    private void NewVisualEffect()
    {
        if (!createNewVFX) return;
        _vfxList.Add(_currentVFX);
        _currentVFX = Instantiate(_vfxPrefab, transform.position, Quaternion.identity, _vfxContainer.transform).GetComponent<VisualEffect>();
        _currentVFX.SetUInt("ParticleCount", (uint)particleCount);
        
        _texture = new Texture2D(particleCount, 1, TextureFormat.RGBAFloat, false);
        _positions = new Color[particleCount];
        _positionsList.Clear();
        
        createNewVFX = false;
    }
}
