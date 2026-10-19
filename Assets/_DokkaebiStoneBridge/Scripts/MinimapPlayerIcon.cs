using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 미니맵 RawImage 위에서 플레이어 위치를 실시간으로 보여주는 마커.
    // 매 프레임 현재 활성화된 MinimapRegistry.Active 카메라 기준으로
    // 플레이어의 뷰포트 좌표를 구해 mapRect 안의 anchoredPosition으로 변환한다.
    public class MinimapPlayerIcon : MonoBehaviour
    {
        [Tooltip("실시간으로 위치를 표시할 대상 (보통 Player)")]
        public Transform target;

        [Tooltip("이 마커가 그 안에서 움직일 미니맵 RawImage의 RectTransform")]
        public RectTransform mapRect;

        private RectTransform _selfRect;

        private void Awake()
        {
            _selfRect = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            var cam = MinimapRegistry.Active;
            if (cam == null || target == null || mapRect == null) return;

            Vector3 viewport = cam.WorldToViewportPoint(target.position);
            Rect rect = mapRect.rect;
            float x = Mathf.Clamp01(viewport.x) * rect.width + rect.xMin;
            float y = Mathf.Clamp01(viewport.y) * rect.height + rect.yMin;
            _selfRect.anchoredPosition = new Vector2(x, y);
        }
    }
}
