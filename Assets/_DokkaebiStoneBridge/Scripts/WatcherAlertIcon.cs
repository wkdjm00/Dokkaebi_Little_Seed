using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace DokkaebiStoneBridge
{
    // 감시자(WatcherVision)가 플레이어를 추격(IsChasing)하기 시작하면 머리 위에 빨간 눈 아이콘을 띄우고,
    // 추격을 포기하고 원래 패턴으로 돌아가면 아이콘을 숨긴다.
    [RequireComponent(typeof(WatcherVision))]
    public class WatcherAlertIcon : MonoBehaviour
    {
        public float heightOffset = 1.6f;
        public float iconSize = 60f;
        public float popDuration = 0.2f;
        public float fadeDuration = 0.25f;

        private WatcherVision _vision;
        private Camera _cam;
        private Canvas _canvas;
        private Image _iconImage;
        private Sprite _eyeSprite;
        private bool _wasChasing;
        private Coroutine _routine;

        private void Start()
        {
            _vision = GetComponent<WatcherVision>();
            _cam = Camera.main;
            _eyeSprite = Resources.Load<Sprite>("UI/icon_detected_eye");
            BuildIcon();
        }

        private void BuildIcon()
        {
            var canvasObj = new GameObject("WatcherAlertIconCanvas");
            canvasObj.transform.SetParent(null);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.transform.localScale = Vector3.one * 0.01f;

            var rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(canvasObj.transform, false);
            _iconImage = iconObj.AddComponent<Image>();
            if (_eyeSprite != null)
            {
                _iconImage.sprite = _eyeSprite;
                _iconImage.preserveAspect = true;
            }
            else
            {
                _iconImage.color = new Color(1f, 0.2f, 0.2f, 1f);
            }
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            canvasObj.SetActive(false);
        }

        private void Update()
        {
            if (_vision == null) return;
            bool chasing = _vision.IsChasing;
            if (chasing != _wasChasing)
            {
                _wasChasing = chasing;
                if (_routine != null) StopCoroutine(_routine);
                _routine = StartCoroutine(chasing ? PlayShow() : PlayHide());
            }
        }

        private IEnumerator PlayShow()
        {
            _canvas.gameObject.SetActive(true);
            SetAlpha(0f);
            _canvas.transform.localScale = Vector3.one * 0.006f;

            float t = 0f;
            while (t < popDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / popDuration);
                SetAlpha(p);
                _canvas.transform.localScale = Vector3.one * Mathf.Lerp(0.006f, 0.01f, p);
                yield return null;
            }

            SetAlpha(1f);
            _canvas.transform.localScale = Vector3.one * 0.01f;
        }

        private IEnumerator PlayHide()
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fadeDuration);
                SetAlpha(1f - p);
                yield return null;
            }

            SetAlpha(0f);
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private void SetAlpha(float a)
        {
            if (_iconImage == null) return;
            var c = _iconImage.color;
            c.a = a;
            _iconImage.color = c;
        }

        private void LateUpdate()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf) return;
            _canvas.transform.position = transform.position + Vector3.up * heightOffset;
            if (_cam != null) _canvas.transform.rotation = _cam.transform.rotation;
        }
    }
}
