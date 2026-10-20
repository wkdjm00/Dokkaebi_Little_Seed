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
