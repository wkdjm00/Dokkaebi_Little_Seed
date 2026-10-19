using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어가 물리적으로 밀 수 있는 오브젝트. 압력판(PressurePlate) 위에 올려두면
    // 계속 눌러놓을 수 있다 ("발판을 누를 수 있는 무언가").
    [RequireComponent(typeof(Rigidbody))]
    public class PushableBlock : MonoBehaviour
    {
        private void Reset()
        {
            var rb = GetComponent<Rigidbody>();
            rb.mass = 2f;
            rb.linearDamping = 2f;
            rb.angularDamping = 4f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
