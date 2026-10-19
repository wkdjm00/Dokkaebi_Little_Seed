using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 씨앗 조각에 붙는 반짝이는 스파클 파티클 연출 (라이트 없음 - 주변을 밝히는 건 플레이어의
    // 빛나는 눈/PlayerGlow가 담당한다). 눈에 잘 띄도록 위아래로 살짝 떠다니며 천천히 회전한다.
    public class SeedGlowEffect : MonoBehaviour
    {
        public Color glowColor = new Color(1f, 0.95f, 0.5f);
        public float bobSpeed = 2f;
        public float bobHeight = 0.15f;
        public float spinSpeed = 60f;

        private Vector3 _basePos;
        private float _t;

        private void Awake()
        {
            _basePos = transform.localPosition;

            var psGO = new GameObject("SeedSparkles (AUTO)");
            psGO.transform.SetParent(transform, false);
            var ps = psGO.AddComponent<ParticleSystem>();
            ConfigureSparkles(ps, glowColor, false);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            transform.localPosition = _basePos + Vector3.up * Mathf.Sin(_t * bobSpeed) * bobHeight;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        public static void ConfigureSparkles(ParticleSystem ps, Color color, bool burst)
        {
            var main = ps.main;
            main.startColor = color;
            main.startSize = 0.08f;
            main.startSpeed = burst ? 2.5f : 0.6f;
            main.startLifetime = burst ? 0.5f : 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = !burst;
            main.maxParticles = 60;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            if (burst)
            {
                emission.rateOverTime = 0f;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
            }
            else
            {
                emission.rateOverTime = 6f;
            }

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            renderer.material = mat;

            if (burst)
            {
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            }

            if (!ps.isPlaying) ps.Play();
        }

        public static void SpawnPopEffect(Vector3 position, Color color)
        {
            var go = new GameObject("SeedPopFX (AUTO)");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureSparkles(ps, color, true);
            Object.Destroy(go, 1.5f);
        }
    }
}
