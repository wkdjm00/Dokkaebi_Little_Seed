using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 이 컴포넌트가 붙은 콜라이더는 감시자의 시야/인식 판정을 가로막는 "벽" 역할을 한다.
    // WatcherVision이 플레이어를 보려고 할 때 이 컴포넌트가 붙은 콜라이더에 선이 가로막히면
    // 발각/추격 판정 자체가 발생하지 않는다 (플레이어가 숨을 수 있는 안전지대를 만드는 용도).
    public class VisionBlocker : MonoBehaviour
    {
    }
}
