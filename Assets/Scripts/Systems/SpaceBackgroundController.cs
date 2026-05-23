using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;

namespace Warblade.Systems
{
    /// <summary>
    /// Applies chapter and cycle visuals to the gameplay background without coupling it to level flow.
    /// </summary>
    public class SpaceBackgroundController : MonoBehaviour
    {
        private const float TwoPi = Mathf.PI * 2f;

        [Header("Data")]
        [SerializeField] private BackgroundPaletteData _paletteData;
        [SerializeField] private CycleScalingData _cycleScalingData;
        [SerializeField] private IntEventChannel _levelStarted;
        [SerializeField, Min(1)] private int _previewLevel = 1;
        [SerializeField] private bool _applyPreviewOnEnable = true;

        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _gradientRenderer;
        [SerializeField] private SpriteRenderer _farNebulaRenderer;
        [SerializeField] private SpriteRenderer _nearNebulaRenderer;

        [Header("Star Layers")]
        [SerializeField] private ParticleSystem _farStars;
        [SerializeField] private ParticleSystem _nearStars;

        [Header("Sorting")]
        [SerializeField] private bool _applySorting = true;
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _gradientSortingOrder = -1000;
        [SerializeField] private int _farNebulaSortingOrder = -990;
        [SerializeField] private int _farStarsSortingOrder = -980;
        [SerializeField] private int _nearNebulaSortingOrder = -970;
        [SerializeField] private int _nearStarsSortingOrder = -960;

        [Header("Cycle Tint")]
        [SerializeField, Range(0f, 1f)] private float _gradientCycleTintResponse = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _nebulaCycleTintResponse = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _starCycleTintResponse = 0.35f;

        private Vector3 _farNebulaInitialLocalPosition;
        private Vector3 _nearNebulaInitialLocalPosition;
        private BackgroundPaletteData.ChapterPalette _currentPalette;

        private void Awake()
        {
            CaptureInitialPositions();
        }

        private void OnEnable()
        {
            if (_levelStarted != null)
            {
                _levelStarted.OnEventRaised += ApplyLevel;
            }

            if (_applyPreviewOnEnable)
            {
                ApplyLevel(_previewLevel);
            }
        }

        private void OnDisable()
        {
            if (_levelStarted != null)
            {
                _levelStarted.OnEventRaised -= ApplyLevel;
            }
        }

        private void Update()
        {
            if (_currentPalette == null)
            {
                return;
            }

            ApplyDrift(_farNebulaRenderer, _farNebulaInitialLocalPosition, _currentPalette.FarNebulaDriftDirection, _currentPalette.FarNebulaDriftDistance, _currentPalette.FarNebulaDriftSeconds);
            ApplyDrift(_nearNebulaRenderer, _nearNebulaInitialLocalPosition, _currentPalette.NearNebulaDriftDirection, _currentPalette.NearNebulaDriftDistance, _currentPalette.NearNebulaDriftSeconds);
        }

        private void OnValidate()
        {
            _previewLevel = Mathf.Max(1, _previewLevel);
        }

        /// <summary>
        /// Applies the configured background palette for the provided absolute level number.
        /// </summary>
        public void ApplyLevel(int levelNumber)
        {
            if (_paletteData == null)
            {
                return;
            }

            _currentPalette = _paletteData.GetPaletteForLevel(levelNumber);
            if (_currentPalette == null)
            {
                return;
            }

            CycleScalingState cycleScaling = _cycleScalingData == null
                ? CycleScalingData.ResolveDefault(levelNumber)
                : _cycleScalingData.Resolve(levelNumber);

            ApplySpriteColor(_gradientRenderer, ApplyCycleTint(_currentPalette.GradientTint, cycleScaling, _gradientCycleTintResponse));
            ApplySpriteColor(_farNebulaRenderer, ApplyCycleTint(_currentPalette.FarNebulaTint, cycleScaling, _nebulaCycleTintResponse));
            ApplySpriteColor(_nearNebulaRenderer, ApplyCycleTint(_currentPalette.NearNebulaTint, cycleScaling, _nebulaCycleTintResponse));
            ApplyParticleColor(_farStars, ApplyCycleTint(_currentPalette.FarStarTint, cycleScaling, _starCycleTintResponse), _currentPalette.FarStarEmissionRate, _currentPalette.FarStarSimulationSpeed);
            ApplyParticleColor(_nearStars, ApplyCycleTint(_currentPalette.NearStarTint, cycleScaling, _starCycleTintResponse), _currentPalette.NearStarEmissionRate, _currentPalette.NearStarSimulationSpeed);

            if (_applySorting)
            {
                ApplySorting();
            }
        }

        [ContextMenu("Apply Preview Level")]
        private void ApplyPreviewLevel()
        {
            CaptureInitialPositions();
            ApplyLevel(_previewLevel);
        }

        private void CaptureInitialPositions()
        {
            if (_farNebulaRenderer != null)
            {
                _farNebulaInitialLocalPosition = _farNebulaRenderer.transform.localPosition;
            }

            if (_nearNebulaRenderer != null)
            {
                _nearNebulaInitialLocalPosition = _nearNebulaRenderer.transform.localPosition;
            }
        }

        private void ApplySorting()
        {
            ApplyRendererSorting(_gradientRenderer, _gradientSortingOrder);
            ApplyRendererSorting(_farNebulaRenderer, _farNebulaSortingOrder);
            ApplyParticleSorting(_farStars, _farStarsSortingOrder);
            ApplyRendererSorting(_nearNebulaRenderer, _nearNebulaSortingOrder);
            ApplyParticleSorting(_nearStars, _nearStarsSortingOrder);
        }

        private void ApplyDrift(SpriteRenderer renderer, Vector3 initialLocalPosition, Vector2 direction, float distance, float seconds)
        {
            if (renderer == null || distance <= 0f || seconds <= 0f)
            {
                return;
            }

            Vector2 normalizedDirection = direction.sqrMagnitude <= Mathf.Epsilon ? Vector2.right : direction.normalized;
            float phase = Mathf.Sin(Time.unscaledTime / seconds * TwoPi);
            Vector2 offset = normalizedDirection * distance * phase;
            renderer.transform.localPosition = initialLocalPosition + new Vector3(offset.x, offset.y, 0f);
        }

        private void ApplySpriteColor(SpriteRenderer renderer, Color color)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        private void ApplyParticleColor(ParticleSystem particleSystem, Color color, float emissionRate, float simulationSpeed)
        {
            if (particleSystem == null)
            {
                return;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = color;
            main.simulationSpeed = simulationSpeed;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = Mathf.Max(0f, emissionRate);
        }

        private void ApplyRendererSorting(Renderer targetRenderer, int sortingOrder)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.sortingLayerName = _sortingLayerName;
            targetRenderer.sortingOrder = sortingOrder;
        }

        private void ApplyParticleSorting(ParticleSystem particleSystem, int sortingOrder)
        {
            if (particleSystem == null)
            {
                return;
            }

            ApplyRendererSorting(particleSystem.GetComponent<ParticleSystemRenderer>(), sortingOrder);
        }

        private static Color ApplyCycleTint(Color color, CycleScalingState cycleScaling, float response)
        {
            float originalAlpha = color.a;
            Color tinted = Color.Lerp(color, cycleScaling.TintColor, Mathf.Clamp01(cycleScaling.TintStrength * response));
            tinted.a = originalAlpha;
            return tinted;
        }
    }
}
