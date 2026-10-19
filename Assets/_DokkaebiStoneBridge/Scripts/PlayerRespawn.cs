using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어가 바닥 밖으로 떨어지면 마지막 안전 위치로, 발각/몬스터 접촉 시에는
    // GameManager가 지정하는 위치(시작 지점)로 리스폰시킨다.
    // 리스폰 직후에는 주변 감시자들에게 다시 유예시간을 줘서, 리스폰 위치가 하필 감시자의
    // 시야/인식 범위 안이어도 곧바로 다시 발각되는 무한 루프에 빠지지 않게 한다.
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerRespawn : MonoBehaviour
    {
        public float fallY = -10f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (transform.position.y < fallY)
            {
                RespawnToLastSafe();
            }
        }

        public void RespawnToLastSafe()
        {
            Vector3 target = GameManager.Instance != null ? GameManager.Instance.LastSafePosition : transform.position;
            RespawnTo(target);
        }

        public void RespawnTo(Vector3 target)
        {
            transform.SetParent(null, true);
            transform.position = target + Vector3.up * 0.3f;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            GrantGraceToNearbyWatchers();

            Debug.Log("[PlayerRespawn] 리스폰 위치: " + target);
        }

        // 현재 씬에서 활성화된 모든 감시자(WatcherVision)에게 각자의 startGraceDuration만큼
        // 유예시간을 다시 부여한다. 비활성 스테이지의 감시자는 FindObjectsInactive.Exclude로
        // 애초에 대상에서 제외된다.
        private void GrantGraceToNearbyWatchers()
        {
            var watchers = Object.FindObjectsByType<WatcherVision>(FindObjectsInactive.Exclude);
            foreach (var w in watchers)
            {
                if (w == null) continue;
                w.GrantGrace(w.startGraceDuration);
            }
        }
    }
}
