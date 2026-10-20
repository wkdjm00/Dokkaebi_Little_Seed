namespace DokkaebiStoneBridge
{
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkyController : MonoBehaviour
{
    [Header("배경 색상")]
    public Color nightBackgroundColor = new Color(0.03f, 0.04f, 0.12f);
    public Color dayBackgroundColor = new Color(0.55f, 0.75f, 0.95f);

    [Header("태양광")]
    public Light sunLight;
    public Color nightSunColor = new Color(0.05f, 0.05f, 0.1f);
    public Color daySunColor = new Color(1f, 0.95f, 0.85f);
    public float nightSunIntensity = 0.05f;
    public float daySunIntensity = 1.2f;
    public Vector3 nightSunEuler = new Vector3(70f, -30f, 0f);
    public Vector3 daySunEuler = new Vector3(45f, -30f, 0f);

    [Header("하늘 그룹 (카메라에 자동으로 붙음)")]
    public Transform nightSkyGroup;
    public Transform daySkyGroup;

    [Header("전환 속도")]
    public float transitionDuration = 1.6f;

    [Header("별 3D 모델 (진행도 별 - 스테이지 수만큼 생성)")]
    public Color starColor = new Color(1f, 0.95f, 0.82f);
    public GameObject[] starPrefabs;
    public float starTargetSize = 0.5f;
    public float starEmissionIntensity = 2.2f;
    public int starFieldSeed = 777;

    [Header("배경 장식 별 (연출용)")]
    public int decorativeStarCount = 18;
    public float decorativeStarTargetSize = 0.22f;
    public float decorativeStarEmissionIntensity = 1.1f;

    [Header("클리어 연출")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.5f;
    public float starFallDuration = 1.2f;

    public bool enableTestKey = true;

    private Camera _cam;
    private bool _isDay;
    private bool _fullyDay;
    private bool _isAnimating;
    private float _currentBrightFrac;

    private readonly List<Transform> _progressStars = new List<Transform>();
    private readonly List<Vector3> _progressHomeLocalPos = new List<Vector3>();
    private readonly List<Vector3> _progressHomeLocalScale = new List<Vector3>();
    private readonly List<Transform> _decoStars = new List<Transform>();
    private readonly List<float> _decoSpinSpeed = new List<float>();

    private void Awake()
    {
        _cam = Camera.main;
        AttachSkyGroupsToCamera();
        BuildStarField3D();
        ApplyNightImmediate();
    }

    private void Start()
    {
        int cleared = CountClearedStages();
        ApplyProgressFromSave(cleared);
    }

    private void Update()
    {
        if (enableTestKey && Input.GetKeyDown(KeyCode.T))
        {
            TriggerDawn();
        }

        for (int i = 0; i < _decoStars.Count; i++)
        {
            Transform t = _decoStars[i];
            if (t == null) continue;
            t.Rotate(Vector3.up, _decoSpinSpeed[i] * Time.deltaTime, Space.Self);
        }
    }

    private static int CountClearedStages()
    {
        int count = 0;
        for (int i = 1; i <= StageProgress.StageCount; i++)
        {
            if (StageProgress.IsCleared(i)) count++;
        }
        return count;
    }

    // 하늘 그룹을 카메라의 자식으로 붙여서, 플레이어가 어느 스테이지(월드 X좌표가
    // 전혀 다른 곳)에 있어도 별이 항상 화면 상단 하늘 영역에 고정되어 보이도록 한다.
    // 월드 고정 좌표에 그대로 두면 1스테이지 근처에서만 보이고 나머지 스테이지에서는
    // 카메라에서 멀리 벗어나 전혀 보이지 않게 된다.
    private void AttachSkyGroupsToCamera()
    {
        if (_cam == null) return;

        if (nightSkyGroup != null)
        {
            nightSkyGroup.SetParent(_cam.transform, false);
            nightSkyGroup.localPosition = Vector3.zero;
            nightSkyGroup.localRotation = Quaternion.identity;
        }
        if (daySkyGroup != null)
        {
            daySkyGroup.SetParent(_cam.transform, false);
            daySkyGroup.localPosition = Vector3.zero;
            daySkyGroup.localRotation = Quaternion.identity;
        }
    }

    private void BuildStarField3D()
    {
        if (nightSkyGroup == null)
        {
            Debug.LogWarning("[SkyController] nightSkyGroup가 비어 있어 별을 배치할 수 없습니다.");
            return;
        }
        if (starPrefabs == null || starPrefabs.Length == 0)
        {
            Debug.LogWarning("[SkyController] starPrefabs가 비어 있어 별 모델을 배치할 수 없습니다.");
            return;
        }

        System.Random rng = new System.Random(starFieldSeed);

        float halfH = (_cam != null && _cam.orthographic) ? _cam.orthographicSize : 3f;
        float halfW = halfH * (_cam != null ? _cam.aspect : 1.78f);

        int n = StageProgress.StageCount;
        for (int i = 0; i < n; i++)
        {
            GameObject prefab = starPrefabs[rng.Next(starPrefabs.Length)];
            GameObject inst = Instantiate(prefab, nightSkyGroup);
            inst.name = "ProgressStar_" + i;

            float nx = (n <= 1) ? 0.5f : (i / (float)(n - 1));
            float x = Mathf.Lerp(-halfW * 0.82f, halfW * 0.82f, nx);
            float y = halfH * Mathf.Lerp(0.55f, 0.92f, (float)rng.NextDouble());
            float z = 20f + (float)rng.NextDouble() * 6f;
            inst.transform.localPosition = new Vector3(x, y, z);
            inst.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            float sizeJitter = 0.85f + (float)rng.NextDouble() * 0.3f;
            ScaleToTarget(inst.transform, starTargetSize * sizeJitter);
            ApplyGlow(inst, starColor, starEmissionIntensity);

            _progressStars.Add(inst.transform);
            _progressHomeLocalPos.Add(inst.transform.localPosition);
            _progressHomeLocalScale.Add(inst.transform.localScale);
        }

        int decoCount = Mathf.Max(0, decorativeStarCount);
        Color decoColor = Color.Lerp(starColor, Color.white, 0.3f);
        for (int i = 0; i < decoCount; i++)
        {
            GameObject prefab = starPrefabs[rng.Next(starPrefabs.Length)];
            GameObject inst = Instantiate(prefab, nightSkyGroup);
            inst.name = "DecoStar_" + i;

            float x = Mathf.Lerp(-halfW * 0.95f, halfW * 0.95f, (float)rng.NextDouble());
            float y = halfH * Mathf.Lerp(0.35f, 0.95f, (float)rng.NextDouble());
            float z = 20f + (float)rng.NextDouble() * 10f;
            inst.transform.localPosition = new Vector3(x, y, z);
            inst.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 360f);

            float sizeJitter = 0.6f + (float)rng.NextDouble() * 0.8f;
            ScaleToTarget(inst.transform, decorativeStarTargetSize * sizeJitter);
            ApplyGlow(inst, decoColor, decorativeStarEmissionIntensity);

            _decoStars.Add(inst.transform);
            _decoSpinSpeed.Add(8f + (float)rng.NextDouble() * 20f);
        }
    }

    private static void ScaleToTarget(Transform t, float targetSize)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z, 0.0001f);
        float scale = targetSize / maxDim;
        // 일부 별 모델(FBX)은 매우 작은 단위로 모델링되어 있어, 임포트된 루트 트랜스폼에
        // 이미 100배 같은 보정 스케일이 걸려 있는 경우가 있다. 여기서 t.localScale을
        // 절대값으로 덮어쓰면 그 보정값이 사라져 별이 거의 안 보이는 크기로 줄어드는
        // 버그가 있었다 — 기존 스케일에 상대적으로 곱해서 보정값을 유지한다.
        t.localScale = Vector3.Scale(t.localScale, new Vector3(scale, scale, scale));
    }

    // URP/Lit 머티리얼은 씬 조명(야간 directional light, ambient)의 영향을 그대로 받는데,
    // 야간에는 조명이 거의 어두워서(의도된 연출) 별 모델이 검은 실루엣으로 보일 수 있다.
    // 개별 인스턴스 머티리얼에 발광(Emission)을 켜서, 주변 조명과 무관하게 별이 항상
    // 또렷하게 빛나 보이도록 한다. (공유 에셋 Star.mat 자체는 건드리지 않음 — r.material은
    // 자동으로 복사본을 만들어 인스턴스화한다.)
    private static void ApplyGlow(GameObject go, Color color, float intensity)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material mat = renderers[i].material;
            if (!mat.HasProperty("_EmissionColor")) continue;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * intensity);
        }
    }

    private void ApplySkyColors(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        _currentBrightFrac = fraction;

        if (_cam != null)
        {
            _cam.backgroundColor = Color.Lerp(nightBackgroundColor, dayBackgroundColor, fraction);
        }
        RenderSettings.ambientLight = Color.Lerp(nightBackgroundColor, dayBackgroundColor, fraction);
        RenderSettings.fogColor = Color.Lerp(nightBackgroundColor, dayBackgroundColor, fraction);

        if (sunLight != null)
        {
            sunLight.color = Color.Lerp(nightSunColor, daySunColor, fraction);
            sunLight.intensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, fraction);
            Vector3 euler = Vector3.Lerp(nightSunEuler, daySunEuler, fraction);
            sunLight.transform.rotation = Quaternion.Euler(euler);
        }
    }

    public void ApplyNightImmediate()
    {
        if (_cam != null) _cam.clearFlags = CameraClearFlags.SolidColor;
        ApplySkyColors(0f);

        if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(true);
        if (daySkyGroup != null) daySkyGroup.gameObject.SetActive(false);

        for (int i = 0; i < _progressStars.Count; i++)
        {
            if (_progressStars[i] == null) continue;
            _progressStars[i].gameObject.SetActive(true);
            _progressStars[i].localPosition = _progressHomeLocalPos[i];
            _progressStars[i].localScale = _progressHomeLocalScale[i];
        }
        for (int i = 0; i < _decoStars.Count; i++)
        {
            if (_decoStars[i] != null) _decoStars[i].gameObject.SetActive(true);
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
        for (int i = 0; i < clearedCount && i < _progressStars.Count; i++)
        {
            if (_progressStars[i] != null) _progressStars[i].gameObject.SetActive(false);
        }
    }

    private void ApplyFullDayImmediate()
    {
        ApplySkyColors(1f);
        if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(false);
        if (daySkyGroup != null) daySkyGroup.gameObject.SetActive(true);
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
        int fallIndex = Mathf.Clamp(clearedCount - 1, 0, Mathf.Max(0, _progressStars.Count - 1));
        bool isFinal = clearedCount >= StageProgress.StageCount;
        float targetFraction = clearedCount / (float)StageProgress.StageCount;

        IsoCameraRig rig = FindAnyObjectByType<IsoCameraRig>();
        if (rig != null)
        {
            StartCoroutine(ShakeCamera(rig, Mathf.Min(shakeDuration, transitionDuration), shakeMagnitude));
        }

        StartCoroutine(DropStar3D(fallIndex, Mathf.Min(starFallDuration, transitionDuration)));

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
            if (isFinal && daySkyGroup != null && !daySkyGroup.gameObject.activeSelf)
            {
                daySkyGroup.gameObject.SetActive(true);
            }
            yield return null;
        }
        ApplySkyColors(targetFraction);

        if (isFinal)
        {
            if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(false);
            _isDay = true;
            _fullyDay = true;
            Debug.Log("[SkyController] 마지막 별까지 떨어지고 하늘이 완전히 밝아졌습니다.");
        }
        else
        {
            Debug.Log("[SkyController] 스테이지 클리어, 하늘 밝기 갱신: " + clearedCount + "/" + StageProgress.StageCount);
        }

        _isAnimating = false;
    }

    private IEnumerator ShakeCamera(IsoCameraRig rig, float duration, float magnitude)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damper = 1f - (t / duration);
            float ox = (Random.value * 2f - 1f) * magnitude * damper;
            float oy = (Random.value * 2f - 1f) * magnitude * damper;
            rig.shakeOffset = new Vector3(ox, oy, 0f);
            rig.Apply();
            yield return null;
        }
        rig.shakeOffset = Vector3.zero;
        rig.Apply();
    }

    // 클리어한 스테이지에 해당하는 별이 잠깐 흔들리다가(jitter) 아래로 떨어지며 사라지는 연출.
    private IEnumerator DropStar3D(int index, float duration)
    {
        if (index < 0 || index >= _progressStars.Count) yield break;
        Transform t = _progressStars[index];
        if (t == null || !t.gameObject.activeSelf) yield break;

        Vector3 startPos = t.localPosition;
        Vector3 startScale = t.localScale;

        duration = Mathf.Max(0.1f, duration);
        float jitterDuration = Mathf.Min(0.35f, duration * 0.35f);
        float fallDuration = duration - jitterDuration;

        System.Random rng = new System.Random(index * 97 + 13);

        float jt = 0f;
        while (jt < jitterDuration)
        {
            jt += Time.deltaTime;
            float shake = 0.06f * (1f - jt / jitterDuration);
            float ox = ((float)rng.NextDouble() * 2f - 1f) * shake;
            float oy = ((float)rng.NextDouble() * 2f - 1f) * shake;
            t.localPosition = startPos + new Vector3(ox, oy, 0f);
            yield return null;
        }
        t.localPosition = startPos;

        Vector3 endPos = startPos + new Vector3(0f, -5.5f, 0f);
        float ft = 0f;
        while (ft < fallDuration)
        {
            ft += Time.deltaTime;
            float p = Mathf.Clamp01(ft / fallDuration);
            float eased = p * p;
            t.localPosition = Vector3.Lerp(startPos, endPos, eased);
            t.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            yield return null;
        }

        t.gameObject.SetActive(false);
        t.localPosition = startPos;
        t.localScale = startScale;
    }
}
}
