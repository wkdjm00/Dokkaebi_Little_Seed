using System.Collections.Generic;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 바닥/리프트 등 위에 플레이어가 올라서면, 플레이어를 그 대상의 자식으로 붙여서
    // 회전하거나 움직일 때 플레이어도 함께 따라가게 만드는 공용 컴포넌트
    // (모뉴먼트 밸리처럼 "바닥과 함께 플레이어도 돈다"를 구현하는 핵심 트릭).
    // carryMonsters를 켜면 몬스터(WatcherVision이 붙은 오브젝트)도 같은 방식으로 같이 옮겨진다
    // (예: 몬스터를 실어서 멀리 치워버리는 리프트/바닥).
    public class StandSurface : MonoBehaviour
    {
        [Tooltip("플레이어를 자식으로 붙일 대상. 비워두면 이 오브젝트의 부모(바닥 본체)를 사용한다.")]
        public Transform attachTarget;

        [Tooltip("켜면 플레이어뿐 아니라 몬스터(WatcherVision)도 이 바닥에 올라서면 같이 실어서 옮긴다.")]
        public bool carryMonsters = false;

        public bool PlayerIsStanding { get; private set; }

        private Rigidbody _standingRb;
        private bool _ignoreExit;
        private readonly List<Transform> _carriedMonsters = new List<Transform>();

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private Transform Target => attachTarget != null ? attachTarget : (transform.parent != null ? transform.parent : transform);

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerIsStanding = true;
                _standingRb = other.attachedRigidbody;
                RecordSafeHere();
                other.transform.SetParent(Target, true);
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
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (_ignoreExit) return; // 회전/뒤집기 애니메이션 도중의 일시적 충돌 변화는 '떨어짐'으로 치지 않음
                PlayerIsStanding = false;
                _standingRb = null;
                other.transform.SetParent(null, true);
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
        // 애니메이션이 끝나는 즉시 플레이어를 '수평으로 똑바로 선' 상태로 보정해서,
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
            }
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
                _standingRb.transform.SetParent(null, true);
                _standingRb = null;
            }

            foreach (var m in _carriedMonsters)
            {
                if (m != null) m.SetParent(null, true);
            }
            _carriedMonsters.Clear();
        }
    }
}
