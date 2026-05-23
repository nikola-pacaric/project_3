using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Warblade.Data;
using Warblade.Entities;

namespace Warblade.Editor
{
    [CustomEditor(typeof(BossData))]
    public class BossDataEditor : UnityEditor.Editor
    {
        private static bool _drawScenePreview = true;
        private static int _previewPhaseIndex;

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawScenePreview;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawScenePreview;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BossData bossData = (BossData)target;
            int phaseCount = bossData.Phases?.Count ?? 0;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);
            _drawScenePreview = EditorGUILayout.Toggle("Draw Movement Preview", _drawScenePreview);
            using (new EditorGUI.DisabledScope(phaseCount == 0))
            {
                _previewPhaseIndex = EditorGUILayout.IntSlider(
                    "Preview Phase Index",
                    Mathf.Clamp(_previewPhaseIndex, 0, Mathf.Max(0, phaseCount - 1)),
                    0,
                    Mathf.Max(0, phaseCount - 1));
            }

            if (phaseCount == 0)
            {
                EditorGUILayout.HelpBox("Add at least one boss phase to preview movement.", MessageType.Info);
            }
            else
            {
                BossPhaseData phase = bossData.Phases[_previewPhaseIndex];
                string phaseName = phase == null ? "Missing phase" : phase.PhaseName;
                EditorGUILayout.HelpBox(
                    $"Scene view previews phase {_previewPhaseIndex}: {phaseName}. " +
                    "Any scene Boss component using this BossData will draw its entry and movement path.",
                    MessageType.None);
            }
        }

        private void DrawScenePreview(SceneView sceneView)
        {
            if (!_drawScenePreview || target == null)
            {
                return;
            }

            BossData bossData = (BossData)target;
            if (bossData.Phases == null || bossData.Phases.Count == 0)
            {
                return;
            }

            _previewPhaseIndex = Mathf.Clamp(_previewPhaseIndex, 0, bossData.Phases.Count - 1);
            BossPhaseData phase = bossData.Phases[_previewPhaseIndex];
            if (phase == null)
            {
                return;
            }

            List<Boss> matchingBosses = FindBossesUsingData(bossData);
            if (matchingBosses.Count == 0)
            {
                DrawBossDataPreviewAtAuthoredPosition(bossData, phase);
                return;
            }

            for (int i = 0; i < matchingBosses.Count; i++)
            {
                DrawBossPreview(matchingBosses[i], bossData, phase);
            }
        }

        private static List<Boss> FindBossesUsingData(BossData bossData)
        {
            List<Boss> matches = new List<Boss>();
            Boss[] bosses = Object.FindObjectsByType<Boss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
            {
                Boss boss = bosses[i];
                if (boss != null && boss.Data == bossData)
                {
                    matches.Add(boss);
                }
            }

            return matches;
        }

        private static void DrawBossDataPreviewAtAuthoredPosition(BossData bossData, BossPhaseData phase)
        {
            DrawEntryPreview(bossData.EntryStartPosition, bossData.EntryTargetPosition);
            DrawMovementPreview(bossData.EntryTargetPosition, phase);

            Handles.color = Color.white;
            Handles.Label(
                bossData.EntryTargetPosition + new Vector2(0f, 0.45f),
                $"{bossData.DisplayName} preview\nNo scene Boss currently references this asset.");
        }

        private static void DrawBossPreview(Boss boss, BossData bossData, BossPhaseData phase)
        {
            Vector2 entryStart = bossData.EntryStartPosition;
            Vector2 entryTarget = bossData.EntryTargetPosition;

            DrawEntryPreview(entryStart, entryTarget);
            DrawMovementPreview(entryTarget, phase);

            Handles.color = Color.white;
            Handles.Label(
                entryTarget + new Vector2(0f, 0.45f),
                $"{boss.name}\n{bossData.DisplayName} phase preview");
        }

        private static void DrawEntryPreview(Vector2 entryStart, Vector2 entryTarget)
        {
            Handles.color = new Color(0.25f, 0.75f, 1f, 0.95f);
            Handles.DrawWireDisc(entryStart, Vector3.forward, 0.18f);
            Handles.DrawLine(entryStart, entryTarget);

            Handles.color = new Color(0.2f, 1f, 0.45f, 0.95f);
            Handles.DrawWireDisc(entryTarget, Vector3.forward, 0.22f);
        }

        private static void DrawMovementPreview(Vector2 center, BossPhaseData phase)
        {
            Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);

            switch (BossMovement.ResolveMovementBehavior(phase.MovementBehavior))
            {
                case BossMovementBehavior.HorizontalPatrol:
                    DrawHorizontalPatrol(center, phase);
                    break;

                case BossMovementBehavior.FigureEight:
                    DrawFigureEight(center, phase);
                    break;

                case BossMovementBehavior.DashAndPause:
                    DrawDashAndPause(center, phase);
                    break;

                case BossMovementBehavior.DiveSweep:
                    DrawDiveSweep(center, phase);
                    break;

                case BossMovementBehavior.LaneSwitch:
                    DrawLaneSwitch(center, phase);
                    break;

                case BossMovementBehavior.PlayerShadow:
                    DrawPlayerShadow(center, phase);
                    break;

                case BossMovementBehavior.BoxPatrol:
                    DrawBoxPatrol(center, phase);
                    break;
            }
        }

        private static void DrawHorizontalPatrol(Vector2 center, BossPhaseData phase)
        {
            Vector2 left = center + Vector2.left * phase.MovementAmplitude;
            Vector2 right = center + Vector2.right * phase.MovementAmplitude;
            Handles.DrawLine(left, right);
            DrawDisc(left, 0.12f);
            DrawDisc(right, 0.12f);
        }

        private static void DrawFigureEight(Vector2 center, BossPhaseData phase)
        {
            const int sampleCount = 72;
            Vector3[] points = new Vector3[sampleCount + 1];
            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount * Mathf.PI * 2f;
                points[i] = center + new Vector2(
                    Mathf.Sin(t) * phase.MovementAmplitude,
                    Mathf.Sin(t * 2f) * phase.VerticalMovementAmplitude);
            }

            Handles.DrawAAPolyLine(3f, points);
        }

        private static void DrawDashAndPause(Vector2 center, BossPhaseData phase)
        {
            Vector2[] points =
            {
                center + new Vector2(-phase.MovementAmplitude, 0f),
                center + new Vector2(phase.MovementAmplitude, -phase.VerticalMovementAmplitude),
                center + new Vector2(phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                center + new Vector2(-phase.MovementAmplitude, -phase.VerticalMovementAmplitude)
            };

            DrawLoop(points);
            DrawPointMarkers(points, 0.13f);
        }

        private static void DrawDiveSweep(Vector2 center, BossPhaseData phase)
        {
            const int sampleCount = 36;
            Vector3[] points = new Vector3[sampleCount + 1];
            for (int i = 0; i <= sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float x = Mathf.Lerp(
                    -phase.MovementAmplitude,
                    phase.MovementAmplitude,
                    progress);
                float y = -Mathf.Sin(progress * Mathf.PI) * phase.VerticalMovementAmplitude;
                points[i] = center + new Vector2(x, y);
            }

            Handles.DrawAAPolyLine(3f, points);
        }

        private static void DrawLaneSwitch(Vector2 center, BossPhaseData phase)
        {
            int laneCount = Mathf.Max(2, phase.MovementLaneCount);
            Vector2 left = center + Vector2.left * phase.MovementAmplitude;
            Vector2 right = center + Vector2.right * phase.MovementAmplitude;
            Handles.DrawLine(left, right);

            for (int i = 0; i < laneCount; i++)
            {
                float laneT = i / (float)(laneCount - 1);
                Vector2 lanePosition = new Vector2(
                    Mathf.Lerp(left.x, right.x, laneT),
                    center.y);
                DrawDisc(lanePosition, 0.12f);
            }
        }

        private static void DrawPlayerShadow(Vector2 center, BossPhaseData phase)
        {
            Vector2 left = center + Vector2.left * phase.MovementAmplitude;
            Vector2 right = center + Vector2.right * phase.MovementAmplitude;
            Handles.DrawLine(left, right);

            Handles.color = new Color(1f, 0.45f, 0.2f, 0.85f);
            float easedRange = phase.MovementAmplitude * phase.PlayerShadowStrength;
            Handles.DrawLine(
                center + Vector2.left * easedRange,
                center + Vector2.right * easedRange);
        }

        private static void DrawBoxPatrol(Vector2 center, BossPhaseData phase)
        {
            Vector2[] points =
            {
                center + new Vector2(-phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                center + new Vector2(phase.MovementAmplitude, phase.VerticalMovementAmplitude),
                center + new Vector2(phase.MovementAmplitude, -phase.VerticalMovementAmplitude),
                center + new Vector2(-phase.MovementAmplitude, -phase.VerticalMovementAmplitude)
            };

            DrawLoop(points);
            DrawPointMarkers(points, 0.12f);
        }

        private static void DrawLoop(Vector2[] points)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            for (int i = 1; i < points.Length; i++)
            {
                Handles.DrawLine(points[i - 1], points[i]);
            }

            Handles.DrawLine(points[points.Length - 1], points[0]);
        }

        private static void DrawPointMarkers(Vector2[] points, float radius)
        {
            if (points == null)
            {
                return;
            }

            for (int i = 0; i < points.Length; i++)
            {
                DrawDisc(points[i], radius);
            }
        }

        private static void DrawDisc(Vector2 position, float radius)
        {
            Handles.DrawWireDisc(position, Vector3.forward, radius);
        }
    }
}
