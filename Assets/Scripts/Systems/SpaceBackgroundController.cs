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

        [Header("Warp Transition")]
        [SerializeField, Min(1f)] private float _farStarWarpEmissionMultiplier = 3f;
        [SerializeField, Min(1f)] private float _nearStarWarpEmissionMultiplier = 4f;
        [SerializeField, Min(1f)] private float _farStarWarpSimulationSpeedMultiplier = 3f;
        [SerializeField, Min(1f)] private float _nearStarWarpSimulationSpeedMultiplier = 4.5f;
        [SerializeField, Min(0f)] private float _warpLengthScale = 3f;
        [SerializeField, Min(0f)] private float _warpVelocityScale = 0.65f;
        [SerializeField] private bool _stretchStarsDuringWarp = true;

        private Vector3 _farNebulaInitialLocalPosition;
        private Vector3 _nearNebulaInitialLocalPosition;
        private BackgroundPaletteData.ChapterPalette _currentPalette;
        private int _currentLevelNumber = 1;
        private float _warpIntensity;
        private ParticleSystemRenderMode _farStarsBaseRenderMode;
        private ParticleSystemRenderMode _nearStarsBaseRenderMode;
        private float _farStarsBaseLengthScale;
        private float _nearStarsBaseLengthScale;
        private float _farStarsBaseVelocityScale;
        private float _nearStarsBaseVelocityScale;

        private void Awake()
        {
            CaptureInitialPositions();
            CaptureStarRendererDefaults();
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
            _farStarWarpEmissionMultiplier = Mathf.Max(1f, _farStarWarpEmissionMultiplier);
            _nearStarWarpEmissionMultiplier = Mathf.Max(1f, _nearStarWarpEmissionMultiplier);
            _farStarWarpSimulationSpeedMultiplier = Mathf.Max(1f, _farStarWarpSimulationSpeedMultiplier);
            _nearStarWarpSimulationSpeedMultiplier = Mathf.Max(1f, _nearStarWarpSimulationSpeedMultiplier);
            _warpLengthScale = Mathf.Max(0f, _warpLengthScale);
            _warpVelocityScale = Mathf.Max(0f, _warpVelocityScale);
        }

        /// <summary>
        /// Applies the configured background palette for the provided absolute level number.
        /// </summary>
        public void ApplyLevel(int levelNumber)
        {
            _currentLevelNumber = Mathf.Max(1, levelNumber);

            if (_paletteData == null)
            {
                return;
            }

            _currentPalette = _paletteData.GetPaletteForLevel(_currentLevelNumber);
            if (_currentPalette == null)
            {
                return;
            }

            ApplyCurrentPalette();
        }

        /// <summary>
        /// Temporarily intensifies star speed, density, and stretch during sector travel.
        /// </summary>
        public void SetWarpIntensity(float intensity)
        {
            float clampedIntensity = Mathf.Clamp01(intensity);
            if (Mathf.Approximately(_warpIntensity, clampedIntensity))
            {
                return;
            }

            _warpIntensity = clampedIntensity;
            ApplyCurrentPalette();
        }

        private void ApplyCurrentPalette()
        {
            if (_currentPalette == null)
            {
                return;
            }

            CycleScalingState cycleScaling = _cycleScalingData == null
                ? CycleScalingData.ResolveDefault(_currentLevelNumber)
                : _cycleScalingData.Resolve(_currentLevelNumber);

            ApplySpriteColor(_gradientRenderer, ApplyCycleTint(_currentPalette.GradientTint, cycleScaling, _gradientCycleTintResponse));
            ApplySpriteColor(_farNebulaRenderer, ApplyCycleTint(_currentPalette.FarNebulaTint, cycleScaling, _nebulaCycleTintResponse));
            ApplySpriteColor(_nearNebulaRenderer, ApplyCycleTint(_currentPalette.NearNebulaTint, cycleScaling, _nebulaCycleTintResponse));
            ApplyParticleColor(
                _farStars,
                ApplyCycleTint(_currentPalette.FarStarTint, cycleScaling, _starCycleTintResponse),
                _currentPalette.FarStarEmissionRate,
                _currentPalette.FarStarSimulationSpeed,
                _farStarWarpEmissionMultiplier,
                _farStarWarpSimulationSpeedMultiplier,
                _farStarsBaseRenderMode,
                _farStarsBaseLengthScale,
                _farStarsBaseVelocityScale);
            ApplyParticleColor(
                _nearStars,
                ApplyCycleTint(_currentPalette.NearStarTint, cycleScaling, _starCycleTintResponse),
                _currentPalette.NearStarEmissionRate,
                _currentPalette.NearStarSimulationSpeed,
                _nearStarWarpEmissionMultiplier,
                _nearStarWarpSimulationSpeedMultiplier,
                _nearStarsBaseRenderMode,
                _nearStarsBaseLengthScale,
                _nearStarsBaseVelocityScale);

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

        private void CaptureStarRendererDefaults()
        {
            CaptureStarRendererDefaults(_farStars, out _farStarsBaseRenderMode, out _farStarsBaseLengthScale, out _farStarsBaseVelocityScale);
            CaptureStarRendererDefaults(_nearStars, out _nearStarsBaseRenderMode, out _nearStarsBaseLengthScale, out _nearStarsBaseVelocityScale);
        }

        private void CaptureStarRendererDefaults(
            ParticleSystem particleSystem,
            out ParticleSystemRenderMode renderMode,
            out float lengthScale,
            out float velocityScale)
        {
            ParticleSystemRenderer particleRenderer = particleSystem == null
                ? null
                : particleSystem.GetComponent<ParticleSystemRenderer>();

            if (particleRenderer == null)
            {
                renderMode = ParticleSystemRenderMode.Billboard;
                lengthScale = 1f;
                velocityScale = 0f;
                return;
            }

            renderMode = particleRenderer.renderMode;
            lengthScale = particleRenderer.lengthScale;
            velocityScale = particleRenderer.velocityScale;
        }

        private void ApplyParticleColor(
            ParticleSystem particleSystem,
            Color color,
            float emissionRate,
            float simulationSpeed,
            float warpEmissionMultiplier,
            float warpSimulationSpeedMultiplier,
            ParticleSystemRenderMode baseRenderMode,
            float baseLengthScale,
            float baseVelocityScale)
        {
            if (particleSystem == null)
            {
                return;
            }

            float emissionMultiplier = Mathf.Lerp(1f, warpEmissionMultiplier, _warpIntensity);
            float speedMultiplier = Mathf.Lerp(1f, warpSimulationSpeedMultiplier, _warpIntensity);

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = color;
            main.simulationSpeed = simulationSpeed * speedMultiplier;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = Mathf.Max(0f, emissionRate * emissionMultiplier);

            ApplyStarRendererWarp(particleSystem, baseRenderMode, baseLengthScale, baseVelocityScale);
        }

        private void ApplyStarRendererWarp(
            ParticleSystem particleSystem,
            ParticleSystemRenderMode baseRenderMode,
            float baseLengthScale,
            float baseVelocityScale)
        {
            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null)
            {
                return;
            }

            if (_stretchStarsDuringWarp && _warpIntensity > 0.01f)
            {
                particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            }
            else
            {
                particleRenderer.renderMode = baseRenderMode;
            }

            particleRenderer.lengthScale = Mathf.Lerp(baseLengthScale, _warpLengthScale, _warpIntensity);
            particleRenderer.velocityScale = Mathf.Lerp(baseVelocityScale, _warpVelocityScale, _warpIntensity);
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
