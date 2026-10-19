using UnityEngine;
using UnityEngine.InputSystem;

namespace DokkaebiStoneBridge
{
    // 마우스 클릭을 레이캐스트해서, 클릭된 바닥(RotatingFloor)에게 동작을 요청한다.
    // 그냥 클릭 = 45도 회전, E키를 누른 채 클릭 = Z축 뒤집기.
    // 주의: 플레이어가 바닥 위에 서 있으면 카메라 레이가 바닥보다 플레이어(캡슐)를 먼저
    // 맞추는 경우가 많다. 그러면 클릭이 씹힌 것처럼 보이므로, Player 태그는 건너뛰고
    // 그 다음으로 가까운 히트를 검사한다.
    public class FloorClickRouter : MonoBehaviour
    {
        public LayerMask clickMask = ~0;

        private void Update()
        {
            if (Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            var cam = Camera.main;
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 200f, clickMask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RotatingFloor floor = null;
            Collider usedCollider = null;
            foreach (var h in hits)
            {
                if (h.collider.CompareTag("Player")) continue; // 플레이어 자신은 클릭 대상에서 제외
                var f = h.collider.GetComponentInParent<RotatingFloor>();
                if (f != null)
                {
                    floor = f;
                    usedCollider = h.collider;
                    break;
                }
            }

            if (floor == null)
            {
                Debug.Log("[FloorClickRouter] 클릭했지만 회전 가능한 바닥을 찾지 못함 (히트 수: " + hits.Length + ")");
                return;
            }

            bool flipHeld = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            Debug.Log("[FloorClickRouter] 클릭 대상: " + usedCollider.name + " / " + (flipHeld ? "뒤집기(E+클릭)" : "회전(클릭)"));
            if (flipHeld) floor.TryFlip();
            else floor.TryRotate();
        }
    }
}
