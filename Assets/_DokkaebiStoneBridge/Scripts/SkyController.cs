using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace DokkaebiStoneBridge
{
    public class SkyController : MonoBehaviour
    {
        public Color nightBackgroundColor = new Color(0.03f, 0.04f, 0.12f);
        public Color dayBackgroundColor = new Color(0.55f, 0.75f, 0.95f);

        public Light sunLight;
        public Color nightSunColor = new Color(0.05f, 0.05f, 0.1f);
        public Color daySunColor = new Color(1f, 0.95f, 0.85f);
        public float nightSunIntensity = 0.05f;
        public float daySunIntensity = 1.2f;
        public Vector3 nightSunEuler = new Vector3(70f, -30f, 0f);
        public Vector3 daySunEuler = new Vector3(45f, -30f, 0f);

        public Transform nightSkyGroup;
        public Transform daySkyGroup;

        public float transitionDuration = 4f;

        public Color starColor = new Color(1f, 0.95f, 0.82f);
        public float starMinSize = 14f;
        public float starMaxSize = 46f;
        public int starFieldSeed = 777;

        [Header("배경 별 장식 (연출용, 스테이지 진행도와 무관)")]
        public int decorativeStarCount = 40;
        public float decorativeStarMinSize = 2f;
        public float decorativeStarMaxSize = 6f;
        public Color moonColor = new Color(0.95f, 0.95f, 0.85f);

        public float shakeDuration = 0.5f;
        public float shakeMagnitude = 0.5f;
        public float starFallDuration = 1.1f;

        public bool enableTestKey = true;

        private Camera _cam;
        private bool _isDay;
        private bool _fullyDay;
        private bool _isAnimating;
        private float _currentBrightFrac;

        private Canvas _starCanvas;
        private readonly List<RectTransform> _starRects = new List<RectTransform>();
        private readonly List<Image> _starImages = new List<Image>();
        private float[] _starPhase;
        private int _fallingIndex = -1;

        private readonly List<RectTransform> _decoRects = new List<RectTransform>();
        private readonly List<Image> _decoImages = new List<Image>();
        private float[] _decoPhase;
        private Image _moonImage;

        private void Awake()
        {
            _cam = Camera.main;
            BuildStarField();
            BuildDecorativeSky();
            ApplyNightImmediate();
        }

        private void Start()
        {
            int cleared = CountClearedStages();
            ApplyProgressFromSave(cleared);
        }

        private void Update()
        {
            if (enableTestKey && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                TriggerDawn();
            }

            for (int i = 0; i < _starImages.Count; i++)
            {
                if (i == _fallingIndex) continue;
                var img = _starImages[i];
                if (img == null || !img.gameObject.activeSelf) continue;
                float pulse = 0.82f + 0.18f * Mathf.Sin(Time.time * 1.4f + _starPhase[i]);
                var c = img.color;
                c.a = pulse;
                img.color = c;
            }

            for (int i = 0; i < _decoImages.Count; i++)
            {
                var img = _decoImages[i];
                if (img == null) continue;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.1f + _decoPhase[i]);
                var c = img.color;
                c.a = Mathf.Lerp(0.25f, 0.85f, pulse);
                img.color = c;
            }
        }

        private static int CountClearedStages()
        {
            int n = 0;
            for (int i = 1; i <= StageProgress.StageCount; i++)
            {
                if (StageProgress.IsCleared(i)) n++;
            }
            return n;
        }

        private void BuildStarField()
        {
            var canvasObj = new GameObject("StarFieldCanvas");
            canvasObj.transform.SetParent(null);
            _starCanvas = canvasObj.AddComponent<Canvas>();
            _starCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _starCanvas.sortingOrder = -50;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            int n = StageProgress.StageCount;
            var rng = new System.Random(starFieldSeed);
            _starPhase = new float[n];

            for (int i = 0; i < n; i++)
            {
                var starObj = new GameObject("Star_" + i);
                starObj.transform.SetParent(canvasObj.transform, false);

                var img = starObj.AddComponent<Image>();
                img.sprite = UIStyleKit.Circle;
                img.raycastTarget = false;
                float brightness = (float)rng.NextDouble() * 0.25f + 0.75f;
                var c = starColor * brightness;
                c.a = 1f;
                img.color = c;

                float size = Mathf.Lerp(starMinSize, starMaxSize, n <= 1 ? 1f : i / (float)(n - 1));
                var rect = starObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(size, size);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                float nx = (float)rng.NextDouble();
                float ny = (float)rng.NextDouble();
                float x = Mathf.Lerp(-860f, 860f, nx);
                float y = Mathf.Lerp(80f, 420f, ny);
                rect.anchoredPosition = new Vector2(x, -y);

                _starRects.Add(rect);
                _starImages.Add(img);
                _starPhase[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
            }
        }

        private void BuildDecorativeSky()
        {
            if (_starCanvas == null) return;

            var rng = new System.Random(starFieldSeed + 1);
            _decoPhase = new float[decorativeStarCount];

            for (int i = 0; i < decorativeStarCount; i++)
            {
                var starObj = new GameObject("DecoStar_" + i);
                starObj.transform.SetParent(_starCanvas.transform, false);

                var img = starObj.AddComponent<Image>();
                img.sprite = UIStyleKit.Circle;
                img.raycastTarget = false;
                float brightness = (float)rng.NextDouble() * 0.3f + 0.6f;
                var c = Color.white * brightness;
                c.a = 0.6f;
                img.color = c;

                float size = Mathf.Lerp(decorativeStarMinSize, decorativeStarMaxSize, (float)rng.NextDouble());
                var rect = starObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(size, size);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                float x = Mathf.Lerp(-960f, 960f, (float)rng.NextDouble());
                float y = Mathf.Lerp(0f, 540f, (float)rng.NextDouble());
                rect.anchoredPosition = new Vector2(x, -y);

                _decoRects.Add(rect);
                _decoImages.Add(img);
                _decoPhase[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
            }

            var moonObj = new GameObject("Moon");
            moonObj.transform.SetParent(_starCanvas.transform, false);

            var glowObj = new GameObject("MoonGlow");
            glowObj.transform.SetParent(moonObj.transform, false);
            var glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = UIStyleKit.Circle;
            glowImg.raycastTarget = false;
            glowImg.color = new Color(moonColor.r, moonColor.g, moonColor.b, 0.18f);
            var glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(220f, 220f);
            glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;

            _moonImage = moonObj.AddComponent<Image>();
            _moonImage.sprite = UIStyleKit.Circle;
            _moonImage.raycastTarget = false;
            _moonImage.color = moonColor;
            var moonRect = moonObj.GetComponent<RectTransform>();
            moonRect.anchorMin = moonRect.anchorMax = new Vector2(1f, 1f);
            moonRect.pivot = new Vector2(1f, 1f);
            moonRect.sizeDelta = new Vector2(90f, 90f);
            moonRect.anchoredPosition = new Vector2(-140f, -120f);
        }

        private void ApplySkyColors(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            Color bg = Color.Lerp(nightBackgroundColor, dayBackgroundColor, fraction);
            if (_cam != null) _cam.backgroundColor = bg;
            if (sunLight != null)
            {
                sunLight.color = Color.Lerp(nightSunColor, daySunColor, fraction);
                sunLight.intensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, fraction);
                sunLight.transform.rotation = Quaternion.Slerp(Quaternion.Euler(nightSunEuler), Quaternion.Euler(daySunEuler), fraction);
            }
            RenderSettings.fogColor = bg;
            RenderSettings.ambientLight = bg;
            _currentBrightFrac = fraction;
        }

        public void ApplyNightImmediate()
        {
            if (_cam != null) _cam.clearFlags = CameraClearFlags.SolidColor;
            ApplySkyColors(0f);
            if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(true);
            if (daySkyGroup != null) daySkyGroup.gameObject.SetActive(false);

            if (_starCanvas != null) _starCanvas.gameObject.SetActive(true);
            for (int i = 0; i < _starRects.Count; i++)
            {
                if (_starRects[i] == null) continue;
                _starRects[i].gameObject.SetActive(true);
                _starRects[i].localScale = Vector3.one;
                var c = _starImages[i].color;
                c.a = 1f;
                _starImages[i].color = c;
            }

            _isDay = false;
            _fullyDay = false;
        }

        private void ApplyProgressFromSave(int clearedCount)
        {
            clearedCount = Mathf.Clamp(clearedCount, 0, StageProgress.StageCount);
            if (clearedCount <= 0) return;

            if (clearedCount >= StageProgress.StageCount)
            {
                ApplyFullDayImmediate();
                return;
            }

            float fraction = clearedCount / (float)StageProgress.StageCount;
            ApplySkyColors(fraction);
            for (int i = 0; i < clearedCount && i < _starRects.Count; i++)
            {
                _starRects[i].gameObject.SetActive(false);
            }
        }

        private void ApplyFullDayImmediate()
        {
            ApplySkyColors(1f);
            if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(false);
            if (daySkyGroup != null) daySkyGroup.gameObject.SetActive(true);
            if (_starCanvas != null) _starCanvas.gameObject.SetActive(false);
            _isDay = true;
            _fullyDay = true;
        }

        public void TriggerDawn()
        {
            if (_fullyDay || _isAnimating) return;
            StartCoroutine(ClearCelebrationRoutine());
        }

        private IEnumerator ClearCelebrationRoutine()
        {
            _isAnimating = true;

            int clearedCount = Mathf.Clamp(CountClearedStages(), 1, StageProgress.StageCount);
            int fallIndex = Mathf.Clamp(clearedCount - 1, 0, Mathf.Max(0, _starRects.Count - 1));
            bool isFinal = clearedCount >= StageProgress.StageCount;
            float targetFraction = clearedCount / (float)StageProgress.StageCount;

            var rig = FindAnyObjectByType<IsoCameraRig>();
            if (rig != null) StartCoroutine(ShakeCamera(rig, Mathf.Min(shakeDuration, transitionDuration), shakeMagnitude));

            StartCoroutine(DropStar(fallIndex, Mathf.Min(starFallDuration, transitionDuration)));

            if (isFinal && daySkyGroup != null) daySkyGroup.gameObject.SetActive(true);

            float from = _currentBrightFrac;
            float t = 0f;
            bool nightHidden = false;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / transitionDuration);
                float eased = p * p * (3f - 2f * p);
                ApplySkyColors(Mathf.Lerp(from, targetFraction, eased));

                if (isFinal && !nightHidden && p > 0.6f && nightSkyGroup != null)
                {
                    nightSkyGroup.gameObject.SetActive(false);
                    nightHidden = true;
                }
                yield return null;
            }
            ApplySkyColors(targetFraction);

            if (isFinal)
            {
                if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(false);
                if (_starCanvas != null) _starCanvas.gameObject.SetActive(false);
                _isDay = true;
                _fullyDay = true;
                Debug.Log("[SkyController] final star fell, sky fully cleared.");
            }
            else
            {
                Debug.Log("[SkyController] stage cleared, sky brightened: " + clearedCount + "/" + StageProgress.StageCount);
            }

            _isAnimating = false;
        }

        private IEnumerator ShakeCamera(IsoCameraRig rig, float duration, float magnitude)
        {
            float t = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (t < duration)
            {
                t += Time.deltaTime;
                float damper = 1f - Mathf.Clamp01(t / duration);
                rig.shakeOffset = UnityEngine.Random.insideUnitSphere * magnitude * damper;
                yield return null;
            }
            rig.shakeOffset = Vector3.zero;
        }

        private IEnumerator DropStar(int index, float duration)
        {
            if (index < 0 || index >= _starRects.Count) yield break;
            var rect = _starRects[index];
            var img = _starImages[index];
            if (rect == null || img == null || !rect.gameObject.activeSelf) yield break;

            _fallingIndex = index;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, -900f);
            Vector3 startScale = rect.localScale;
            Color startColor = img.color;

            float t = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                rect.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, p);
                img.color = c;
                yield return null;
            }

            rect.gameObject.SetActive(false);
            rect.anchoredPosition = startPos;
            rect.localScale = startScale;
            img.color = startColor;
            _fallingIndex = -1;
        }
    }
}
