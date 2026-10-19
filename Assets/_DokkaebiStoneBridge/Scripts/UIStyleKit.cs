using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace DokkaebiStoneBridge
{
    // 코드로 생성하는 UI를 "둥글둥글하고 아기자기한" 느낌으로 만들기 위한 보조 도구 모음.
    // - 라운드 사각형 / 원형 스프라이트를 절차적으로 생성 (별도 이미지 에셋 불필요, 흰색으로 만들고
    //   Image.color로 색을 입혀서 재사용한다)
    // - 버튼 호버/클릭 시 통통 튀는 펀치 애니메이션 (UIButtonPunch)
    // - 패널(카드)이 켜질 때 살짝 튀어오르듯 나타나는 팝인 애니메이션 (UIPopIn)
    public static class UIStyleKit
    {
        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;

        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedSprite == null) _roundedSprite = CreateRoundedSprite(128, 40);
                return _roundedSprite;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circleSprite == null) _circleSprite = CreateRoundedSprite(64, 32);
                return _circleSprite;
            }
        }

        public static Sprite CreateRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, radius, size - radius - 1);
                    float cy = Mathf.Clamp(y, radius, size - radius - 1);
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float a = Mathf.Clamp01(0.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();

            int r = Mathf.Min(radius, size / 2);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        public static Color Lighten(Color c, float t) => Color.Lerp(c, Color.white, t);
        public static Color Darken(Color c, float t) => Color.Lerp(c, Color.black, t);
    }

    // 버튼에 붙이면 마우스 오버 시 살짝 커지고, 누르면 살짝 눌리는 통통 튀는 피드백을 준다.
    public class UIButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.06f;
        public float pressScale = 0.92f;
        public float speed = 14f;

        private Vector3 _target = Vector3.one;
        private bool _hovering;

        private void OnEnable()
        {
            transform.localScale = Vector3.one;
            _target = Vector3.one;
            _hovering = false;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _target, Time.unscaledDeltaTime * speed);
        }

        public void OnPointerEnter(PointerEventData e) { _hovering = true; _target = Vector3.one * hoverScale; }
        public void OnPointerExit(PointerEventData e) { _hovering = false; _target = Vector3.one; }
        public void OnPointerDown(PointerEventData e) { _target = Vector3.one * pressScale; }
        public void OnPointerUp(PointerEventData e) { _target = _hovering ? Vector3.one * hoverScale : Vector3.one; }
    }

    // 패널/카드가 활성화될 때(OnEnable) 살짝 튀어오르듯 팝인 되는 애니메이션.
    public class UIPopIn : MonoBehaviour
    {
        public float duration = 0.28f;
        public float overshoot = 1.08f;

        private Coroutine _routine;

        private void OnEnable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            float t = 0f;
            transform.localScale = Vector3.one * 0.7f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float scale = p < 0.7f
                    ? Mathf.Lerp(0.7f, overshoot, p / 0.7f)
                    : Mathf.Lerp(overshoot, 1f, (p - 0.7f) / 0.3f);
                transform.localScale = Vector3.one * scale;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
