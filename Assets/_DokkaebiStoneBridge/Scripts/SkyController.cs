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

    [Header("별 배치 방식")]
    [Tooltip("켜면 NightSkyGroup 안에 미리 배치해 둔 ProgressStar_N / DecoStar_N 오브젝트를 그대로 사용합니다(씬에서 직접 수정 가능). 하나도 없으면 예전처럼 플레이 시작 시 코드로 생성합니다.")]
    public bool useScenePlacedStars = true;
    [Tooltip("플레이 시작 시 별 머티리얼에 발광(Emission)을 강제로 켤지 여부. 끄면 모델에 적용된 Star.mat 그대로 사용합니다.")]
    public bool forceStarEmission = false;

    [Header("배경 장식 별 (연출용)")]
    public int decorativeStarCount = 18;
    public float decorativeStarTargetSize = 0.22f;
    public float decorativeStarEmissionIntensity = 1.1f;

    [Header("클리어 연출")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.5f;
    public float starFallDuration = 1.2f;

    [Header("마지막 스테이지 클리어: 가장 큰 별이 떨어진 뒤 남은 별도 전부 떨어짐")]
    [Tooltip("남은 별들이 떨어지기 시작하는 시간이 이 범위 안에서 제각각 달라진다(초). 0이면 한꺼번에 떨어진다.")]
    public float finalRestFallStagger = 0.6f;
    [Tooltip("남은 별 하나가 떨어지는 데 걸리는 시간(초)")]
    public float finalRestFallDuration = 1.0f;

    public bool enableTestKey = true;

    private Camera _cam;
    private bool _isDay;
    private bool _fullyDay;
    private bool _isAnimating;
    private float _currentBrightFrac;
    private int _restFalling;
    private bool _finalRestFallFinished = true;

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
        // 테스트용 T키 (새 Input System 방식). 별은 회전시키지 않는다.
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (enableTestKey && kb != null && kb.tKey.wasPressedThisFrame)
        {
            TriggerDawn();
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
            // 에디터에서 이미 카메라의 자식으로 배치해 두었다면, 그 위치/회전(= 별과 카메라 사이 거리 등)을 그대로 유지한다.
            bool nightAlreadyOnCamera = nightSkyGroup.parent == _cam.transform;
            if (!nightAlreadyOnCamera) nightSkyGroup.SetParent(_cam.transform, false);
            if (!nightAlreadyOnCamera) nightSkyGroup.localPosition = Vector3.zero;
            if (!nightAlreadyOnCamera) nightSkyGroup.localRotation = Quaternion.identity;
        }
        if (daySkyGroup != null)
        {
            bool dayAlreadyOnCamera = daySkyGroup.parent == _cam.transform;
            if (!dayAlreadyOnCamera) daySkyGroup.SetParent(_cam.transform, false);
            if (!dayAlreadyOnCamera) daySkyGroup.localPosition = Vector3.zero;
            if (!dayAlreadyOnCamera) daySkyGroup.localRotation = Quaternion.identity;
        }
    }

    private void BuildStarField3D()
    {
        if (nightSkyGroup == null)
        {
            Debug.LogWarning("[SkyController] nightSkyGroup가 비어 있어 별을 배치할 수 없습니다.");
            return;
        }
        if (useScenePlacedStars && CollectScenePlacedStars()) return;

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
            if (forceStarEmission) ApplyGlow(inst, starColor, starEmissionIntensity);

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
            if (forceStarEmission) ApplyGlow(inst, decoColor, decorativeStarEmissionIntensity);

            _decoStars.Add(inst.transform);
            _decoSpinSpeed.Add(8f + (float)rng.NextDouble() * 20f);
        }
    }

    // 씬(NightSkyGroup)에 미리 배치해 둔 별을 수집한다.
    //   ProgressStar_0, ProgressStar_1 ... : 스테이지 클리어 때 흔들리다 떨어지는 별 (번호 = 스테이지 번호 - 1)
    //   DecoStar_N                         : 배경 장식 별 (플레이 중 천천히 회전)
    // 위치/회전/크기/모델은 씬에서 자유롭게 수정해도 그대로 쓰인다. 이름의 번호만 유지하면 된다.
    private bool CollectScenePlacedStars()
    {
        if (nightSkyGroup == null) return false;

        int n = StageProgress.StageCount;
        Transform[] slots = new Transform[n];
        bool found = false;

        foreach (Transform child in nightSkyGroup)
        {
            if (child.name.StartsWith("ProgressStar_"))
            {
                int idx = ParseTrailingIndex(child.name);
                if (idx >= 0 && idx < n && slots[idx] == null)
                {
                    slots[idx] = child;
                    found = true;
                }
            }
            else if (child.name.StartsWith("DecoStar_"))
            {
                _decoStars.Add(child);
                found = true;
            }
        }

        if (!found)
        {
            _decoStars.Clear();
            return false;
        }

        for (int i = 0; i < n; i++)
        {
            Transform t = slots[i];
            _progressStars.Add(t);
            _progressHomeLocalPos.Add(t != null ? t.localPosition : Vector3.zero);
            _progressHomeLocalScale.Add(t != null ? t.localScale : Vector3.one);
        }

        System.Random rng = new System.Random(starFieldSeed + 1);
        for (int i = 0; i < _decoStars.Count; i++)
        {
            _decoSpinSpeed.Add(8f + (float)rng.NextDouble() * 20f);
        }

        Debug.Log("[SkyController] 씬에 배치된 별 사용: 진행도 별 " + n + "칸, 장식 별 " + _decoStars.Count + "개");
        return true;
    }

    private static int ParseTrailingIndex(string name)
    {
        int end = name.Length;
        int start = end;
        while (start > 0 && char.IsDigit(name[start - 1])) start--;
        if (start == end) return -1;
        int v;
        return int.TryParse(name.Substring(start), out v) ? v : -1;
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

    // 이번 클리어 연출이 모두 끝날 때까지 걸리는 시간. GameManager가 다음 화면으로 넘어가기 전에 기다리는 데 쓴다.
    // 마지막 스테이지는 가장 큰 별이 떨어진 뒤 남은 별까지 전부 떨어져야 하므로 더 길다.
    public float GetDawnDuration()
    {
        bool isFinal = CountClearedStages() >= StageProgress.StageCount;
        if (!isFinal) return transitionDuration;

        float restStart = Mathf.Min(starFallDuration, transitionDuration) + 0.1f;
        float restEnd = restStart + Mathf.Max(0f, finalRestFallStagger) + Mathf.Max(0.1f, finalRestFallDuration);
        return Mathf.Max(transitionDuration, restEnd) + 0.4f;
    }

    // 인스펙터 컴포넌트 메뉴(점 3개)에서 플레이 중에 실행해 마지막 스테이지 클리어 연출을 바로 볼 수 있다.
    [ContextMenu("테스트/마지막 스테이지 클리어 연출 재생 (플레이 중에만)")]
    public void TestFinalClearSequence()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SkyController] 플레이 중에만 실행할 수 있습니다.");
            return;
        }
        for (int i = 1; i <= StageProgress.StageCount; i++) StageProgress.MarkCleared(i);
        TriggerDawn();
        // 연출 코루틴은 시작하는 순간 클리어 수를 이미 읽었으므로, 테스트용으로 넣은 클리어 기록은 바로 지운다.
        StageProgress.ResetAll();
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

        float bigStarFall = Mathf.Min(starFallDuration, transitionDuration);
        StartCoroutine(DropStar3D(fallIndex, bigStarFall));

        // 마지막 스테이지: 가장 큰 별이 떨어진 뒤, 남아 있는 별(장식 별 등)도 전부 떨어뜨린다.
        _finalRestFallFinished = !isFinal;
        if (isFinal)
        {
            StartCoroutine(FinalRestFallRoutine(bigStarFall + 0.1f, fallIndex));
        }

        float from = _currentBrightFrac;
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / transitionDuration);
            float eased = p * p * (3f - 2f * p);
            ApplySkyColors(Mathf.Lerp(from, targetFraction, eased));

            if (isFinal && daySkyGroup != null && !daySkyGroup.gameObject.activeSelf)
            {
                daySkyGroup.gameObject.SetActive(true);
            }
            yield return null;
        }
        ApplySkyColors(targetFraction);

        if (isFinal)
        {
            // 남은 별이 전부 떨어질 때까지 기다린 뒤에 밤 하늘 그룹을 끈다.
            while (!_finalRestFallFinished) yield return null;

            if (nightSkyGroup != null) nightSkyGroup.gameObject.SetActive(false);
            _isDay = true;
            _fullyDay = true;
            Debug.Log("[SkyController] 마지막 별에 이어 남은 별도 모두 떨어지고 하늘이 완전히 밝아졌습니다.");
        }
        else
        {
            Debug.Log("[SkyController] 스테이지 클리어, 하늘 밝기 갱신: " + clearedCount + "/" + StageProgress.StageCount);
        }

        _isAnimating = false;
    }

    // 큰 별이 떨어진 뒤 호출: 아직 보이는 별을 모두 모아서 제각각의 시간차로 떨어뜨린다.
    private IEnumerator FinalRestFallRoutine(float waitBeforeStart, int skipProgressIndex)
    {
        float w = 0f;
        while (w < waitBeforeStart)
        {
            w += Time.deltaTime;
            yield return null;
        }

        var remaining = new List<Transform>();
        for (int i = 0; i < _progressStars.Count; i++)
        {
            if (i == skipProgressIndex) continue; // 방금 떨어진 큰 별은 제외
            if (_progressStars[i] != null && _progressStars[i].gameObject.activeSelf) remaining.Add(_progressStars[i]);
        }
        for (int i = 0; i < _decoStars.Count; i++)
        {
            if (_decoStars[i] != null && _decoStars[i].gameObject.activeSelf) remaining.Add(_decoStars[i]);
        }

        System.Random rng = new System.Random(4242);
        _restFalling = remaining.Count;
        for (int i = 0; i < remaining.Count; i++)
        {
            float delay = (float)rng.NextDouble() * Mathf.Max(0f, finalRestFallStagger);
            StartCoroutine(DropRestStar(remaining[i], delay, Mathf.Max(0.1f, finalRestFallDuration), 1000 + i));
        }

        while (_restFalling > 0) yield return null;
        _finalRestFallFinished = true;
    }

    // 남은 별 하나: 잠깐 흔들린 뒤 아래로 떨어지며 작아져 사라진다 (큰 별 연출과 같은 모양, 더 짧게).
    private IEnumerator DropRestStar(Transform star, float delay, float duration, int seed)
    {
        float wt = 0f;
        while (wt < delay)
        {
            wt += Time.deltaTime;
            yield return null;
        }

        if (star != null && star.gameObject.activeSelf)
        {
            Vector3 startPos = star.localPosition;
            Vector3 startScale = star.localScale;
            System.Random rng = new System.Random(seed);

            float jitterDuration = Mathf.Min(0.25f, duration * 0.3f);
            float jt = 0f;
            while (jt < jitterDuration)
            {
                jt += Time.deltaTime;
                float shake = 0.05f * (1f - jt / jitterDuration);
                float ox = ((float)rng.NextDouble() * 2f - 1f) * shake;
                float oy = ((float)rng.NextDouble() * 2f - 1f) * shake;
                star.localPosition = startPos + new Vector3(ox, oy, 0f);
                yield return null;
            }
            star.localPosition = startPos;

            Vector3 endPos = startPos + new Vector3(0f, -5.5f, 0f);
            float fallDuration = Mathf.Max(0.05f, duration - jitterDuration);
            float ft = 0f;
            while (ft < fallDuration)
            {
                ft += Time.deltaTime;
                float p = Mathf.Clamp01(ft / fallDuration);
                star.localPosition = Vector3.Lerp(startPos, endPos, p * p);
                star.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                yield return null;
            }

            star.gameObject.SetActive(false);
            star.localPosition = startPos;
            star.localScale = startScale;
        }

        _restFalling--;
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
