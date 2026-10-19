using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 씨앗 조각 / 목표 지점 등에 붙이는 아주 가벼운 장식용 회전 효과.
    public class SpinFx : MonoBehaviour
    {
        public float spinSpeed = 90f;

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
