using System.Collections;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어가 지금 서 있는 바닥을 클릭하면 90도 회전한다 (모뉴먼트 밸리 스타일, 수직(Y)축 기준).
    // E키를 누른 채 클릭하면 '뒤집기'를 한다: 이 오브젝트가 생성될 때의 로컬 X축을 고정 축으로 삼아서,
    // 0도 -> -90도 -> -180도 -> -90도 -> 0도 ... 순서로 왕복하며 -90도씩만 움직인다
    // (양수 방향으로는 절대 돌지 않고, -180도를 넘어가지도 않는다. 끝에 닿으면 반대로 튕겨서 되돌아온다).
    //
    // standSurface: 플레이어를 '태워서'(부모로 붙여서) 같이 옮겨야 하는 경우에 사용 (Y축 회전 다리처럼
    //   플레이어가 빈 공간 위를 건너가는 발판일 때). 수직(Y)축 회전은 높이가 안 바뀌어서 안전하다.
    // proximitySensor: 플레이어를 태우지 않고 '근처에 있는지'만 보는 경우에 사용 (E+클릭 뒤집기 바닥처럼
    //   플레이어가 실제로는 옆의 단단한 바닥 위에 서서 조작하는 경우). 수평축 회전은 태운 채로 돌리면
    //   플레이어 위치가 뒤틀려서 바닥 밑으로 빠지는 버그가 생기기 때문에, 이 경우는 절대 태우지 않는다.
    // 두 참조가 모두 있으면 proximitySensor를 우선한다.
    public class RotatingFloor : MonoBehaviour
    {
        [Tooltip("이 바닥 위에 플레이어가 있으면 같이 태워서(부모로 붙여서) 옮긴다. Y축 회전(다리)용.")]
        public StandSurface standSurface;
        [Tooltip("플레이어를 태우지 않고 근처에 있는지만 본다. E+클릭 뒤집기(수평축)용.")]
        public ProximitySensor proximitySensor;

        public float rotateStepDegrees = 45f;
        public float rotateDuration = 0.25f;

        [Tooltip("E+클릭으로 뒤집을 때 한 번에 도는 각도 (항상 음수 방향으로 90도씩)")]
        public float flipStepDegrees = 90f;
        public float flipDuration = 0.3f;

        [Tooltip("일반 클릭으로 Y축 회전이 가능한지")]
        public bool allowPlainRotate = true;
        [Tooltip("E+클릭으로 뒤집기(0/-90/-180 왕복)가 가능한지")]
        public bool allowFlip = false;

        private bool _rotating;

        // 뒤집기 상태: 0, -1, -2 가 각각 0도, -90도, -180도를 의미.
        private int _flipStep = 0;
        private int _flipDir = -1; // 현재 진행 방향 (-1: 음수 쪽으로, +1: 0도 쪽으로 복귀)
        private Vector3 _flipAxis;
        private bool _flipAxisCaptured;

        private void Awake()
        {
            CaptureFlipAxis();
        }

        // 생성된 시점(배치될 때)의 로컬 X축을 뒤집기 축으로 고정한다.
        private void CaptureFlipAxis()
        {
            if (_flipAxisCaptured) return;
            _flipAxis = transform.right;
            _flipAxisCaptured = true;
        }

        private bool IsPlayerPresent()
        {
            if (proximitySensor != null) return proximitySensor.PlayerNearby;
            return standSurface != null && standSurface.PlayerIsStanding;
        }

        public void TryRotate()
        {
            if (!allowPlainRotate)
            {
                Debug.Log("[RotatingFloor] " + name + ": 이 바닥은 일반 클릭 회전이 꺼져 있음 (E+클릭 전용 바닥일 수 있음)");
                return;
            }
            if (!CanOperate())
            {
                Debug.Log("[RotatingFloor] " + name + ": 지금은 회전 불가 - " + WhyCannotOperate());
                return;
            }
            StartCoroutine(RotateAroundWorldAxis(Vector3.up, rotateStepDegrees, rotateDuration));
        }

        public void TryFlip()
        {
            if (!allowFlip)
            {
                Debug.Log("[RotatingFloor] " + name + ": 이 바닥은 E+클릭 뒤집기가 꺼져 있음");
                return;
            }
            if (!CanOperate())
            {
                Debug.Log("[RotatingFloor] " + name + ": 지금은 뒤집기 불가 - " + WhyCannotOperate());
                return;
            }
            CaptureFlipAxis();

            int nextStep = _flipStep + _flipDir;
            if (nextStep < -2 || nextStep > 0)
            {
                _flipDir = -_flipDir; // 끝(0도 또는 -180도)에 닿으면 방향을 반대로 튕긴다
                nextStep = _flipStep + _flipDir;
            }
            _flipStep = nextStep;

            // 주의: 뒤집기 바닥은 proximitySensor를 쓰는 경우 standSurface가 null이라
            // BeginForcedCarry/EndForcedCarry가 호출되지 않는다 (의도된 동작 - 플레이어는
            // 옆의 단단한 바닥 위에 그대로 있으므로 굳이 태울 필요가 없다).
            StartCoroutine(RotateAroundWorldAxis(_flipAxis, _flipDir * flipStepDegrees, flipDuration));
        }

        private bool CanOperate()
        {
            if (_rotating) return false;
            if (!IsPlayerPresent()) return false;
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.Cleared)) return false;
            return true;
        }

        private string WhyCannotOperate()
        {
            if (_rotating) return "이미 움직이는 중";
            if (proximitySensor == null && standSurface == null) return "센서 연결 안됨";
            if (!IsPlayerPresent()) return "플레이어가 이 바닥의 감지 영역 안에 없음 (가장자리 쪽으로 더 가까이 가야 할 수 있음)";
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return "게임오버 상태";
            if (GameManager.Instance != null && GameManager.Instance.Cleared) return "이미 클리어됨";
            return "알 수 없음";
        }

        private IEnumerator RotateAroundWorldAxis(Vector3 worldAxis, float degrees, float duration)
        {
            _rotating = true;
            if (standSurface != null) standSurface.BeginForcedCarry();

            Quaternion start = transform.rotation;
            Quaternion end = Quaternion.AngleAxis(degrees, worldAxis) * start;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(start, end, Mathf.Clamp01(t / duration));
                yield return null;
            }

            transform.rotation = end;

            if (standSurface != null) standSurface.EndForcedCarry();
            _rotating = false;
        }
    }
}
