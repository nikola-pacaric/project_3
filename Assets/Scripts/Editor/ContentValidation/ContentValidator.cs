using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Warblade.Data;
using Object = UnityEngine.Object;

namespace Warblade.Editor.ContentValidation
{
    public static class ContentValidator
    {
        private const int CampaignLevelCount = 100;

        [MenuItem("Warblade/Validate Content")]
        public static void ValidateCurrentContent()
        {
            ValidationReport report = Validate(strictCampaignGate: false);
            ShowSummary(report, "Current content validation");
        }

        [MenuItem("Warblade/Validate M6 Gate (1-100)")]
        public static void ValidateM6Gate()
        {
            ValidationReport report = Validate(strictCampaignGate: true);
            ShowSummary(report, "M6 gate validation");
        }

        public static ValidationReport Validate(bool strictCampaignGate)
        {
            ValidationReport report = new ValidationReport();
            LevelData[] levels = LoadAllAssets<LevelData>();
            WaveData[] waves = LoadAllAssets<WaveData>();
            BossData[] bosses = LoadAllAssets<BossData>();

            ValidateWaves(waves, report);
            ValidateLevels(levels, strictCampaignGate, report);
            ValidateBosses(bosses, strictCampaignGate, report);

            string mode = strictCampaignGate ? "M6 gate" : "current content";
            Debug.Log(
                $"[ContentValidator] Completed {mode} validation. " +
                $"Errors: {report.ErrorCount}, Warnings: {report.WarningCount}.");

            return report;
        }

        private static void ValidateWaves(IReadOnlyList<WaveData> waves, ValidationReport report)
        {
            if (waves == null || waves.Count == 0)
            {
                report.Error("No WaveData assets found.", null);
                return;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                WaveData wave = waves[i];
                if (wave == null)
                {
                    report.Error("A WaveData asset failed to load.", null);
                    continue;
                }

                if (wave.SlotCount <= 0)
                {
                    report.Error("Wave has no slots.", wave);
                    continue;
                }

                for (int slotIndex = 0; slotIndex < wave.SlotCount; slotIndex++)
                {
                    WaveData.WaveSlot slot = wave.GetSlot(slotIndex);
                    if (slot.EnemyData == null)
                    {
                        report.Error($"Wave slot {slotIndex} has no EnemyData.", wave);
                    }
                }

                if (wave.PathMode == WaveData.EntryPathMode.WaypointPath && wave.EntryPathWaypointCount <= 0)
                {
                    report.Warning("Waypoint Path mode is selected but the wave has no shared waypoints.", wave);
                }

                if (wave.PathMode == WaveData.EntryPathMode.SimpleBezier && wave.EntryPathWaypointCount > 0)
                {
                    report.Warning("Wave has entry path waypoints, but Simple Bezier mode ignores them.", wave);
                }

                if (wave.UsesWaypointEntryPath && wave.EntrySpacing > 0f)
                {
                    report.Warning("Waypoint Path mode uses one shared entry start, so Entry Spacing is ignored.", wave);
                }

                if (wave.MotionMode == WaveData.FormationMotionMode.HorizontalSway)
                {
                    if (wave.FormationSwayAmplitude <= 0f)
                    {
                        report.Warning("Horizontal Sway is selected but Formation Sway Amplitude is 0.", wave);
                    }

                    if (wave.FormationSwaySpeed <= 0f)
                    {
                        report.Warning("Horizontal Sway is selected but Formation Sway Speed is 0.", wave);
                    }
                }
            }
        }

        private static void ValidateLevels(
            IReadOnlyList<LevelData> levels,
            bool strictCampaignGate,
            ValidationReport report)
        {
            if (levels == null || levels.Count == 0)
            {
                report.Error("No LevelData assets found.", null);
                return;
            }

            Dictionary<int, List<LevelData>> levelsByNumber = new Dictionary<int, List<LevelData>>();
            int highestLevel = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelData level = levels[i];
                if (level == null)
                {
                    report.Error("A LevelData asset failed to load.", null);
                    continue;
                }

                int levelNumber = level.LevelNumber;
                highestLevel = Mathf.Max(highestLevel, levelNumber);
                if (!levelsByNumber.TryGetValue(levelNumber, out List<LevelData> matchingLevels))
                {
                    matchingLevels = new List<LevelData>();
                    levelsByNumber.Add(levelNumber, matchingLevels);
                }

                matchingLevels.Add(level);
                ValidateLevelWaves(level, report);
            }

