using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어가 트리거 범위 안에 있는지만 감지한다 (부모-자식으로 '태우지' 않음).
    // 수평축(X)으로 접혔다 펼쳐지는 바닥(FlipBridge)을 조작할 때 쓴다: 플레이어는
    // 실제로는 옆의 단단한 발판 위에 서 있는 채로 조작하는 거라서, 발판에 붙여서
    // 같이 돌릴 필요가 없다 (오히려 붙이면 수평축 회전 중에 위치가 뒤틀려서
    // 바닥 밑으로 빠지는 문제가 생긴다).
    public class ProximitySensor : MonoBehaviour
    {
        public bool PlayerNearby { get; private set; }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) PlayerNearby = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) PlayerNearby = false;
        }
    }
}
