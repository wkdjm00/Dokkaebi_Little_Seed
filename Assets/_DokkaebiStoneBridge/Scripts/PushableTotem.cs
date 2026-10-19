using System.Collections;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 장승 모양의 밀 수 있는 오브젝트. 플레이어처럼 물리로 자연스럽게 미끄러지는 대신,
    // 한 번 밀면 격자 한 칸만큼 딱딱 끊어서 이동한다 (소코반 스타일).
    // 민 방향을 X축 또는 Z축 중 하나로 고정해서, 대각선으로 어중간하게 밀리지 않게 한다.
    //
    // + 한 방향으로만 밀리도록: allowedPushDirection을 인스펙터에서 직접 지정할 수도 있고,
    //   비워두면(0,0,0) 처음 성공적으로 밀린 방향을 자동으로 기억해서 그 뒤로는 그 방향의
    //   밀기만 허용한다 (반대/다른 축 방향 밀기는 무시됨).
    // + 계속 맞닿아 있다고 계속 밀리지 않도록: 한 번 이동을 시작하면 그 이동이 끝나고도
    //   약간의 재접촉 쿨다운(pushCooldown)을 둬서, '밀린 만큼만' 딱 한 칸만 가고 멈춘다.
    //   (호출부인 PlayerMover/WatcherVision도 OnCollisionEnter 기준으로 호출하도록 같이 수정함)
    //
    // 꼭대기에 StandSurface(RideSurface)가 있어서, 플레이어가 위에 올라가 있으면
    // 장승이 이동할 때 같이 옮겨간다 (회전 바닥과 같은 BeginForcedCarry 방식 재사용).
    [RequireComponent(typeof(Rigidbody))]
    public class PushableTotem : MonoBehaviour
    {
        [Tooltip("한 번에 이동하는 거리 (보통 바닥 블럭 한 칸)")]
        public float stepDistance = 4f;
        public float moveDuration = 0.3f;

        [Tooltip("이 방향으로만 밀리게 고정. (0,0,0)이면 처음 성공한 밀기 방향을 자동으로 기억해서 그 방향으로만 고정한다.")]
        public Vector3 allowedPushDirection = Vector3.zero;

        [Tooltip("한 칸 이동이 끝난 뒤, 다시 밀 수 있게 되기까지의 최소 대기 시간 (계속 붙어서 미는 것으로 연속 이동하는 것 방지)")]
        public float pushCooldown = 0.15f;

        public bool IsMoving { get; private set; }

        private Rigidbody _rb;
        private StandSurface _rideSurface;
        private Vector3 _lockedDirection = Vector3.zero; // 자동 학습된 방향 (allowedPushDirection이 비어있을 때 사용)
        private float _cooldownUntil;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true; // 물리 충돌로 직접 밀리지 않고, TryPush로만 이동한다.
            _rb.freezeRotation = true;
            _rideSurface = GetComponentInChildren<StandSurface>();

            if (allowedPushDirection.sqrMagnitude > 0.0001f)
            {
                _lockedDirection = LockToPrimaryAxis(allowedPushDirection);
            }
        }

        // moverForward: 미는 쪽(플레이어/몬스터)이 향하고 있는 방향. 가장 큰 축 하나로 스냅한다.
        public bool TryPush(Vector3 moverForward)
        {
            if (IsMoving) return false;
            if (Time.time < _cooldownUntil) return false; // 방금 한 칸 간 직후 쿨다운 중

            Vector3 dir = LockToPrimaryAxis(moverForward);
            if (dir == Vector3.zero) return false;

            // 이미 고정된 방향이 있으면, 그 방향이 아닌 밀기는 무시한다.
            if (_lockedDirection != Vector3.zero && Vector3.Dot(dir, _lockedDirection) < 0.99f)
            {
                return false;
            }

            Vector3 target = transform.position + dir * stepDistance;
            StartCoroutine(MoveStep(target, dir));
            return true;
        }

        private Vector3 LockToPrimaryAxis(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
                return new Vector3(Mathf.Sign(dir.x), 0f, 0f);
            return new Vector3(0f, 0f, Mathf.Sign(dir.z));
        }

        private IEnumerator MoveStep(Vector3 target, Vector3 usedDirection)
        {
            IsMoving = true;
            // 처음 성공한 밀기라면, 이 방향으로 영구 고정한다 (인스펙터에 직접 지정 안 했을 때만).
            if (_lockedDirection == Vector3.zero) _lockedDirection = usedDirection;

            // 위에 플레이어가 타고 있으면, 회전 바닥과 똑같은 방식으로 물리를 잠깐 꺼서 같이 옮겨간다.
            if (_rideSurface != null) _rideSurface.BeginForcedCarry();

            Vector3 start = transform.position;
            float t = 0f;
            while (t < moveDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(t / moveDuration));
                yield return null;
            }
            transform.position = target;

            if (_rideSurface != null) _rideSurface.EndForcedCarry();
            IsMoving = false;
            _cooldownUntil = Time.time + pushCooldown;
        }
    }
}
