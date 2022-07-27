using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class Scanner : MonoBehaviour
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
        // Get InputAction from PlayerInput
        _fire = playerInput.actions["Fire"];
        createNewVFX = true;
        CreateNewVisualEffect();
    }
    
    private void Update()
    {
        // only call if button is pressed
        if (!_fire.IsPressed()) return;
        
        for (int i = 0; i < _pointsPerScan; i++)
        {
            // generate random point
            Vector2 randomPoint = Random.insideUnitCircle * _radius;
            Vector3 castPointPosition = _castPoint.position;
            Vector3 randomPoint3D = new (randomPoint.x + castPointPosition.x, randomPoint.y + castPointPosition.y, castPointPosition.z);
            
            // calculate direction to random point
            Vector3 dir = (randomPoint3D - transform.position).normalized;
            
            // cast ray
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, _range))
            {
                Debug.DrawRay(transform.position, dir * hit.distance, Color.green);
                // only add point if the particle count limit is not reached
                if (_positionsList.Count < particleCount)
                {
                    _positionsList.Add(hit.point);
                }
                // create new VFX if the particle count limit is reached
                else
                {
                    createNewVFX = true;
                    CreateNewVisualEffect();
                    break;
                }
            }
            else
            {
                Debug.DrawRay(transform.position, dir * _range, Color.red);
            }
        }
        ApplyPositions();
    }

    private void ApplyPositions()
    {
        // create array from list
        Vector3[] pos = _positionsList.ToArray();
        
        // cache position for offset
        Vector3 vfxPos = _currentVFX.transform.position;
        
        // loop through all positions and encode them into a Color array
        for (int i = 0; i < particleCount; i++)
        {
            // encode the positions we have and make the rest Color.clear
            if (i < pos.Length)
            {
                _positions[i] = new Color(pos[i].x - vfxPos.x, pos[i].y - vfxPos.y, pos[i].z - vfxPos.z, 0);
            }
            else
            {
                _positions[i] = new Color(0, 0, 0, 0);
            }
        }
        
        // apply to texture
        _texture.SetPixels(_positions);
        _texture.Apply();
        
        // apply to VFX
        _currentVFX.SetTexture("PositionsTexture", _texture);
        _currentVFX.Reinit();
    }

    private void CreateNewVisualEffect() // this is fucking performance heavy help
    {
        // make sure it only gets called once
        if (!createNewVFX) return;
        
        // add old VFX to list
        _vfxList.Add(_currentVFX);
        
        // create new VFX
        _currentVFX = Instantiate(_vfxPrefab, transform.position, Quaternion.identity, _vfxContainer.transform).GetComponent<VisualEffect>();
        _currentVFX.SetUInt("ParticleCount", (uint)particleCount);
        
        // create texture
        _texture = new Texture2D(particleCount, 1, TextureFormat.RGBAFloat, false);
        
        // create color array for positions
        _positions = new Color[particleCount];
        
        // clear list
        _positionsList.Clear();
        
        createNewVFX = false;
    }
}
