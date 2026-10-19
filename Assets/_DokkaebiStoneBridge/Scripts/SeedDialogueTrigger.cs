using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace DokkaebiStoneBridge
{
    // 씨앗(또는 다른 대상)이 여러 줄의 대사를 순서대로 말하는 범용 대화 트리거.
    // 플레이어가 트리거 범위에 들어오면(once=true면 최초 1회만) lines를 순서대로 보여준다.
    // 2스테이지(씨앗의 첫 대사), 4스테이지(이야기꾼 대사), 8스테이지(감시자와의 대화 조우 이벤트)
    // 등에서 재사용한다.
    //
    // 2026-10-19 2차 UI 라운드: 캐릭터 머리 위에 뜨던 월드스페이스 말풍선 대신, 화면 하단에
    // 고정으로 뜨는 대화창(UIManager.ShowDialogueLine)을 사용하도록 전면 교체함. anchor/
    // bubbleColor/heightOffset은 더 이상 쓰이지 않지만(레거시 필드), 기존에 씬에 배치된
    // 트리거의 직렬화된 값이 깨지지 않도록 필드 자체는 남겨둔다. UIManager가 없는 경우
    // (예: UI 없는 테스트 씬) 자동으로 기존 월드스페이스 말풍선 방식으로 폴백한다.
    public class SeedDialogueTrigger : MonoBehaviour
    {
        [TextArea(1, 3)]
        public string[] lines;
        public float lineDuration = 2.4f;
        [Tooltip("(레거시, 더 이상 사용하지 않음 - UIManager 폴백 전용) 말풍선 높이 오프셋")]
        public float heightOffset = 1.8f;
        [Tooltip("(레거시, 더 이상 사용하지 않음 - UIManager 폴백 전용) 말풍선을 띄울 위치")]
        public Transform anchor;
        public bool once = true;
        [Tooltip("true면 8스테이지 조우 캐릭터처럼 대화창에 초상화가 있는 버전을 사용한다.")]
        public bool showPortrait = false;
        [Tooltip("(레거시, 더 이상 사용하지 않음 - UIManager 폴백 전용)")]
        public Color bubbleColor = Color.white;
        public Color textColor = new Color(0.2f, 0.16f, 0.1f);

        private bool _fired;

        // --- 폴백용 월드스페이스 말풍선 (UIManager가 없을 때만 생성) ---
        private Canvas _canvas;
        private RectTransform _bubbleRect;
        private Image _bubbleImage;
        private Text _text;
        private Camera _cam;
        private Transform _followTarget;
        private bool _useFallbackBubble;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void Start()
        {
            _useFallbackBubble = UIManager.Instance == null;
            if (_useFallbackBubble)
            {
                _cam = Camera.main;
                BuildBubble();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (once && _fired) return;
            _fired = true;
            _followTarget = anchor != null ? anchor : other.transform;
            StopAllCoroutines();
            StartCoroutine(_useFallbackBubble ? PlaySequence() : PlaySequenceUI());
        }

        // 새 화면 하단 대화창 경로. 각 줄을 lineDuration 동안 보여주되, 플레이어가 대화창의
        // 삼각형(다음) 버튼을 클릭하면 즉시 다음 줄로 넘어간다.
        private IEnumerator PlaySequenceUI()
        {
            if (lines == null || lines.Length == 0) yield break;
            var ui = UIManager.Instance;
            if (ui == null) yield break;

            foreach (var line in lines)
            {
                ui.ShowDialogueLine(line, showPortrait);
                float t = 0f;
                while (t < lineDuration)
                {
                    if (ui.DialogueAdvanceRequested) break;
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            ui.HideDialoguePanel();
        }

        private void BuildBubble()
        {
            var canvasObj = new GameObject(name + "_DialogueCanvas");
            canvasObj.transform.SetParent(null);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.transform.localScale = Vector3.one * 0.012f;

            var rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 100f);

            var bubbleObj = new GameObject("Bubble");
            bubbleObj.transform.SetParent(canvasObj.transform, false);
            _bubbleImage = bubbleObj.AddComponent<Image>();
            if (UIStyleKit.Circle != null)
            {
                _bubbleImage.sprite = UIStyleKit.Circle;
                _bubbleImage.type = Image.Type.Sliced;
            }
            _bubbleImage.color = bubbleColor;
            _bubbleRect = bubbleObj.GetComponent<RectTransform>();
            _bubbleRect.sizeDelta = new Vector2(240f, 90f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(bubbleObj.transform, false);
            _text = textObj.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 22;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.color = textColor;
            _text.text = "";
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.08f);
            textRect.anchorMax = new Vector2(0.92f, 0.92f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            canvasObj.SetActive(false);
        }

        private IEnumerator PlaySequence()
        {
            if (lines == null || lines.Length == 0) yield break;
            _canvas.gameObject.SetActive(true);

            foreach (var line in lines)
            {
                _text.text = line;
                _canvas.transform.localScale = Vector3.one * 0.012f;
                _bubbleImage.color = bubbleColor;
                _text.color = textColor;

                float t = 0f;
                float pop = 0.2f;
                while (t < pop)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / pop);
                    float s = p < 0.7f ? Mathf.Lerp(0f, 1.1f, p / 0.7f) : Mathf.Lerp(1.1f, 1f, (p - 0.7f) / 0.3f);
                    _bubbleRect.localScale = Vector3.one * s;
                    yield return null;
                }
                _bubbleRect.localScale = Vector3.one;

                yield return new WaitForSeconds(lineDuration);
            }

            float ft = 0f;
            float fadeDur = 0.25f;
            while (ft < fadeDur)
            {
                ft += Time.deltaTime;
                float p = Mathf.Clamp01(ft / fadeDur);
                var c = bubbleColor; c.a = 1f - p;
                _bubbleImage.color = c;
                var tc = textColor; tc.a = 1f - p;
                _text.color = tc;
                yield return null;
            }
            _canvas.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf || _followTarget == null) return;
            _canvas.transform.position = _followTarget.position + Vector3.up * heightOffset;
            if (_cam != null) _canvas.transform.rotation = _cam.transform.rotation;
        }
    }
}