            foreach (KeyValuePair<int, List<LevelData>> pair in levelsByNumber)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    report.Error($"Duplicate LevelData.LevelNumber {pair.Key}.", pair.Value[i]);
                }
            }

            int requiredMaxLevel = strictCampaignGate ? CampaignLevelCount : highestLevel;
            for (int levelNumber = 1; levelNumber <= requiredMaxLevel; levelNumber++)
            {
                if (!levelsByNumber.ContainsKey(levelNumber))
                {
                    string message = strictCampaignGate
                        ? $"Missing LevelData for campaign level {levelNumber}."
                        : $"Missing LevelData inside authored range at level {levelNumber}.";
                    report.Error(message, null);
                }
            }
        }

        private static void ValidateLevelWaves(LevelData level, ValidationReport report)
        {
            IReadOnlyList<WaveData> waves = level.Waves;
            if (waves == null || waves.Count == 0)
            {
                report.Error("LevelData has an empty wave list.", level);
                return;
            }

            for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
            {
                if (waves[waveIndex] == null)
                {
                    report.Error($"LevelData has a missing WaveData reference at index {waveIndex}.", level);
                }
            }
        }

        private static void ValidateBosses(
            IReadOnlyList<BossData> bosses,
            bool strictCampaignGate,
            ValidationReport report)
        {
            if (bosses == null || bosses.Count == 0)
            {
                report.Warning("No BossData assets found.", null);
                return;
            }

            if (strictCampaignGate && bosses.Count < 4)
            {
                report.Error("M6 gate expects at least four BossData assets for levels 25, 50, 75, and 100.", null);
            }

            for (int bossIndex = 0; bossIndex < bosses.Count; bossIndex++)
            {
                BossData boss = bosses[bossIndex];
                if (boss == null)
                {
                    report.Error("A BossData asset failed to load.", null);
                    continue;
                }

                IReadOnlyList<BossPhaseData> phases = boss.Phases;
                if (phases == null || phases.Count == 0)
                {
                    report.Error("BossData has no phases.", boss);
                    continue;
                }

                for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
                {
                    BossPhaseData phase = phases[phaseIndex];
                    if (phase == null)
                    {
                        report.Error($"BossData has a null phase at index {phaseIndex}.", boss);
                        continue;
                    }

                    if (phase.AttackPatterns == null || phase.AttackPatterns.Count == 0)
                    {
                        report.Error($"Boss phase '{phase.PhaseName}' has no attack patterns.", boss);
                        continue;
                    }

                    for (int patternIndex = 0; patternIndex < phase.AttackPatterns.Count; patternIndex++)
                    {
                        if (phase.AttackPatterns[patternIndex] == null)
                        {
                            report.Error(
                                $"Boss phase '{phase.PhaseName}' has a missing attack pattern at index {patternIndex}.",
                                boss);
                        }
                    }
                }

                if (boss.RewardDropTable == null)
                {
                    report.Warning("BossData has no reward drop table.", boss);
                }
            }
        }

        private static T[] LoadAllAssets<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets.ToArray();
        }

        private static void ShowSummary(ValidationReport report, string title)
        {
            string message = report.ErrorCount == 0
                ? $"Validation finished with {report.WarningCount} warning(s)."
                : $"Validation found {report.ErrorCount} error(s) and {report.WarningCount} warning(s).";

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        public sealed class ValidationReport
        {
            public int ErrorCount { get; private set; }
            public int WarningCount { get; private set; }

            public void Error(string message, Object context)
            {
                ErrorCount++;
                Debug.LogError(FormatMessage(message, context), context);
            }

            public void Warning(string message, Object context)
            {
                WarningCount++;
                Debug.LogWarning(FormatMessage(message, context), context);
            }

            private static string FormatMessage(string message, Object context)
            {
                if (context == null)
                {
                    return $"[ContentValidator] {message}";
                }

                string path = AssetDatabase.GetAssetPath(context);
                return $"[ContentValidator] {message} Asset: {path}";
            }
        }
    }
}
