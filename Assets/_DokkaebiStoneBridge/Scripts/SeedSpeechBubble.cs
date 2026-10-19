using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace DokkaebiStoneBridge
{
    // 1스테이지에서 씨앗을 먹을 때 도깨비(플레이어) 머리 위에 말풍선을 띄운다.
    // 첫 번째 씨앗은 물음표(?), 그 이후부터는 느낌표(!)가 표시된다.
    // GameManager.OnSeedCountChanged 이벤트를 구독해서 동작하며, 2스테이지 이후에는 표시하지 않는다.
    public class SeedSpeechBubble : MonoBehaviour
    {
        public float heightOffset = 1.6f;
        public float showDuration = 1.6f;
        public float popDuration = 0.25f;
        public float fadeDuration = 0.2f;
        public Color bubbleColor = Color.white;
        public Color textColor = new Color(0.25f, 0.18f, 0.08f);

        private Transform _player;
        private Canvas _canvas;
        private RectTransform _bubbleRect;
        private Image _bubbleImage;
        private Text _text;
        private Camera _cam;
        private Coroutine _routine;
        private bool _subscribed;

        private void Start()
        {
            _player = transform;
            _cam = Camera.main;

            BuildBubble();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSeedCountChanged += HandleSeedCountChanged;
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_subscribed && GameManager.Instance != null)
                GameManager.Instance.OnSeedCountChanged -= HandleSeedCountChanged;
        }

        private void BuildBubble()
        {
            var canvasObj = new GameObject("SeedSpeechBubbleCanvas");
            canvasObj.transform.SetParent(null);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.transform.localScale = Vector3.one * 0.01f;

            var rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);

            var bubbleObj = new GameObject("Bubble");
            bubbleObj.transform.SetParent(canvasObj.transform, false);
            _bubbleImage = bubbleObj.AddComponent<Image>();
            _bubbleImage.sprite = UIStyleKit.Circle;
            _bubbleImage.type = Image.Type.Sliced;
            _bubbleImage.color = bubbleColor;
            _bubbleRect = bubbleObj.GetComponent<RectTransform>();
            _bubbleRect.sizeDelta = new Vector2(70f, 70f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(bubbleObj.transform, false);
            _text = textObj.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 48;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = textColor;
            _text.text = "?";
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            canvasObj.SetActive(false);
        }

        private void HandleSeedCountChanged(int collected, int total)
        {
            if (collected <= 0) return; // Start()에서 (0,total)로 들어오는 최초 알림은 무시
            if (GameManager.Instance != null && GameManager.Instance.CurrentStage != 1) return;

            string symbol = collected == 1 ? "?" : "!";
            ShowBubble(symbol);
        }

        private void ShowBubble(string symbol)
        {
            if (_canvas == null || _player == null) return;
            _text.text = symbol;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlayBubble());
        }

        private IEnumerator PlayBubble()
        {
            _canvas.gameObject.SetActive(true);
            _canvas.transform.localScale = Vector3.one * 0.01f;
            _bubbleImage.color = bubbleColor;
            _text.color = textColor;

            float t = 0f;
            while (t < popDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / popDuration);
                float scale = p < 0.7f ? Mathf.Lerp(0f, 1.15f, p / 0.7f) : Mathf.Lerp(1.15f, 1f, (p - 0.7f) / 0.3f);
                _bubbleRect.localScale = Vector3.one * scale;
                yield return null;
            }
            _bubbleRect.localScale = Vector3.one;

            yield return new WaitForSeconds(showDuration);

            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fadeDuration);
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
            if (_canvas == null || _player == null) return;
            _canvas.transform.position = _player.position + Vector3.up * heightOffset;
            if (_cam != null)
                _canvas.transform.rotation = _cam.transform.rotation;
        }
    }
}
