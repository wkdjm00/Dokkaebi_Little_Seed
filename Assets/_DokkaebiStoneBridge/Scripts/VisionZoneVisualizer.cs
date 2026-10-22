using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 감시자(Watcher)의 원형 시야 범위(WatcherVision)를 실제 플레이 화면에서 바닥에
    // 둥글게 깔리는 반투명 원(디스크)으로 보여준다. 매 프레임 영역 중심에서 아래로
    // 레이캐스트해서 실제 바닥 높이를 찾아 그 위에 딱 붙여서 그린다.
    //
    // 조명/시간대(일출 연출 등)나 바닥 색과 겹쳐서 잘 안 보이는 문제를 방지하기 위해,
    // 1) 기본 알파를 높이고 2) 라이팅의 영향을 받지 않는 Unlit 셰이더를 쓰며
    // 3) 바닥보다 확실히 위에서 그려지도록 렌더 큐/깊이 처리를 조정한다.
    [RequireComponent(typeof(WatcherVision))]
    public class VisionZoneVisualizer : MonoBehaviour
    {
        public Color zoneColor = new Color(1f, 0.1f, 0.05f, 0.55f);
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.15f;
        public float groundOffset = 0.06f;
        public LayerMask groundMask = ~0;

        private WatcherVision _vision;
        private Transform _visual;
        private Material _mat;
        private float _t;
        private static readonly RaycastHit[] _hitBuf = new RaycastHit[32];

        private void Awake()
        {
            _vision = GetComponent<WatcherVision>();

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "VisionZoneVisual (AUTO)";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _visual = go.transform;
            _visual.SetParent(null);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            _mat = BuildUnlitTransparentMaterial(zoneColor);
            renderer.material = _mat;
            renderer.sortingOrder = 100;

            // 위치가 잡히기 전(월드 원점)에 화면에 보이지 않도록 숨겨 둔다. 첫 Update에서 위치를 잡은 뒤 켠다.
            go.SetActive(false);
        }

        // 스테이지가 꺼질 때(감시자 비활성)는 시야 원도 같이 숨긴다. 안 그러면 원점(0,0,0)에 원통이 멈춰 남는다.
        private void OnDisable()
        {
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        private static Material BuildUnlitTransparentMaterial(Color color)
        {
            string[] candidates = {
                "Universal Render Pipeline/Unlit",
                "Sprites/Default",
                "Unlit/Transparent",
                "Unlit/Color",
                "Standard"
            };

            Shader shader = null;
            foreach (var name in candidates)
            {
                shader = Shader.Find(name);
                if (shader != null) break;
            }
            if (shader == null) shader = Shader.Find("Diffuse");

            var mat = new Material(shader);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 50;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            return mat;
        }

        // 감시자 자신/플레이어/밀 수 있는 물체/시야 차단 벽 위에 원이 얹히면 공중에 떠 보이므로 진짜 바닥만 대상으로 한다.
        private bool TryFindGround(Vector3 center, out RaycastHit best)
        {
            best = default(RaycastHit);
            int n = Physics.RaycastNonAlloc(center + Vector3.up * 5f, Vector3.down, _hitBuf, 30f, groundMask, QueryTriggerInteraction.Ignore);
            bool found = false;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Collider col = _hitBuf[i].collider;
                if (col == null || IsNotGround(col)) continue;
                if (_hitBuf[i].distance < bestDist)
                {
                    bestDist = _hitBuf[i].distance;
                    best = _hitBuf[i];
                    found = true;
                }
            }
            return found;
        }

        private static bool IsNotGround(Collider col)
        {
            if (col.GetComponentInParent<PlayerMover>() != null) return true;
            if (col.GetComponentInParent<WatcherVision>() != null) return true;
            if (col.GetComponentInParent<VisionBlocker>() != null) return true;
            if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) return true;
            return false;
        }

        private void Update()
        {
            if (_vision == null || _visual == null) return;

            Vector3 center = _vision.GetZoneCenter();

            float groundY = center.y;
            if (TryFindGround(center, out RaycastHit hit))
            {
                groundY = hit.point.y;
            }

            _visual.position = new Vector3(center.x, groundY + groundOffset, center.z);
            _visual.rotation = Quaternion.identity;
            if (!_visual.gameObject.activeSelf) _visual.gameObject.SetActive(true);

            float diameter = _vision.zoneRadius * 2f;
            _visual.localScale = new Vector3(diameter, 0.02f, diameter);

            _t += Time.deltaTime;
            float a = zoneColor.a + Mathf.Sin(_t * pulseSpeed) * pulseAmount;
            var c = zoneColor;
            c.a = Mathf.Clamp01(a);
            if (_mat != null)
            {
                _mat.color = c;
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            }
        }

        private void OnDestroy()
        {
            if (_visual != null) Destroy(_visual.gameObject);
        }
    }
}
