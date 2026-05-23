using System;
using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Background Palette Data", fileName = "BackgroundPaletteData")]
    public class BackgroundPaletteData : ScriptableObject
    {
        private const int LevelsPerChapter = 25;

        [Serializable]
        public class ChapterPalette
        {
            [SerializeField] private string _name = "Chapter";

            [Header("Sprite Layers")]
            [SerializeField] private Color _gradientTint = new Color(0.04f, 0.06f, 0.16f, 1f);
            [SerializeField] private Color _farNebulaTint = new Color(0.12f, 0.22f, 0.42f, 0.24f);
            [SerializeField] private Color _nearNebulaTint = new Color(0.25f, 0.52f, 0.85f, 0.16f);

            [Header("Star Layers")]
            [SerializeField] private Color _farStarTint = new Color(0.72f, 0.88f, 1f, 0.55f);
            [SerializeField] private Color _nearStarTint = new Color(1f, 1f, 1f, 0.82f);
            [SerializeField, Min(0f)] private float _farStarEmissionRate = 14f;
            [SerializeField, Min(0f)] private float _nearStarEmissionRate = 7f;
            [SerializeField, Min(0f)] private float _farStarSimulationSpeed = 0.35f;
            [SerializeField, Min(0f)] private float _nearStarSimulationSpeed = 0.75f;

            [Header("Drift")]
            [SerializeField] private Vector2 _farNebulaDriftDirection = new Vector2(1f, 0.18f);
            [SerializeField, Min(0f)] private float _farNebulaDriftDistance = 0.16f;
            [SerializeField, Min(1f)] private float _farNebulaDriftSeconds = 36f;
            [SerializeField] private Vector2 _nearNebulaDriftDirection = new Vector2(-0.7f, 0.25f);
            [SerializeField, Min(0f)] private float _nearNebulaDriftDistance = 0.28f;
            [SerializeField, Min(1f)] private float _nearNebulaDriftSeconds = 24f;

            public ChapterPalette()
            {
            }

            public ChapterPalette(
                string name,
                Color gradientTint,
                Color farNebulaTint,
                Color nearNebulaTint,
                Color farStarTint,
                Color nearStarTint)
            {
                _name = name;
                _gradientTint = gradientTint;
                _farNebulaTint = farNebulaTint;
                _nearNebulaTint = nearNebulaTint;
                _farStarTint = farStarTint;
                _nearStarTint = nearStarTint;
            }

            public string Name => _name;
            public Color GradientTint => _gradientTint;
            public Color FarNebulaTint => _farNebulaTint;
            public Color NearNebulaTint => _nearNebulaTint;
            public Color FarStarTint => _farStarTint;
            public Color NearStarTint => _nearStarTint;
            public float FarStarEmissionRate => _farStarEmissionRate;
            public float NearStarEmissionRate => _nearStarEmissionRate;
            public float FarStarSimulationSpeed => _farStarSimulationSpeed;
            public float NearStarSimulationSpeed => _nearStarSimulationSpeed;
            public Vector2 FarNebulaDriftDirection => _farNebulaDriftDirection;
            public float FarNebulaDriftDistance => _farNebulaDriftDistance;
            public float FarNebulaDriftSeconds => _farNebulaDriftSeconds;
            public Vector2 NearNebulaDriftDirection => _nearNebulaDriftDirection;
            public float NearNebulaDriftDistance => _nearNebulaDriftDistance;
            public float NearNebulaDriftSeconds => _nearNebulaDriftSeconds;
        }

        [SerializeField] private ChapterPalette[] _chapterPalettes =
        {
            new ChapterPalette(
                "Chapter 1 - Deep Blue",
                new Color(0.035f, 0.055f, 0.145f, 1f),
                new Color(0.10f, 0.20f, 0.40f, 0.22f),
                new Color(0.20f, 0.46f, 0.82f, 0.15f),
                new Color(0.70f, 0.86f, 1f, 0.50f),
                new Color(1f, 1f, 1f, 0.80f)),
            new ChapterPalette(
                "Chapter 2 - Violet Rift",
                new Color(0.055f, 0.035f, 0.13f, 1f),
                new Color(0.22f, 0.10f, 0.38f, 0.21f),
                new Color(0.48f, 0.22f, 0.78f, 0.14f),
                new Color(0.86f, 0.74f, 1f, 0.48f),
                new Color(1f, 0.95f, 1f, 0.78f)),
            new ChapterPalette(
                "Chapter 3 - Green Sector",
                new Color(0.025f, 0.07f, 0.075f, 1f),
                new Color(0.08f, 0.26f, 0.20f, 0.20f),
                new Color(0.20f, 0.62f, 0.48f, 0.13f),
                new Color(0.72f, 1f, 0.90f, 0.46f),
                new Color(0.92f, 1f, 0.96f, 0.76f)),
            new ChapterPalette(
                "Chapter 4 - Red Alert",
                new Color(0.09f, 0.035f, 0.045f, 1f),
                new Color(0.32f, 0.09f, 0.12f, 0.19f),
                new Color(0.82f, 0.20f, 0.18f, 0.12f),
                new Color(1f, 0.76f, 0.68f, 0.44f),
                new Color(1f, 0.95f, 0.88f, 0.74f))
        };

        /// <summary>
        /// Returns the background palette for the provided absolute level number.
        /// </summary>
        public ChapterPalette GetPaletteForLevel(int levelNumber)
        {
            if (_chapterPalettes == null || _chapterPalettes.Length == 0)
            {
                return null;
            }

            int chapterIndex = GetChapterIndexForLevel(levelNumber);
            int paletteIndex = Mathf.Clamp(chapterIndex, 0, _chapterPalettes.Length - 1);
            return _chapterPalettes[paletteIndex];
        }

        /// <summary>
        /// Returns the zero-based campaign chapter index for an absolute level number.
        /// </summary>
        public static int GetChapterIndexForLevel(int levelNumber)
        {
            int campaignLevel = CycleScalingData.GetCampaignLevelForCycle(levelNumber);
            return Mathf.Max(0, (campaignLevel - 1) / LevelsPerChapter);
        }

        private void OnValidate()
        {
            if (_chapterPalettes == null || _chapterPalettes.Length == 0)
            {
                _chapterPalettes = new[] { new ChapterPalette() };
            }
        }
    }
}
