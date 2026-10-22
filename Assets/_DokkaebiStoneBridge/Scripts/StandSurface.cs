using System.Collections.Generic;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 바닥/리프트 등 위에 플레이어가 올라서면, 플레이어를 그 대상의 자식으로 붙여서
    // 회전하거나 움직일 때 플레이어도 함께 따라가게 만드는 공용 컴포넌트
    // (모뉴먼트 밸리처럼 바닥과 함께 플레이어도 돈다를 구현하는 핵심 트릭).
    // carryMonsters를 켜면 몬스터(WatcherVision이 붙은 오브젝트)도 같은 방식으로 같이 옮겨진다
    // (예: 몬스터를 실어서 멀리 치워버리는 리프트/바닥).
    //
    // [겹친 바닥 우선순위]
    // 플레이어가 여러 바닥에 동시에 걸쳐 있으면(바닥끼리 겹쳐 있거나, 돌아가는 바닥이 돌다가 다른 바닥에 닿을 때)
    // 우선순위가 가장 높은 바닥 하나만 플레이어를 태운다.
    //   1순위: 돌아가는 바닥(RotatingFloor, 클릭 회전)
    //   2순위: 뒤집히는 바닥(RotatingFloor, E+클릭 뒤집기)
    //   3순위: 그냥 바닥
    // 우선순위가 같으면 먼저 올라탄 바닥이 계속 유지한다.
    // 덕분에 다른 바닥에 닿거나 벗어나도 돌아가는 바닥에서 플레이어가 떨어져 나가지 않는다.
    public class StandSurface : MonoBehaviour
    {
        [Tooltip("플레이어를 자식으로 붙일 대상. 비워두면 이 오브젝트의 부모(바닥 본체)를 사용한다.")]
        public Transform attachTarget;

        [Tooltip("켜면 플레이어뿐 아니라 몬스터(WatcherVision)도 이 바닥에 올라서면 같이 실어서 옮긴다.")]
        public bool carryMonsters = false;

        public bool PlayerIsStanding { get; private set; }

        // 겹친 바닥끼리의 우선순위 (클수록 우선). 돌아가는 바닥 2 > 뒤집히는 바닥 1 > 그냥 바닥 0
        public int Priority
        {
            get
            {
                if (_priority < 0) _priority = ComputePriority();
                return _priority;
            }
        }

        private int _priority = -1;
        private Rigidbody _standingRb;
        private bool _ignoreExit;
        private readonly List<Transform> _carriedMonsters = new List<Transform>();

        // 지금 플레이어와 겹쳐 있는 바닥들, 그리고 그중 플레이어를 실제로 태우고 있는 바닥
        private static readonly List<StandSurface> s_overlapping = new List<StandSurface>();
        private static StandSurface s_owner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_overlapping.Clear();
            s_owner = null;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnDisable()
        {
            s_overlapping.Remove(this);
            if (s_owner == this) s_owner = null;
        }

        private Transform Target => attachTarget != null ? attachTarget : (transform.parent != null ? transform.parent : transform);

        private int ComputePriority()
        {
            RotatingFloor rf = GetComponentInParent<RotatingFloor>();
            if (rf == null) return 0;
            if (rf.allowPlainRotate) return 2;
            if (rf.allowFlip) return 1;
            return 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerIsStanding = true;
                _standingRb = other.attachedRigidbody;
                RecordSafeHere();
                if (!s_overlapping.Contains(this)) s_overlapping.Add(this);
                ResolveOwner(other.transform);
                return;
            }

            if (carryMonsters)
            {
                var watcher = other.GetComponentInParent<WatcherVision>();
                if (watcher != null && !_carriedMonsters.Contains(watcher.transform))
                {
                    watcher.transform.SetParent(Target, true);
                    _carriedMonsters.Add(watcher.transform);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            RecordSafeHere();

            // 장승에서 내린 직후처럼 플레이어가 부모 없이 바닥 위에 서 있으면 다시 태운다.
            if (other.transform.parent == null && s_overlapping.Contains(this)) ResolveOwner(other.transform);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (_ignoreExit) return; // 회전/뒤집기 애니메이션 도중의 일시적 충돌 변화는 떨어짐으로 치지 않음
                PlayerIsStanding = false;
                _standingRb = null;
                s_overlapping.Remove(this);
                ResolveOwner(other.transform);
                return;
            }

            if (carryMonsters)
            {
                var watcher = other.GetComponentInParent<WatcherVision>();
                if (watcher != null && _carriedMonsters.Remove(watcher.transform))
                {
                    watcher.transform.SetParent(null, true);
                }
            }
        }

        // 겹쳐 있는 바닥들 중 우선순위가 가장 높은 바닥(같으면 기존에 태우던 바닥)을 골라 플레이어를 그 바닥의 자식으로 붙인다.
        // 겹친 바닥이 하나도 없으면 풀어준다.
        private static void ResolveOwner(Transform player)
        {
            s_overlapping.RemoveAll(s => s == null);

            StandSurface best = null;
            int bestPriority = int.MinValue;
            foreach (var s in s_overlapping)
            {
                int p = s.Priority;
                if (p > bestPriority || (p == bestPriority && s == s_owner))
                {
                    best = s;
                    bestPriority = p;
                }
            }

            // 바닥이 아닌 다른 것(장승 등)이 플레이어를 자기 자식으로 태우고 있다면 건드리지 않는다.
            Transform cur = player.parent;
            bool managed = cur == null || cur.GetComponentInChildren<StandSurface>(true) != null;
            if (!managed) return;

            if (best == null)
            {
                if (cur != null) player.SetParent(null, true);
                s_owner = null;
                return;
            }

            if (cur != best.Target) player.SetParent(best.Target, true);
            s_owner = best;
        }

        // 플레이어가 이 블록 위에서 떨어졌을 때 되돌아갈 위치 = 이 트리거(=블록 중앙)의 위치.
        // 플레이어의 순간 좌표(가장자리일 수 있음) 대신 블록 중앙을 기록해서,
        // 리스폰하자마자 다시 떨어지는 문제를 막는다.
        private void RecordSafeHere()
        {
            if (GameManager.Instance != null) GameManager.Instance.RecordSafePosition(transform.position);
        }

        // RotatingFloor가 회전/뒤집기 애니메이션 도중 플레이어를 강제로 붙잡아둘 때 사용.
        // 물리(Rigidbody)를 일시적으로 Kinematic으로 바꿔서, 부모(바닥)의 회전을 트랜스폼 계층을 통해
        // 그대로 따라가게 만든다. Kinematic이 아니면 물리 엔진이 매 프레임 플레이어 위치를 다시 계산하면서
        // 빠르게 도는 바닥(특히 피벗에서 먼 긴 다리)과 충돌 반응을 일으켜 플레이어가 튕겨나가거나
        // 바닥만 혼자 돌고 플레이어는 남겨지는 문제가 생긴다.
        public void BeginForcedCarry()
        {
            _ignoreExit = true;
            if (_standingRb != null)
            {
                _standingRb.useGravity = false;
                _standingRb.linearVelocity = Vector3.zero;
                _standingRb.angularVelocity = Vector3.zero;
                _standingRb.isKinematic = true;
            }
        }

        // 회전/뒤집기(E+클릭, Z축 180도)가 끝나면 호출됨.
        // 뒤집기처럼 바닥이 위아래로 뒤집히는 경우, 부모를 따라간 플레이어의 회전값도 함께 기울어지므로
        // 애니메이션이 끝나는 즉시 플레이어를 수평으로 똑바로 선 상태로 보정해서,
        // 뒤집힌 뒤에도 바로 정상적으로 걸어다닐 수 있게 한다 (바라보는 방향(Yaw)만 유지).
        public void EndForcedCarry()
        {
            _ignoreExit = false;
            if (_standingRb != null)
            {
                _standingRb.isKinematic = false;
                _standingRb.useGravity = true;

                Transform playerT = _standingRb.transform;
                Vector3 fwd = playerT.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                playerT.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);

                // 회전 중에는 나감(Exit) 신호를 무시했으므로, 끝난 뒤 실제로 아직 이 바닥 위에 있는지 한 번 확인한다.
                if (!IsStillTouching(playerT.position))
                {
                    PlayerIsStanding = false;
                    _standingRb = null;
                    s_overlapping.Remove(this);
                    ResolveOwner(playerT);
                }
            }
        }

        private bool IsStillTouching(Vector3 playerPos)
        {
            var col = GetComponent<Collider>();
            if (col == null) return true;
            Vector3 cp = col.ClosestPoint(playerPos);
            return (cp - playerPos).sqrMagnitude <= 0.8f * 0.8f;
        }

        // 바닥이 사라지거나(VanishingBlock) 갑자기 치워질 때, 콜라이더를 끄기 전에 먼저 호출해서
        // 올라타 있던 플레이어/몬스터를 명시적으로 풀어준다 (OnTriggerExit가 확실히 불린다는
        // 보장이 없으므로 안전하게 직접 처리).
        public void ForceReleaseAll()
        {
            if (_standingRb != null)
            {
                PlayerIsStanding = false;
                _ignoreExit = false;
                Transform playerT = _standingRb.transform;
                _standingRb = null;
                s_overlapping.Remove(this);
                // 다른 바닥(그냥 바닥 등)에 아직 걸쳐 있으면 그쪽에 태우고, 없으면 풀어준다.
                ResolveOwner(playerT);
            }

            foreach (var m in _carriedMonsters)
            {
                if (m != null) m.SetParent(null, true);
            }
            _carriedMonsters.Clear();
        }
    }
}
