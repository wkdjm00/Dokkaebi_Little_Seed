using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 감시자의 고정(또는 좌우 왕복) 시야 범위. 바닥에 반투명한 원형 데칼로 표시되며,
    // 플레이어가 그 원 안(수평 거리 기준) + 적절한 높이 범위에 들어오면 즉시 발각 처리한다.
    //
    // + 벽/시야 차단 (VisionBlocker): zoneRadius/awarenessRadius 안에 있어도, 감시자의 시야
    //   중심에서 플레이어까지 직선 경로가 VisionBlocker 컴포넌트가 붙은 콜라이더에 가로막히면
    //   발각되지 않고 추격 인식도 되지 않는다. 즉 벽 뒤는 '진짜 안전지대'가 된다.
    //
    // + 추격(Chase): 시야 범위(zoneRadius)보다 조금 더 넓은 '경계 범위'(awarenessRadius) 안에
    //   플레이어가 (그리고 벽에 가리지 않은 채로) 들어오면, 순찰/정지 패턴을 멈추고 플레이어 쪽으로
    //   쫓아온다. 플레이어가 경계 범위를 벗어나거나 벽 뒤로 숨으면 즉시 포기하지 않고
    //   loseInterestDelay초 동안은 계속 추격하다가, 그 시간이 지나도록 다시 보이지 않으면
    //   원래 패턴(순찰 또는 정지)으로 복귀한다.
    //
    // + 회전: 추격하거나 순찰로 이동할 때, 이동 방향을 향해 몸을 부드럽게 회전시킨다.
    // + 시작 유예: 스테이지 시작 직후 startGraceDuration초 동안은 발각/추격 판정을 하지 않는다
    //   (스폰 지점이 경계 범위 안에 들어가 있어도 플레이어가 움직일 시간을 벌어준다).
    // + 시야 바닥 표시: zoneRadius 크기의 반투명 원을 바닥에 실제로 그려서, 에디터 기즈모가 아니라
    //   실제 플레이 화면에서도 위험 범위를 알 수 있게 한다 (awarenessRadius는 표시하지 않음 — 그건
    //   플레이어에게 숨겨진 추격 트리거 범위).
    //
    // Rigidbody로 움직여서, 사라지는 바닥(VanishingBlock) 위에서는 중력에 의해 자연스럽게
    // 떨어지게 만든다 (떨어진 뒤에는 MonsterFallRespawn이 있으면 알아서 부활).
    [RequireComponent(typeof(Rigidbody))]
    public class WatcherVision : MonoBehaviour
    {
        [Tooltip("시야 영역의 중심을 이 오브젝트 기준 로컬 좌표로 얼마나 띄울지")]
        public Vector3 zoneOffset = new Vector3(0f, 0f, 1.5f);
        [Tooltip("시야 영역의 반지름 (바닥에 보이는 원의 크기). 이 안에 들어오면 즉시 발각된다 (벽에 가리지 않은 경우에만).")]
        public float zoneRadius = 1.5f;
        [Tooltip("수직으로 허용하는 높이 범위 (이 범위를 벗어난 높이에 있으면 발각되지 않음, 다른 층 분리용)")]
        public float zoneHeight = 1.2f;
        [Tooltip("시야/인식 판정에 쓰는 눈 높이 (감시자 발 기준 위로 얼마나 띄울지)")]
        public float eyeHeight = 0.9f;

        [Header("추격 (Chase)")]
        [Tooltip("이 범위 안에 플레이어가 들어오면 추격을 시작한다 (zoneRadius보다 커야 의미가 있음). 0 이하이면 추격 기능 비활성화.")]
        public float awarenessRadius = 3f;
        [Tooltip("추격 중 이동 속도")]
        public float chaseSpeed = 2.5f;
        [Tooltip("플레이어가 awarenessRadius를 벗어나거나 벽 뒤로 숨은 뒤, 이 시간(초) 동안은 계속 추격하다가\n그래도 다시 보이지 않으면 원래 패턴으로 복귀")]
        public float loseInterestDelay = 0.6f;

        [Header("회전")]
        [Tooltip("이동 방향을 바라보도록 몸을 돌리는 속도 (클수록 빨리 돈다)")]
        public float rotateSpeed = 8f;

        [Header("시작 유예")]
        [Tooltip("스테이지 시작 후 이 시간(초) 동안은 발각/추격 판정을 하지 않는다")]
        public float startGraceDuration = 2.5f;

        [Header("순찰 (선택)")]
        public bool patrol;
        public float patrolDistance = 2f;
        public float patrolSpeed = 1f;

        public bool IsChasing { get; private set; }

        // 플레이어가 리스폰된 직후 다시 즉시 발각되지 않도록, 외부(리스폰 로직)에서 유예시간을
        // 다시 부여할 수 있게 해주는 공개 API. 시야(IsInsideZone/추격) 판정과 WatcherTouch의
        // 직접 접촉 판정 둘 다 이 유예시간 동안은 발각 처리를 하지 않는다.
        public bool IsInGrace => Time.time < _graceUntil;

        public void GrantGrace(float duration)
        {
            _graceUntil = Mathf.Max(_graceUntil, Time.time + Mathf.Max(0f, duration));
        }

        private Vector3 _patrolStart;
        private Vector3 _patrolAxis = Vector3.right; // 순찰 왕복 축 (Start 시점의 방향으로 고정, 회전에 따라 흔들리지 않도록)
        private Transform _player;
        private Rigidbody _rb;
        private MonsterFallRespawn _fallRespawn;
        private Vector3 _lastMoveDir = Vector3.forward;
        private float _loseInterestAt = -1f;
        private float _graceUntil;

        private bool Incapacitated => _fallRespawn != null && _fallRespawn.IsIncapacitated;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _fallRespawn = GetComponent<MonsterFallRespawn>();
        }

        private void Start()
        {
            _patrolStart = transform.position;
            _patrolAxis = transform.right;
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;

            _graceUntil = Time.time + Mathf.Max(0f, startGraceDuration);

        }

        private void FixedUpdate()
        {
            if (Incapacitated)
            {
                var stay = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(0f, stay.y, 0f);
                return;
            }

            if (IsChasing && _player != null)
            {
                Vector3 toPlayer = _player.position - transform.position;
                toPlayer.y = 0f;
                Vector3 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

                Vector3 vel = dir * chaseSpeed;
                vel.y = _rb.linearVelocity.y;
                _rb.linearVelocity = vel;

                if (dir != Vector3.zero)
                {
                    _lastMoveDir = dir;
                    FaceDirection(dir);
                }
            }
            else if (patrol)
            {
                float x = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
                Vector3 targetPos = _patrolStart + _patrolAxis * x;
                Vector3 toTarget = targetPos - transform.position;
                toTarget.y = 0f;

                Vector3 vel = toTarget / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
                float maxSpeed = patrolDistance * patrolSpeed * 1.5f + 0.5f;
                if (vel.magnitude > maxSpeed) vel = vel.normalized * maxSpeed;
                vel.y = _rb.linearVelocity.y;
                _rb.linearVelocity = vel;

                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    _lastMoveDir = toTarget.normalized;
                    FaceDirection(_lastMoveDir);
                }
            }
            else
            {
                var stay = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(0f, stay.y, 0f);
            }
        }

        private void FaceDirection(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            float t = 1f - Mathf.Exp(-rotateSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }

        private void Update()
        {
            if (Incapacitated) return;
            if (_player == null || GameManager.Instance == null) return;
            if (GameManager.Instance.IsGameOver || GameManager.Instance.Cleared) return;
            if (IsInGrace) return;

            if (IsInsideZone(_player.position))
            {
                GameManager.Instance.OnDetected();
                return;
            }

            UpdateChaseState();
        }

        private void UpdateChaseState()
        {
            if (awarenessRadius <= 0f)
            {
                IsChasing = false;
                return;
            }

            bool inAwareness = IsWithinAwareness(_player.position);
            if (inAwareness)
            {
                IsChasing = true;
                _loseInterestAt = -1f; // 보이는 동안은 타이머를 계속 리셋
            }
            else if (IsChasing)
            {
                if (_loseInterestAt < 0f) _loseInterestAt = Time.time + loseInterestDelay;
                if (Time.time >= _loseInterestAt)
                {
                    IsChasing = false;
                    _loseInterestAt = -1f;
                    _patrolStart = transform.position;
            _patrolAxis = transform.right; // 추격을 멈춘 위치를 새 순찰 중심으로 삼는다
                }
            }
        }

        // 순찰하다가 장승(PushableTotem)과 부딪히면, 가고 있던 방향으로 한 칸 밀어버린다.
        // (계속 맞닿아 있다고 계속 밀리지 않도록 OnCollisionEnter 기준으로 동작)
        private void OnCollisionEnter(Collision collision)
        {
            if (Incapacitated) return;
            var totem = collision.collider.GetComponentInParent<PushableTotem>();
            if (totem != null) totem.TryPush(_lastMoveDir);
        }

        public Vector3 GetZoneCenter()
        {
            return transform.TransformPoint(zoneOffset);
        }

        public bool IsInsideZone(Vector3 worldPos)
        {
            return IsWithinRadius(worldPos, zoneRadius);
        }

        public bool IsWithinAwareness(Vector3 worldPos)
        {
            return IsWithinRadius(worldPos, awarenessRadius);
        }

        private bool IsWithinRadius(Vector3 worldPos, float radius)
        {
            Vector3 center = GetZoneCenter();
            float dx = worldPos.x - center.x;
            float dz = worldPos.z - center.z;
            if ((dx * dx + dz * dz) > radius * radius) return false;
            if (Mathf.Abs(worldPos.y - center.y) > zoneHeight) return false;
            return HasClearLineOfSight(center + Vector3.up * eyeHeight, worldPos + Vector3.up * eyeHeight);
        }

        // 두 지점 사이 직선 경로에 VisionBlocker가 붙은 콜라이더가 있으면 시야가 막힌 것으로 본다.
        private static bool HasClearLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 diff = to - from;
            float dist = diff.magnitude;
            if (dist < 0.001f) return true;
            Vector3 dir = diff / dist;
            RaycastHit[] hits = Physics.RaycastAll(from, dir, dist);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<VisionBlocker>() != null) return false;
            }
            return true;
        }

        private void OnDrawGizmos()
        {
            Vector3 center = GetZoneCenter();
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawSphere(center, zoneRadius * 0.98f);
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawWireSphere(center, zoneRadius);

            if (awarenessRadius > zoneRadius)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
                Gizmos.DrawWireSphere(center, awarenessRadius);
            }
        }
    }
}
