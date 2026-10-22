using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어나 밀 수 있는 오브젝트(PushableBlock/PushableTotem)가 올라서면 눌리는 압력판(발판).
    // FloorLift 등 다른 스크립트가 IsPressed를 읽어서 반응하면 된다.
    public class PressurePlate : MonoBehaviour
    {
        public bool IsPressed => _weightCount > 0;

        private int _weightCount;

        [Header("눌림 표현")]
        [Tooltip("눌릴 때 아래로 내려갈 모델. 비워두면 'Visual' 자식 -> 첫 번째 자식 순으로 자동 선택")]
        public Transform visual;
        [Tooltip("눌렸을 때 내려가는 깊이(월드 단위)")]
        public float pressDepth = 0.08f;
        [Tooltip("내려가고 올라오는 속도 (클수록 빠름)")]
        public float pressSpeed = 14f;

        private Vector3 _visualRestLocalPos;
        private float _pressAmount;

        private void Awake()
        {
            if (visual == null)
            {
                visual = transform.Find("Visual");
                if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            }
            if (visual != null) _visualRestLocalPos = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null) return;
            float target = IsPressed ? 1f : 0f;
            _pressAmount = Mathf.Lerp(_pressAmount, target, 1f - Mathf.Exp(-pressSpeed * Time.unscaledDeltaTime));
            if (Mathf.Abs(_pressAmount - target) < 0.001f) _pressAmount = target;
            visual.localPosition = _visualRestLocalPos + Vector3.down * (pressDepth * _pressAmount);
        }

        private void OnDisable()
        {
            _pressAmount = 0f;
            if (visual != null) visual.localPosition = _visualRestLocalPos;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanPress(other)) return;
            _weightCount++;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!CanPress(other)) return;
            _weightCount = Mathf.Max(0, _weightCount - 1);
        }

        private bool CanPress(Collider other)
        {
            return other.CompareTag("Player")
                || other.GetComponentInParent<PushableBlock>() != null
                || other.GetComponentInParent<PushableTotem>() != null
                || other.GetComponentInParent<RideableTotem>() != null;
        }
    }
}
