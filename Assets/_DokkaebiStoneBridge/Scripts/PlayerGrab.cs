using UnityEngine;
using UnityEngine.InputSystem;

namespace DokkaebiStoneBridge
{
    // E키를 누르고 있는 동안 가까운 PushableBlock을 붙잡아서 플레이어를 따라오게 한다('당기기').
    // 미는 건 그냥 캐릭터로 부딪히면 물리적으로 밀리는 것으로 처리되고(PushableBlock),
    // 당기는 것만 이 컴포넌트가 담당한다. (E는 FloorClickRouter의 '뒤집기(E+클릭)'와 키는 겹치지만,
    // 하나는 클릭이 필요하고 하나는 근처 블록이 있어야 하므로 실사용상 크게 충돌하지 않는다.)
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerGrab : MonoBehaviour
    {
        public float grabRange = 1.4f;

        private Rigidbody _grabbedRb;
        private Vector3 _grabOffset; // 월드 공간 오프셋 (플레이어 회전과 무관하게 일정 거리 유지)

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.eKey.wasPressedThisFrame)
            {
                TryGrabNearest();
            }
            if (!kb.eKey.isPressed)
            {
                Release();
            }
        }

        private void FixedUpdate()
        {
            if (_grabbedRb == null) return;
            _grabbedRb.MovePosition(transform.position + _grabOffset);
        }

        private void TryGrabNearest()
        {
            var hits = Physics.OverlapSphere(transform.position, grabRange);
            PushableBlock best = null;
            float bestDist = float.MaxValue;

            foreach (var h in hits)
            {
                var block = h.GetComponentInParent<PushableBlock>();
                if (block == null) continue;
                float d = Vector3.Distance(transform.position, block.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = block;
                }
            }

            if (best != null)
            {
                _grabbedRb = best.GetComponent<Rigidbody>();
                if (_grabbedRb != null)
                {
                    _grabOffset = best.transform.position - transform.position;
                    Debug.Log("[PlayerGrab] 블록을 붙잡음: " + best.name);
                }
            }
        }

        private void Release()
        {
            if (_grabbedRb != null) Debug.Log("[PlayerGrab] 블록을 놓음.");
            _grabbedRb = null;
        }
    }
}
