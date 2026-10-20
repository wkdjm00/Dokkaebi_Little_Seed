using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어가 바닥 밖으로 떨어지면 마지막 안전 위치로, 발각/몬스터 접촉 시에는
    // GameManager가 지정하는 위치(시작 지점)로 리스폰시킨다.
    // 리스폰되면 이번 생에서 모았던 씨앗은 전부 다시 모아야 하고, 감시자(몬스터)들도
    // 전부 원래 자리/순찰 상태로 되돌아간다 (처음부터 다시 도전하는 긴장감을 주기 위함).
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

            if (GameManager.Instance != null) GameManager.Instance.ResetCollectedSeeds();
            ResetAllWatchersToHome();

            Debug.Log("[PlayerRespawn] 리스폰 위치: " + target);
        }

        // 현재 씬에서 활성화된 모든 감시자(몬스터)를 원래 자리/순찰 상태로 되돌리고
        // 다시 유예시간을 부여한다 (리스폰 위치가 하필 감시자 시야/인식 범위 안이어도
        // 곧바로 다시 발각되는 무한 루프에 빠지지 않게 함).
        // 비활성 스테이지의 감시자는 FindObjectsInactive.Exclude로 애초에 대상에서 제외된다.
        private void ResetAllWatchersToHome()
        {
            var fallResponders = Object.FindObjectsByType<MonsterFallRespawn>(FindObjectsInactive.Exclude);
            foreach (var f in fallResponders)
            {
                if (f != null) f.ResetToHome();
            }

            var watchers = Object.FindObjectsByType<WatcherVision>(FindObjectsInactive.Exclude);
            foreach (var w in watchers)
            {
                if (w != null) w.ResetToHome();
            }
        }
    }
}
