/*
 * Author: Leonhard Robin Schnaitl
 * GitHub: https://github.com/leonhardrobin
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace LRS
{
    [RequireComponent(typeof(LineRenderer))]
    public class ScannerAdvanced : MonoBehaviour
    {
        private InputAction _fire;
        private InputAction _changeRadius;
        private LineRenderer _lineRenderer;

        [SerializeField] private List<PointsData> pointsData = new();

        private const string REJECT_LAYER_NAME = "PointReject";
        //private const string PLAYER_TAG = "Player";
        private const string TEXTURE_NAME = "PositionsTexture";
        private const string RESOLUTION_PARAMETER_NAME = "Resolution";
        //private const string PARTICLE_AMOUNT_PARAMETER_NAME = "ParticleAmount";
        //private const string PARTICLES_PER_SCAN_PARAMETER_NAME = "ParticlesPerScan";

        [SerializeField] private bool reuseOldParticles = false;
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private GameObject vfxContainer;
        [SerializeField] private Transform castPoint;
        [SerializeField] private float radius = 10f;
        [SerializeField] private float maxRadius = 10f;
        [SerializeField] private float minRadius = 1f;
        [SerializeField] private int pointsPerScan = 100;
        [SerializeField] private float range = 10f;

        [SerializeField] private int resolution = 100;
        
        // safety check -> don't call NewVisualEffect more than once
        private bool _createNewVFX;

        private void Start()
        {
            // Get InputAction from PlayerInput
            _fire = playerInput.actions["Fire"];
            _changeRadius = playerInput.actions["Scroll"];
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;

            pointsData.ForEach(data =>
            {
                data.ClearData();
                _createNewVFX = true;
                data.currentVisualEffect = NewVisualEffect(data.prefab, out data.texture, out data.positionsAsColors);
                ApplyPositions(data.positionsList, data.currentVisualEffect, data.texture, data.positionsAsColors);
            });
        }
        
        private void FixedUpdate()
        {
            Scan();
            ChangeRadius();
        }

        private void ChangeRadius()
        {
            if (_changeRadius.triggered)
            {
                radius = Mathf.Clamp(radius + _changeRadius.ReadValue<float>() * Time.deltaTime, minRadius, maxRadius);
            }
        }

        private void ApplyPositions(List<Vector3> positionsList, VisualEffect currentVFX, Texture2D texture, Color[] positions)
        {
            // create array from list
            Vector3[] pos = positionsList.ToArray();
            
            // cache position for offset
            Vector3 vfxPos = currentVFX.transform.position;
            
            // cache transform position
            Vector3 transformPos = transform.position;
            
            // cache some more stuff for faster access
            int loopLength = texture.width * texture.height;
            int posListLen = pos.Length;

            for (int i = 0; i < loopLength; i++)
            {
                Color data;

                if (i < posListLen - 1)
                {
                    data = new Color(pos[i].x - vfxPos.x, pos[i].y - vfxPos.y, pos[i].z - vfxPos.z, 1);
                }
                else
                {
                    data = new Color(0, 0, 0, 0);
                }
                positions[i] = data;
            }
            
            // apply to texture
            texture.SetPixels(positions);
            texture.Apply();
            
            // apply to VFX
            currentVFX.SetTexture(TEXTURE_NAME, texture);
            currentVFX.Reinit();
        }

        private VisualEffect NewVisualEffect(VisualEffect visualEffect, out Texture2D texture, out Color[] positions) // this is fucking performance heavy help
        {
            if (!_createNewVFX)
            {
                texture = null;
                positions = new Color[] {};
                return null;
            }
            
            // create new VFX
            VisualEffect vfx = Instantiate(visualEffect, transform.position, Quaternion.identity, vfxContainer.transform);
            vfx.SetUInt(RESOLUTION_PARAMETER_NAME, (uint)resolution);
            
            // create texture
            texture = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
            
            // create color array for positions
            positions = new Color[resolution * resolution];

            _createNewVFX = false;

            return vfx;
        }
        
        private void Scan()
        {
            // only call if button is pressed
            if (_fire.IsPressed())
            {
                for (int i = 0; i < pointsPerScan; i++)
                {
                    // generate random point
                    Vector3 randomPoint = Random.insideUnitSphere * radius;
                    randomPoint += castPoint.position;

                    // calculate direction to random point
                    Vector3 dir = (randomPoint - transform.position).normalized;

                    // cast ray
                    if (Physics.Raycast(transform.position, dir, out RaycastHit hit, range, layerMask))
                    {
                        if (hit.collider.CompareTag(REJECT_LAYER_NAME)) continue;
                        // On Hit
                        // check which color was hit
                        int resolution2 = resolution * resolution;
                        pointsData.ForEach(data =>
                        {
                            data.includedTags.ForEach(tag =>
                            {
                                if (hit.collider.CompareTag(tag))
                                {
                                    if (data.positionsList.Count < resolution2)
                                    {
                                        data.positionsList.Add(hit.point);
                                    }
                                    else if (reuseOldParticles)
                                    {
                                        data.positionsList.RemoveAt(0);
                                        data.positionsList.Add(hit.point);
                                    }
                                    else
                                    {
                                        _createNewVFX = true;
                                        data.currentVisualEffect = NewVisualEffect(data.prefab, out data.texture, out data.positionsAsColors);
                                        data.positionsList.Clear();
                                    }
                                }
                            });
                        });
                        _lineRenderer.enabled = true;
                        _lineRenderer.SetPositions(new[]
                        {
                            transform.position,
                            hit.point
                        });
                    } // raycast
                    else
                    {
                        Debug.DrawRay(transform.position, dir * range, Color.red);
                    }
                } // for loop
                // Apply positions to VFX
                pointsData.ForEach(data =>
                {
                    ApplyPositions(data.positionsList, data.currentVisualEffect, data.texture, data.positionsAsColors);
                });
            } // button press
            else
            {
                _lineRenderer.enabled = false;
            }
        }
    }
}

