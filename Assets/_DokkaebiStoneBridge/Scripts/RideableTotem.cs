using UnityEngine;
using UnityEngine.InputSystem;

namespace DokkaebiStoneBridge
{
    // 장승.
    //  - 밀기 : 플레이어가 밀면 물리로 밀리는 대로 이동한다. 회전은 항상 고정(돌거나 쓰러지지 않음).
    //  - 타기 : 장승 꼭대기에 올라서면 플레이어가 장승에 얹히고, WASD가 장승을 움직인다(플레이어는 같이 실려 이동).
    //           내리려면 dismountKey(기본 Space)를 누른다.
    [RequireComponent(typeof(Rigidbody))]
    public class RideableTotem : MonoBehaviour
    {
        [Tooltip("올라탄 채 WASD로 움직일 때의 이동 속도")]
        public float rideSpeed = 2.4f;
        [Tooltip("올라탄 상태에서 내릴 때 누르는 키")]
        public Key dismountKey = Key.Space;
        [Tooltip("내릴 때, 마지막으로 이동하던 방향으로 옮겨주는 거리")]
        public float dismountDistance = 1.0f;
        [Tooltip("플레이어 발바닥이 장승 꼭대기 높이에서 이만큼 이내일 때만 '올라탔다'고 본다(옆에서 미는 것과 구분)")]
        public float rideHeightTolerance = 0.3f;
        [Tooltip("장승 몸통 가장자리에서 이만큼 안쪽/바깥쪽까지 '꼭대기 위'로 본다")]
        public float rideMargin = 0.15f;

        public bool IsRiding { get; private set; }

        private Rigidbody _rb;
        private Collider _body;
        private PlayerMover _mover;
        private Vector3 _lastDir = Vector3.forward;
        private float _noRideUntil;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation; // 밀려도 회전값은 고정
            _body = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            if (_mover == null) _mover = FindAnyObjectByType<PlayerMover>();
            if (_mover == null) return;

            if (!IsRiding)
            {
                TryBeginRide();
                return;
            }

            // 리스폰 등으로 플레이어가 장승에서 떨어져 나갔으면 탑승 해제
            if (_mover.transform.parent != transform)
            {
                EndRide(false);
                return;
            }

            bool locked = GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.Cleared);
            Vector3 move = locked ? Vector3.zero : ReadMove();
            Vector3 vel = move * rideSpeed;
            vel.y = _rb.linearVelocity.y;
            _rb.linearVelocity = vel;
            if (move.sqrMagnitude > 0.0001f) _lastDir = move.normalized;
        }

        private void Update()
        {
            if (!IsRiding) return;
            var kb = Keyboard.current;
            if (kb != null && kb[dismountKey].wasPressedThisFrame) EndRide(true);
        }

        private void TryBeginRide()
        {
            if (Time.time < _noRideUntil || _body == null) return;
            var pc = _mover.GetComponent<Collider>();
            if (pc == null) return;

            Bounds tb = _body.bounds;
            Bounds pb = pc.bounds;
            float topY = tb.max.y;
            bool above = pb.min.y >= topY - rideHeightTolerance && pb.min.y <= topY + rideHeightTolerance + 0.5f;
            Vector3 d = _mover.transform.position - tb.center;
            bool within = Mathf.Abs(d.x) <= tb.extents.x + rideMargin && Mathf.Abs(d.z) <= tb.extents.z + rideMargin;
            if (above && within) BeginRide();
        }

        private void BeginRide()
        {
            var prb = _mover.GetComponent<Rigidbody>();
            prb.linearVelocity = Vector3.zero;
            prb.angularVelocity = Vector3.zero;
            prb.useGravity = false;
            prb.isKinematic = true;
            _mover.transform.SetParent(transform, true);
            _mover.InputSuspended = true;
            IsRiding = true;
        }

        private void EndRide(bool dismount)
        {
            IsRiding = false;
            _noRideUntil = Time.time + 0.6f;
            if (_mover == null) return;

            var pt = _mover.transform;
            if (pt.parent == transform) pt.SetParent(null, true);

            if (dismount)
            {
                Vector3 dir = _lastDir.sqrMagnitude > 0.0001f ? _lastDir : pt.forward;
                pt.position += dir * dismountDistance + Vector3.up * 0.1f;
            }

            Vector3 fwd = pt.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            pt.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);

            var prb = _mover.GetComponent<Rigidbody>();
            prb.isKinematic = false;
            prb.useGravity = true;
            prb.linearVelocity = Vector3.zero;
            _mover.InputSuspended = false;
        }

        private static Vector3 ReadMove()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector3.zero;
            float x = 0f, z = 0f;
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.sKey.isPressed) z -= 1f;
            if (kb.wKey.isPressed) z += 1f;
            Vector3 m = new Vector3(x, 0f, z);
            if (m.sqrMagnitude > 1f) m.Normalize();
            return m;
        }
    }
}
