using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 압력판을 밟은 뒤 pressDelay(기본 1.5초)가 지나야 올라가기 시작하고,
    // 발판에서 내려온 뒤에도 releaseDelay(기본 1.5초)동안은 계속 올라가 있는 상태를 유지하는 리프트.
    public class FloorLift : MonoBehaviour
    {
        public PressurePlate trigger;
        public float raisedHeight = 2f;
        public float moveSpeed = 2f;
        [Tooltip("체크하면 한 번 올라간 뒤로는 계속 올라가 있는 상태 유지")]
        public bool staysUpOncePressed = false;
        [Tooltip("발판을 밟은 뒤, 이 시간(초)이 지나야 실제로 올라가기 시작한다")]
        public float pressDelay = 1.5f;
        [Tooltip("발판에서 내려온 뒤, 이 시간(초) 동안은 계속 올라가 있는 상태를 유지한다 (건너갈 시간을 줌)")]
        public float releaseDelay = 1.5f;

        private Vector3 _downPos;
        private Vector3 _upPos;
        private bool _everPressed;
        private float _pressTimer;
        private float _releaseTimer;

        private void Start()
        {
            _downPos = transform.position;
            _upPos = _downPos + Vector3.up * raisedHeight;
        }

        private void Update()
        {
            bool pressed = trigger != null && trigger.IsPressed;

            if (pressed) _pressTimer += Time.deltaTime;
            else _pressTimer = 0f;

            bool pressedLongEnough = _pressTimer >= pressDelay;

            if (pressedLongEnough)
            {
                _everPressed = true;
                _releaseTimer = releaseDelay; // 실제로 올라간 뒤에만 '내려가기 전 유예시간'을 채워준다
            }
            else if (_releaseTimer > 0f)
            {
                _releaseTimer -= Time.deltaTime;
            }

            bool holdingUp = staysUpOncePressed ? _everPressed : (pressedLongEnough || _releaseTimer > 0f);
            Vector3 target = holdingUp ? _upPos : _downPos;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        }
    }
}
