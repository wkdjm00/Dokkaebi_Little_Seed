using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 플레이어는 빛나는 눈을 가지고 있어서 스스로 주변을 밝혀준다.
    // 캡슐(플레이스홀더) 머리 높이쯤에 포인트 라이트를 붙여서 밤 배경에서도
    // 플레이어 주변이 따뜻한 주황빛으로 환하게 밝혀지도록 한다 (이전 씨앗 라이트와 같은 색감).
    // 실제 모델이 들어오면 eyeHeight/위치를 눈 위치에 맞게 조절하면 된다.
    public class PlayerGlow : MonoBehaviour
    {
        public Color glowColor = new Color(1f, 0.6f, 0.2f);
        public float range = 7f;
        public float intensity = 3f;
        public float eyeHeight = 0.5f;

        [Header("은은한 깜빡임")]
        public bool flicker = true;
        public float flickerAmount = 0.4f;
        public float flickerSpeed = 3f;

        private Light _light;
        private float _t;

        private void Awake()
        {
            var lightGO = new GameObject("PlayerEyeGlow (AUTO)");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, eyeHeight, 0f);

            _light = lightGO.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = glowColor;
            _light.range = range;
            _light.intensity = intensity;
            _light.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (!flicker || _light == null) return;
            _t += Time.deltaTime;
            _light.intensity = intensity + Mathf.Sin(_t * flickerSpeed) * flickerAmount;
        }
    }
}
