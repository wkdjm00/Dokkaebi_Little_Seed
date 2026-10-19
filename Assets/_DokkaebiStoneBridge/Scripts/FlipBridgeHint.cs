using UnityEngine;
using UnityEngine.UI;

namespace DokkaebiStoneBridge
{
    // 플레이어가 ProximitySensor 범위 안에 들어오면 '조작법' 말풍선을 띄워주는 힌트.
    // 튜토리얼은 따로 안 만들고, 이 기믹이 처음 나오는 스테이지 1에서만 붙여서 쓴다.
    [RequireComponent(typeof(ProximitySensor))]
    public class FlipBridgeHint : MonoBehaviour
    {
        public string message = "E + 클릭으로 바닥 펼치기";
        public Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);
        public Color backgroundColor = new Color(0.17f, 0.12f, 0.24f, 0.82f);
        public Color textColor = Color.white;

        private ProximitySensor _sensor;
        private Canvas _canvas;
        private Transform _canvasTransform;
        private bool _wasNearby;

        private void Awake()
        {
            _sensor = GetComponent<ProximitySensor>();
            BuildWorldCanvas();
        }

        private void BuildWorldCanvas()
        {
            var go = new GameObject("FlipBridgeHint_Canvas (AUTO)");
            _canvasTransform = go.transform;
            _canvasTransform.SetParent(null, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            var rt = _canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 90);
            go.transform.localScale = Vector3.one * 0.01f;
            go.AddComponent<CanvasScaler>();

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(go.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = backgroundColor;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(bgGO.transform, false);
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16, 8);
            textRt.offsetMax = new Vector2(-16, -8);
            var text = textGO.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            go.SetActive(false);
        }

        private void Update()
        {
            if (_sensor == null || _canvasTransform == null) return;

            bool nearby = _sensor.PlayerNearby;
            if (nearby != _wasNearby)
            {
                _canvasTransform.gameObject.SetActive(nearby);
                _wasNearby = nearby;
            }
            if (!nearby) return;

            _canvasTransform.position = transform.position + worldOffset;
            var cam = Camera.main;
            if (cam != null)
                _canvasTransform.rotation = Quaternion.LookRotation(_canvasTransform.position - cam.transform.position);
        }

        private void OnDestroy()
        {
            if (_canvasTransform != null) Destroy(_canvasTransform.gameObject);
        }
    }
}
