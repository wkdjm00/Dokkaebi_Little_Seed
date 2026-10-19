using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 모든 씨앗 조각을 모은 뒤 도깨비가 닿으면 세계수가 깨어나는 제단.
    // 게임을 끝내는 게 아니라 밤->아침으로 하늘이 밝아지는 연출을 시작시킨다.
    [RequireComponent(typeof(Collider))]
    public class EndingAltar : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.AllFragmentsCollected) return;

            GameManager.Instance.OnWorldTreeTouched();
        }
    }
}
