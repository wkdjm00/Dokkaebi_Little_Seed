using UnityEngine;
using UnityEngine.InputSystem;

namespace DokkaebiStoneBridge
{
    // WASD로 직접 움직이는 플레이어 이동. 물리 기반(Rigidbody)이라
    // PushableBlock/PushableTotem을 자연스럽게 밀 수 있다.
    // (이 프로젝트는 새 Input System을 쓰므로 레거시 UnityEngine.Input은 사용하지 않는다.)
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMover : MonoBehaviour
    {
        public float moveSpeed = 3.4f;
        public float turnSpeed = 12f;

        // 장승(RideableTotem)에 올라탄 동안 true. 이때는 플레이어가 직접 이동하지 않고 장승이 대신 움직인다.
        public bool InputSuspended { get; set; }

        private Rigidbody _rb;
        private Vector3 _lastMoveDir = Vector3.forward;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true; // 충돌로 데굴데굴 구르지 않게
        }

        private void FixedUpdate()
        {
            if (InputSuspended) return;

            bool locked = GameManager.Instance != null &&
                          (GameManager.Instance.IsGameOver || GameManager.Instance.Cleared);

            if (locked)
            {
                var stay = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(0f, stay.y, 0f);
                return;
            }

            Vector2 input = ReadInput();
            Vector3 move = new Vector3(input.x, 0f, input.y);
            if (move.sqrMagnitude > 1f) move.Normalize();

            Vector3 vel = move * moveSpeed;
            vel.y = _rb.linearVelocity.y; // 수직 속도(중력, 리프트 등)는 건드리지 않음
            _rb.linearVelocity = vel;

            if (move.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = move.normalized;
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime);
            }
        }

        // 장승(PushableTotem)에 부딪히면 지금 가던 방향으로 한 칸 밀어본다.
        // OnCollisionStay가 아니라 OnCollisionEnter를 쓰는 이유: 계속 맞닿아 있다고 해서
        // 계속 밀리면(쭉 나아가는 느낌) 안 되고, '부딪힌 순간'마다 딱 한 칸만 밀려야 한다.
        // 장승이 한 칸 이동하면 접촉이 자연히 끊어졌다가, 플레이어가 다시 다가가 부딪히면
        // 그때 또 한 칸 밀리는 식으로 동작한다.
        private void OnCollisionEnter(Collision collision)
        {
            var totem = collision.collider.GetComponentInParent<PushableTotem>();
            if (totem != null) totem.TryPush(_lastMoveDir);
        }

        private Vector2 ReadInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.sKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed) y += 1f;
            return new Vector2(x, y);
        }
    }
}
