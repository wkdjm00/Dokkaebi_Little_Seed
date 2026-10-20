using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 씨앗 조각 수집 오브젝트. 도깨비(Player 태그)가 지나가면 자동 수집되며,
    // 수집 시 반짝이는 파티클이 한번 터진다 (SeedGlowEffect 참고).
    // 리스폰(발각/추락)되면 이번 생에 모았던 씨앗은 전부 다시 모아야 하므로,
    // Destroy 대신 비활성화만 해두고 GameManager가 리스폰 시 다시 활성화시킨다.
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(SeedGlowEffect))]
    public class SeedFragment : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GameManager.Instance != null) GameManager.Instance.NotifyFragmentCollected(this);

            var glow = GetComponent<SeedGlowEffect>();
            Color popColor = glow != null ? glow.glowColor : new Color(1f, 0.95f, 0.5f);
            SeedGlowEffect.SpawnPopEffect(transform.position, popColor);

            gameObject.SetActive(false);
        }

        // GameManager가 리스폰 시 호출해서 이 씨앗을 다시 주울 수 있는 상태로 되돌린다.
        public void Reactivate()
        {
            if (this == null) return;
            gameObject.SetActive(true);
        }
    }
}
