﻿using System.Collections.Generic;
using Magneto.Track;
using UnityEngine;

public class TrackSpawner : MonoBehaviour
{
    private int _frameCounter;
    private TrackManager.TrackGenerationSystem _generator;

    private IntersectionData[] _intersectionData;
    private SplineData[] _splineData;
    private Vector3[] _usedCells;
    private int[] _indices;

    private bool[] _usedCellsRaw;

    // Start is called before the first frame update
    private void Start()
    {
        _generator = new TrackManager.TrackGenerationSystem();
        _generator.Init();
    }

    private void Update()
    {
        if (_generator == null) return;
        if (!_generator.IsRunning)
        {
            _generator.Schedule();
        }
        else if (_frameCounter >= TrackManager.JOB_EXECUTION_MAXIMUM_FRAMES)
        {
            // Completion Stuff
            _generator.Complete(out _intersectionData, out _splineData, out _usedCellsRaw, out _indices);
            var locations = new List<Vector3>();
            
            var count = _usedCellsRaw.Length;
            for (var i = 0; i < count; i++)
                if (_usedCellsRaw[i])
                    locations.Add(GetVector3FromIndex(i));
            _usedCells = locations.ToArray();

            var indicesCount = _indices.Length;
            var locationCount = 0;
            foreach (int i in _indices)
            {
                if (i != -1)
                {
                    locationCount++;
                }
            }
            
            
            Debug.Log($"Active Cells: {locations.Count}");
            Debug.Log($"Intersections Cells: {_intersectionData.Length}");
            Debug.Log($"Splines Data: {_splineData.Length}");
            Debug.Log($"Grid Indices Non -1 Count: {locationCount}");

            _generator = null;
        }
        else
        {
            _frameCounter++;
        }
    }

    private void OnDestroy()
    {
        _generator?.Complete(out _intersectionData, out _splineData, out _usedCellsRaw, out _indices);
        _generator = null;
    }


#if UNITY_EDITOR

    public void OnDrawGizmos()
    {
        var transparent = new Color(1, 1, 1, 0.1f);

        // Should used cells
        if (_usedCells != null && _usedCells.Length > 0)
            foreach (var c in _usedCells)
            {
                Gizmos.color = transparent;
                Gizmos.DrawCube(new Vector3(c.x, c.y, c.z), Vector3.one);
            }

        // Show intersection cells
        if (_intersectionData != null && _intersectionData.Length > 0)
            foreach (var i in _intersectionData)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(new Vector3(i.Position.x, i.Position.y, i.Position.z), Vector3.one);
            }
    }

#endif

    private static Vector3 GetVector3FromIndex(int idx)
    {
        var z = idx / (TrackManager.VOXEL_COUNT * TrackManager.VOXEL_COUNT);
        idx -= z * TrackManager.VOXEL_COUNT * TrackManager.VOXEL_COUNT;
        var y = idx / TrackManager.VOXEL_COUNT;
        var x = idx % TrackManager.VOXEL_COUNT;

        return new Vector3(x, y, z);
    }
}