using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 미니맵 카메라를 스테이지별로 두되(각 StageRoot 아래 하나씩), 현재 활성화된
    // 스테이지의 카메라만 OnEnable에서 자기 자신을 등록한다.
    // UI 쪽(MinimapPlayerIcon)은 이 static Active 참조만 보고 투영 좌표를 계산한다.
    [RequireComponent(typeof(Camera))]
    public class MinimapRegistry : MonoBehaviour
    {
        public static Camera Active { get; private set; }

        private Camera _cam;

        private void Awake() => _cam = GetComponent<Camera>();

        private void OnEnable() => Active = _cam;

        private void OnDisable()
        {
            if (Active == _cam) Active = null;
        }
    }
}
