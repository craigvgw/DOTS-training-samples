﻿using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class PheromoneRendering : MonoBehaviour
{
    [Header("Update Bools")]
    [SerializeField] private bool _randomizePheromones = false;
    [SerializeField] private bool _allowPheromoneDynamicBuffer = false;

    [Header("Map Properties")]
    [SerializeField] private Material m_Mat = null;
    [SerializeField] public static int _resolution = 128;
    [SerializeField] public static float _worldSize = 128;
    [SerializeField] public static float2 _worldOffset = 128;

    // Private Variables
    Texture2D m_VisTexture = null;
    byte[] m_PheromoneArray = null;


    #region // MonoBehaviour Events
    private void Start()
    {
        m_PheromoneArray = new byte[_resolution * _resolution];
    }

    private void Update()
    {
        if(_randomizePheromones)
        {
            GenerateRandomData();
            SetPheromoneArray(m_PheromoneArray);
        }
    }

    private void OnDisable()
    {
        if (m_VisTexture == null) return;
        Texture2D.Destroy(m_VisTexture);
    }
    #endregion


    #region // MonoBehaviour Random Data Generation
    private void GenerateRandomData()
    {
        for (int i = 0; i < m_PheromoneArray.Length; i++)
        {
            m_PheromoneArray[i] = (byte)UnityEngine.Random.Range(0, 255);
        }
    }

    // This byteArray is generated by this MonoBehaviour
    public void SetPheromoneArray(byte[] byteArray)
    {
        CheckTextureInit();

        m_VisTexture.SetPixelData(byteArray, 0, 0);
        m_VisTexture.Apply();
    }

    #endregion


    // This dynamic buffer is generated by the PheromoneSystem
    public void SetPheromoneArray(DynamicBuffer<PheromoneStrength> pheromoneBuffer)
    {
        if (_allowPheromoneDynamicBuffer)
        {
            CheckTextureInit();

            // OPTIMIZATION: Can we set the Dynamic Buffer to use bytes instead of floats, and just pass it directly through to SetPixelData? (SetPixelData didn't like the fact that pheromoneBuffer type wasn't implicit?)
            byte[] newByteArray = new byte[pheromoneBuffer.Length];

            for (int i = 0; i < pheromoneBuffer.Length; i++)
            {
                newByteArray[i] = (byte)pheromoneBuffer[i];
            }

            m_VisTexture.SetPixelData(newByteArray, 0, 0);

            m_VisTexture.Apply();
        }
    }

    // Initialize the texture
    public void CheckTextureInit()
    {
        if(m_VisTexture != null && m_VisTexture.width != _resolution)
        {
            Texture2D.Destroy(m_VisTexture);
            m_VisTexture = null;
        }

        if (m_VisTexture == null)
        {
            m_VisTexture = new Texture2D(_resolution, _resolution, TextureFormat.R8, mipChain: false, linear: true);
        }

        m_Mat.mainTexture =m_VisTexture;
    }
}