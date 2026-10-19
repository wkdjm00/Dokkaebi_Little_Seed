using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 씨앗 조각 수집 오브젝트. 도깨비(Player 태그)가 지나가면 자동 수집되며,
    // 수집 시 반짝이는 파티클이 한번 터진다 (SeedGlowEffect 참고).
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
            if (GameManager.Instance != null) GameManager.Instance.NotifyFragmentCollected();

            var glow = GetComponent<SeedGlowEffect>();
            Color popColor = glow != null ? glow.glowColor : new Color(1f, 0.95f, 0.5f);
            SeedGlowEffect.SpawnPopEffect(transform.position, popColor);

            Destroy(gameObject);
        }
    }
}
