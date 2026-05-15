using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusPanelUI : MonoBehaviour
{
    [SerializeField] private StatusMarkerUI _markerPrefab;
    [SerializeField] private Transform      _container;
    [SerializeField] private TagDisplayEntry[] _displayTable;

    [Header("레이아웃")]
    [Tooltip("마커 하나의 가로 간격")]
    [SerializeField] private float _cellWidth  = 0.3f;
    [Tooltip("마커 하나의 세로 간격 (양수 = 아래로)")]
    [SerializeField] private float _cellHeight = 0.3f;
    [Tooltip("한 줄에 배치할 최대 마커 수")]
    [SerializeField] private int   _maxPerRow  = 7;

    [Serializable]
    public struct TagDisplayEntry
    {
        public ETagType Type;
        public Sprite   Icon;
        public Color    Color;
    }

    private readonly Dictionary<ETagType, StatusMarkerUI> _markers = new();
    private readonly List<StatusMarkerUI> _activeMarkers = new();

    public void UpdateStatus(ETagType type, int count)
    {
        if (_markerPrefab == null || _container == null) return;

        if (!_markers.TryGetValue(type, out StatusMarkerUI marker))
        {
            if (count == 0) return;
            marker = CreateMarker(type);
        }

        marker.SetCount(count);
        RefreshLayout();
    }

    private StatusMarkerUI CreateMarker(ETagType type)
    {
        Sprite icon  = null;
        Color  color = Color.white;
        foreach (var entry in _displayTable)
            if (entry.Type == type) { icon = entry.Icon; color = entry.Color; break; }

        var marker = Instantiate(_markerPrefab, _container);
        marker.Init(type, icon, color);
        _markers[type] = marker;
        return marker;
    }

    public void ClearAll()
    {
        foreach (var marker in _markers.Values)
            marker.SetCount(0);

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        _activeMarkers.Clear();
        foreach (var marker in _markers.Values)
        {
            if (marker.gameObject.activeSelf)
                _activeMarkers.Add(marker);
        }

        int total = _activeMarkers.Count;
        for (int i = 0; i < total; i++)
        {
            int col = i % _maxPerRow;
            int row = i / _maxPerRow;

            int markersInRow = Mathf.Min(_maxPerRow, total - row * _maxPerRow);
            float rowWidth   = (markersInRow - 1) * _cellWidth;
            float startX     = -rowWidth * 0.5f;

            _activeMarkers[i].transform.localPosition = new Vector3(
                startX + col * _cellWidth,
               -row * _cellHeight,
                0f
            );
        }
    }
}
