using System.Collections.Generic;
using UnityEngine;
using Warblade.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Warblade.Systems
{
    /// <summary>
    /// Draws Scene View guides for authoring wave formations against camera bounds, HUD rails, and player-safe space.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GameplayAuthoringGuide : MonoBehaviour
    {
        private const float DefaultReferenceAspect = 16f / 9f;
        private const float MinimumRectSize = 0.01f;

        [Header("Camera Bounds")]
        [SerializeField] private Camera _referenceCamera;
        [SerializeField, Min(0.01f)] private float _fallbackOrthographicSize = 5f;
        [SerializeField, Min(0.01f)] private float _fallbackAspect = DefaultReferenceAspect;
        [SerializeField] private Vector2 _fallbackCameraCenter;

        [Header("Reserved Areas")]
        [Tooltip("World-space width reserved by the left HUD rail inside the camera frame.")]
        [SerializeField, Min(0f)] private float _leftHudReservedWidth = 1.7f;
        [Tooltip("World-space width reserved by the right HUD rail inside the camera frame.")]
        [SerializeField, Min(0f)] private float _rightHudReservedWidth = 1.7f;
        [Tooltip("World-space height reserved near the player at the bottom of the camera frame.")]
        [SerializeField, Min(0f)] private float _bottomPlayerReserveHeight = 1.5f;
        [Tooltip("Small top margin so formations do not sit directly on the visible camera edge.")]
        [SerializeField, Min(0f)] private float _topFormationMargin = 0.25f;

        [Header("Formation Sway")]
        [Tooltip("Minimum world-space horizontal padding applied to both sides of the safe formation area.")]
        [SerializeField, Min(0f)] private float _minimumFormationSwayPadding = 0.5f;
        [Tooltip("Optional level data used to preview the largest horizontal sway amplitude in that level.")]
        [SerializeField] private LevelData _levelDataForSwayPadding;
        [SerializeField] private bool _useLargestSwayFromLevelData = true;

        [Header("Drawing")]
        [SerializeField] private bool _drawCameraFrame = true;
        [SerializeField] private bool _drawHudReservedAreas = true;
        [SerializeField] private bool _drawPlayerReserve = true;
        [SerializeField] private bool _drawSafeFormationArea = true;
        [SerializeField] private bool _drawCenterLines = true;
        [SerializeField] private bool _drawLabels = true;
        [SerializeField, Min(0f)] private float _z = 0f;

        [Header("Colors")]
        [SerializeField] private Color _cameraFrameColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color _hudReservedColor = new Color(1f, 0.15f, 0.1f, 0.16f);
        [SerializeField] private Color _playerReserveColor = new Color(1f, 0.85f, 0.15f, 0.14f);
        [SerializeField] private Color _safeFormationColor = new Color(0.2f, 1f, 0.45f, 0.12f);
        [SerializeField] private Color _safeFormationOutlineColor = new Color(0.2f, 1f, 0.45f, 0.9f);
        [SerializeField] private Color _centerLineColor = new Color(0.35f, 0.85f, 1f, 0.45f);

        private void OnValidate()
        {
            _fallbackOrthographicSize = Mathf.Max(0.01f, _fallbackOrthographicSize);
            _fallbackAspect = Mathf.Max(0.01f, _fallbackAspect);
            _leftHudReservedWidth = Mathf.Max(0f, _leftHudReservedWidth);
            _rightHudReservedWidth = Mathf.Max(0f, _rightHudReservedWidth);
            _bottomPlayerReserveHeight = Mathf.Max(0f, _bottomPlayerReserveHeight);
            _topFormationMargin = Mathf.Max(0f, _topFormationMargin);
            _minimumFormationSwayPadding = Mathf.Max(0f, _minimumFormationSwayPadding);
        }

        private void OnDrawGizmos()
        {
            Rect cameraRect = ResolveCameraRect();
            float swayPadding = ResolveEffectiveSwayPadding();
            Rect safeFormationRect = ResolveSafeFormationRect(cameraRect, swayPadding);

            if (_drawHudReservedAreas)
            {
                DrawHudReservedAreas(cameraRect);
            }

            if (_drawPlayerReserve)
            {
                DrawBottomRect(cameraRect, _bottomPlayerReserveHeight, _playerReserveColor);
            }

            if (_drawSafeFormationArea)
            {
                DrawFilledRect(safeFormationRect, _safeFormationColor);
                DrawWireRect(safeFormationRect, _safeFormationOutlineColor);
            }

            if (_drawCameraFrame)
            {
                DrawWireRect(cameraRect, _cameraFrameColor);
            }

            if (_drawCenterLines)
            {
                DrawCenterLines(cameraRect);
            }

#if UNITY_EDITOR
            if (_drawLabels)
            {
                DrawLabels(cameraRect, safeFormationRect, swayPadding);
            }
#endif
        }

        private Rect ResolveCameraRect()
        {
            if (_referenceCamera != null && _referenceCamera.orthographic)
            {
                float halfHeight = _referenceCamera.orthographicSize;
                float halfWidth = halfHeight * _referenceCamera.aspect;
                Vector3 center = _referenceCamera.transform.position;
                return Rect.MinMaxRect(
                    center.x - halfWidth,
                    center.y - halfHeight,
                    center.x + halfWidth,
                    center.y + halfHeight);
            }

            float fallbackHalfHeight = _fallbackOrthographicSize;
            float fallbackHalfWidth = fallbackHalfHeight * _fallbackAspect;
            return Rect.MinMaxRect(
                _fallbackCameraCenter.x - fallbackHalfWidth,
                _fallbackCameraCenter.y - fallbackHalfHeight,
                _fallbackCameraCenter.x + fallbackHalfWidth,
                _fallbackCameraCenter.y + fallbackHalfHeight);
        }

        private Rect ResolveSafeFormationRect(Rect cameraRect, float swayPadding)
        {
            float minX = cameraRect.xMin + _leftHudReservedWidth + swayPadding;
            float maxX = cameraRect.xMax - _rightHudReservedWidth - swayPadding;
            float minY = cameraRect.yMin + _bottomPlayerReserveHeight;
            float maxY = cameraRect.yMax - _topFormationMargin;

            if (maxX < minX)
            {
                float centerX = (minX + maxX) * 0.5f;
                minX = centerX - MinimumRectSize * 0.5f;
                maxX = centerX + MinimumRectSize * 0.5f;
            }

            if (maxY < minY)
            {
                float centerY = (minY + maxY) * 0.5f;
                minY = centerY - MinimumRectSize * 0.5f;
                maxY = centerY + MinimumRectSize * 0.5f;
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private float ResolveEffectiveSwayPadding()
        {
            float padding = _minimumFormationSwayPadding;

            if (!_useLargestSwayFromLevelData || _levelDataForSwayPadding == null)
            {
                return padding;
            }

            IReadOnlyList<WaveData> waves = _levelDataForSwayPadding.Waves;
            for (int i = 0; i < waves.Count; i++)
            {
                WaveData wave = waves[i];
                if (wave == null || wave.MotionMode != WaveData.FormationMotionMode.HorizontalSway)
                {
                    continue;
                }

                padding = Mathf.Max(padding, wave.FormationSwayAmplitude);
            }

            return padding;
        }

        private void DrawHudReservedAreas(Rect cameraRect)
        {
            if (_leftHudReservedWidth > 0f)
            {
                Rect leftHud = Rect.MinMaxRect(
                    cameraRect.xMin,
                    cameraRect.yMin,
                    Mathf.Min(cameraRect.xMin + _leftHudReservedWidth, cameraRect.xMax),
                    cameraRect.yMax);

                DrawFilledRect(leftHud, _hudReservedColor);
                DrawWireRect(leftHud, WithAlpha(_hudReservedColor, 0.75f));
            }

            if (_rightHudReservedWidth > 0f)
            {
                Rect rightHud = Rect.MinMaxRect(
                    Mathf.Max(cameraRect.xMax - _rightHudReservedWidth, cameraRect.xMin),
                    cameraRect.yMin,
                    cameraRect.xMax,
                    cameraRect.yMax);

                DrawFilledRect(rightHud, _hudReservedColor);
                DrawWireRect(rightHud, WithAlpha(_hudReservedColor, 0.75f));
            }
        }

        private void DrawBottomRect(Rect cameraRect, float height, Color color)
        {
            if (height <= 0f)
            {
                return;
            }

            Rect rect = Rect.MinMaxRect(
                cameraRect.xMin,
                cameraRect.yMin,
                cameraRect.xMax,
                Mathf.Min(cameraRect.yMin + height, cameraRect.yMax));

            DrawFilledRect(rect, color);
            DrawWireRect(rect, WithAlpha(color, 0.75f));
        }

        private void DrawFilledRect(Rect rect, Color color)
        {
            Color previous = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawCube(GetCenter(rect), GetSize(rect));
            Gizmos.color = previous;
        }

        private void DrawWireRect(Rect rect, Color color)
        {
            Color previous = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(GetCenter(rect), GetSize(rect));
            Gizmos.color = previous;
        }

        private void DrawCenterLines(Rect cameraRect)
        {
            Color previous = Gizmos.color;
            Gizmos.color = _centerLineColor;

            Vector3 verticalBottom = new Vector3(cameraRect.center.x, cameraRect.yMin, _z);
            Vector3 verticalTop = new Vector3(cameraRect.center.x, cameraRect.yMax, _z);
            Vector3 horizontalLeft = new Vector3(cameraRect.xMin, cameraRect.center.y, _z);
            Vector3 horizontalRight = new Vector3(cameraRect.xMax, cameraRect.center.y, _z);

            Gizmos.DrawLine(verticalBottom, verticalTop);
            Gizmos.DrawLine(horizontalLeft, horizontalRight);
            Gizmos.color = previous;
        }

        private Vector3 GetCenter(Rect rect)
        {
            return new Vector3(rect.center.x, rect.center.y, _z);
        }

        private static Vector3 GetSize(Rect rect)
        {
            return new Vector3(rect.width, rect.height, 0f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

#if UNITY_EDITOR
        private void DrawLabels(Rect cameraRect, Rect safeFormationRect, float swayPadding)
        {
            DrawLabel(
                new Vector3(cameraRect.xMin, cameraRect.yMax + 0.15f, _z),
                "Camera frame",
                _cameraFrameColor);

            DrawLabel(
                new Vector3(safeFormationRect.center.x, safeFormationRect.yMax + 0.15f, _z),
                $"Safe formation area (sway padding {swayPadding:0.##})",
                _safeFormationOutlineColor);

            if (_drawHudReservedAreas)
            {
                DrawLabel(
                    new Vector3(cameraRect.xMin + _leftHudReservedWidth * 0.5f, cameraRect.center.y, _z),
                    "HUD",
                    WithAlpha(_hudReservedColor, 0.95f));

                DrawLabel(
                    new Vector3(cameraRect.xMax - _rightHudReservedWidth * 0.5f, cameraRect.center.y, _z),
                    "HUD",
                    WithAlpha(_hudReservedColor, 0.95f));
            }

            if (_drawPlayerReserve)
            {
                DrawLabel(
                    new Vector3(cameraRect.center.x, cameraRect.yMin + _bottomPlayerReserveHeight + 0.12f, _z),
                    "Player reserve",
                    WithAlpha(_playerReserveColor, 0.95f));
            }
        }

        private static void DrawLabel(Vector3 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = color }
            };

            Handles.Label(position, text, style);
        }
#endif
    }
}
