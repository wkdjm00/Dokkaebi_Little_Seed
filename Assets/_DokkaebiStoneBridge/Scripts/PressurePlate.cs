using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어나 밀 수 있는 오브젝트(PushableBlock/PushableTotem)가 올라서면 눌리는 압력판(발판).
    // FloorLift 등 다른 스크립트가 IsPressed를 읽어서 반응하면 된다.
    public class PressurePlate : MonoBehaviour
    {
        public bool IsPressed => _weightCount > 0;

        private int _weightCount;

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
                || other.GetComponentInParent<PushableTotem>() != null;
        }
    }
}
