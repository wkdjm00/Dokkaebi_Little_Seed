using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 몬스터(방해 캐릭터)의 몸에 직접 닿아도 시야 감지(WatcherVision)와 동일하게 '발각' 처리한다.
    // 단, 같은 오브젝트에 WatcherVision이 붙어있고 지금 유예시간(startGraceDuration) 중이라면
    // 시야 감지와 똑같이 무시한다 (스테이지 시작 직후나 리스폰 직후 몸이 겹쳐서 바로 또 잡히는 것을 방지).
    public class WatcherTouch : MonoBehaviour
    {
        private WatcherVision _vision;

        private void Awake()
        {
            _vision = GetComponent<WatcherVision>();
        }

        private bool InGrace => _vision != null && _vision.IsInGrace;

        private void OnCollisionEnter(Collision collision)
        {
            if (InGrace) return;
            if (collision.collider.CompareTag("Player") && GameManager.Instance != null)
                GameManager.Instance.OnDetected();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (InGrace) return;
            if (other.CompareTag("Player") && GameManager.Instance != null)
                GameManager.Instance.OnDetected();
        }
    }
}
