using UnityEngine;

namespace DokkaebiStoneBridge
{
    [RequireComponent(typeof(Rigidbody))]
    public class WatcherVision : MonoBehaviour
    {
        public Vector3 zoneOffset = new Vector3(0f, 0f, 1.5f);
        public float zoneRadius = 1.5f;
        public float zoneHeight = 1.2f;
        public float eyeHeight = 0.9f;
        public float awarenessRadius = 3f;
        public float chaseSpeed = 2.5f;
        public float loseInterestDelay = 0.6f;
        public float rotateSpeed = 8f;
        public float startGraceDuration = 2.5f;
        public bool patrol;
        public float patrolDistance = 2f;
        public float patrolSpeed = 1f;

        [Header("칸 단위 순찰 경로 (바닥 블럭 1칸 = cellSize)")]
        [Tooltip("순서대로 이동할 칸 수. x=동(+X) 칸, y=북(+Z) 칸. 시작 위치 기준 상대값. 비워두면 위의 patrol(좌우 왕복) 방식을 그대로 사용. 예) (3,0) = 동쪽으로 3칸 갔다가 처음 자리로 되돌아옴")]
        public Vector2Int[] patrolSteps;
        [Tooltip("체크: 마지막 지점에서 시작점으로 곧장 돌아와 반복(네모 같은 순환 경로). 해제: 왔던 길을 그대로 되돌아가는 왕복")]
        public bool patrolLoop;
        [Tooltip("바닥 블럭 한 칸의 크기(월드 단위)")]
        public float cellSize = 1f;
        [Tooltip("순찰 이동 속도(월드 단위/초)")]
        public float patrolMoveSpeed = 1.5f;
        [Tooltip("지점에 도착했을 때 잠깐 멈추는 시간(초)")]
        public float patrolPauseTime = 0.3f;
        public bool IsChasing { get; private set; }
        public bool IsInGrace => Time.time < _graceUntil;
        public void GrantGrace(float duration)
        {
            _graceUntil = Mathf.Max(_graceUntil, Time.time + Mathf.Max(0f, duration));
        }
        private Vector3 _patrolStart;
        private Vector3 _patrolAxis = Vector3.right;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homePatrolStart;
        private Vector3 _homePatrolAxis;
        private Vector3[] _pathPoints;
        private int _pathIndex;
        private int _pathDir = 1;
        private float _pathWaitUntil;
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
            _patrolStart = transform.position;
            _patrolAxis = transform.right;
            _lastMoveDir = transform.forward;
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            _homePatrolStart = _patrolStart;
            _homePatrolAxis = _patrolAxis;
            BuildPatrolPath();
        }
        private void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
            _graceUntil = Time.time + Mathf.Max(0f, startGraceDuration);
        }
        public void ResetToHome()
        {
            IsChasing = false;
            _loseInterestAt = -1f;
            transform.position = _homePosition;
            transform.rotation = _homeRotation;
            _patrolStart = _homePatrolStart;
            _patrolAxis = _homePatrolAxis;
            _lastMoveDir = _homeRotation * Vector3.forward;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            ResetPatrolPath();
            GrantGrace(startGraceDuration);
        }
        private void FixedUpdate()
        {
            if (Incapacitated)
            {
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
            else if (HasPathPatrol)
            {
                UpdatePathPatrol();
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
        private bool HasPathPatrol { get { return patrolSteps != null && patrolSteps.Length > 0; } }

        // 시작 위치 + 칸 단위 이동 목록 -> 월드 좌표 지점 목록 (씬 뷰 기즈모에서도 사용)
        private Vector3[] BuildPathPoints(Vector3 origin)
        {
            var pts = new Vector3[patrolSteps.Length + 1];
            pts[0] = origin;
            Vector3 p = origin;
            for (int i = 0; i < patrolSteps.Length; i++)
            {
                p += new Vector3(patrolSteps[i].x, 0f, patrolSteps[i].y) * cellSize;
                pts[i + 1] = p;
            }
            return pts;
        }

        private void BuildPatrolPath()
        {
            _pathPoints = HasPathPatrol ? BuildPathPoints(transform.position) : null;
            ResetPatrolPath();
        }

        private void ResetPatrolPath()
        {
            _pathIndex = 0;
            _pathDir = 1;
            _pathWaitUntil = 0f;
        }

        private void UpdatePathPatrol()
        {
            Vector3 vel = _rb.linearVelocity;
            if (_pathPoints == null || _pathPoints.Length < 2 || Time.time < _pathWaitUntil)
            {
                _rb.linearVelocity = new Vector3(0f, vel.y, 0f);
                return;
            }

            Vector3 to = _pathPoints[_pathIndex] - transform.position;
            to.y = 0f;
            float step = patrolMoveSpeed * Time.fixedDeltaTime;
            if (to.magnitude <= Mathf.Max(0.05f, step))
            {
                // 지점 도착: 잠깐 멈췄다가 다음 지점으로
                _pathWaitUntil = Time.time + patrolPauseTime;
                AdvancePathIndex();
                _rb.linearVelocity = new Vector3(0f, vel.y, 0f);
                return;
            }

            Vector3 dir = to.normalized;
            _rb.linearVelocity = new Vector3(dir.x * patrolMoveSpeed, vel.y, dir.z * patrolMoveSpeed);
            _lastMoveDir = dir;
            FaceDirection(dir);
        }

        private void AdvancePathIndex()
        {
            if (patrolLoop)
            {
                // 마지막 지점 다음엔 시작점으로 곧장 이동 (순환)
                _pathIndex = (_pathIndex + 1) % _pathPoints.Length;
            }
            else
            {
                // 끝에 닿으면 왔던 길을 되돌아감 (왕복)
                int next = _pathIndex + _pathDir;
                if (next >= _pathPoints.Length || next < 0)
                {
                    _pathDir = -_pathDir;
                    next = _pathIndex + _pathDir;
                }
                _pathIndex = next;
            }
        }

        // 추격하다 놓친 뒤에는 가장 가까운 경로 지점으로 돌아가서 순찰을 이어간다.
        private void SnapToNearestPathPoint()
        {
            if (_pathPoints == null) return;
            float best = float.MaxValue;
            int bestIndex = _pathIndex;
            for (int i = 0; i < _pathPoints.Length; i++)
            {
                float dx = _pathPoints[i].x - transform.position.x;
                float dz = _pathPoints[i].z - transform.position.z;
                float d = dx * dx + dz * dz;
                if (d < best) { best = d; bestIndex = i; }
            }
            _pathIndex = bestIndex;
            _pathWaitUntil = 0f;
        }

#if UNITY_EDITOR
        // 컴포넌트 우클릭(⋮) 메뉴에서 예시 패턴을 바로 채울 수 있다. 이후 patrolSteps 숫자를 직접 고치면 된다.
        [ContextMenu("순찰 패턴 예시/직선 왕복 3칸")]
        private void PresetLine3() { ApplyPreset(new[] { new Vector2Int(3, 0) }, false); }

        [ContextMenu("순찰 패턴 예시/네모 3x3 (순환)")]
        private void PresetSquare3() { ApplyPreset(new[] { new Vector2Int(3, 0), new Vector2Int(0, 3), new Vector2Int(-3, 0), new Vector2Int(0, -3) }, true); }

        [ContextMenu("순찰 패턴 예시/ㄱ자 왕복 (3칸 + 3칸)")]
        private void PresetGiyeok() { ApplyPreset(new[] { new Vector2Int(3, 0), new Vector2Int(0, -3) }, false); }

        [ContextMenu("순찰 패턴 예시/ㄴ자 왕복 (3칸 + 3칸)")]
        private void PresetNieun() { ApplyPreset(new[] { new Vector2Int(0, 3), new Vector2Int(3, 0) }, false); }

        private void ApplyPreset(Vector2Int[] steps, bool loop)
        {
            UnityEditor.Undo.RecordObject(this, "순찰 패턴 예시 적용");
            patrolSteps = steps;
            patrolLoop = loop;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        // 선택했을 때 씬 뷰에 순찰 경로(칸 단위)를 그려서 보면서 수정할 수 있다.
        private void OnDrawGizmosSelected()
        {
            if (!HasPathPatrol) return;
            Vector3 origin = (Application.isPlaying && _pathPoints != null) ? _pathPoints[0] : transform.position;
            Vector3[] pts = BuildPathPoints(origin);
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.95f);
            for (int i = 0; i < pts.Length; i++)
            {
                Gizmos.DrawSphere(pts[i] + Vector3.up * 0.05f, i == 0 ? 0.14f : 0.1f);
                if (i > 0) Gizmos.DrawLine(pts[i - 1] + Vector3.up * 0.05f, pts[i] + Vector3.up * 0.05f);
            }
            if (patrolLoop && pts.Length > 1) Gizmos.DrawLine(pts[pts.Length - 1] + Vector3.up * 0.05f, pts[0] + Vector3.up * 0.05f);
        }
#endif

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
                _loseInterestAt = -1f;
            }
            else if (IsChasing)
            {
                if (_loseInterestAt < 0f) _loseInterestAt = Time.time + loseInterestDelay;
                if (Time.time >= _loseInterestAt)
                {
                    IsChasing = false;
                    _loseInterestAt = -1f;
                    _patrolStart = transform.position;
                    _patrolAxis = transform.right;
                    SnapToNearestPathPoint();
                }
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (Incapacitated) return;
            var totem = collision.collider.GetComponentInParent<PushableTotem>();
            if (totem != null) totem.TryPush(_lastMoveDir);
        }
        public Vector3 GetZoneCenter()
        {
            Quaternion rot = Application.isPlaying ? Quaternion.LookRotation(_lastMoveDir, Vector3.up) : transform.rotation;
            return transform.position + rot * zoneOffset;
        }
        public bool IsInsideZone(Vector3 worldPos)
        { return IsWithinRadius(worldPos, zoneRadius); }
        public bool IsWithinAwareness(Vector3 worldPos)
        { return IsWithinRadius(worldPos, awarenessRadius); }
        private bool IsWithinRadius(Vector3 worldPos, float radius)
        {
            Vector3 center = GetZoneCenter();
            float dx = worldPos.x - center.x;
            float dz = worldPos.z - center.z;
            if ((dx * dx + dz * dz) > radius * radius) return false;
            if (Mathf.Abs(worldPos.y - center.y) > zoneHeight) return false;
            return HasClearLineOfSight(center + Vector3.up * eyeHeight, worldPos + Vector3.up * eyeHeight);
        }
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
