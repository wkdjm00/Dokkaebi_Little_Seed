using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 클리어 포인트의 마법진 연출. 씨앗을 다 모으기 전에는 어둡게, 다 모으면 밝아지면서 천천히 돈다(활성화).
    public class ClearPointMagic : MonoBehaviour
    {
        [Tooltip("마법진 렌더러. 비워두면 이름에 Magic이 들어간 자식을 자동으로 찾는다.")]
        public Renderer magic;
        public Color inactiveTint = new Color(0.35f, 0.35f, 0.35f, 1f);
        public Color activeTint = Color.white;
        public float spinSpeed = 40f;

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            if (magic == null)
            {
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name.Contains("Magic")) { magic = r; break; }
                }
            }
            _mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (magic == null) return;
            bool active = GameManager.Instance != null && GameManager.Instance.AllFragmentsCollected;
            magic.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", active ? activeTint : inactiveTint);
            magic.SetPropertyBlock(_mpb);
            if (active) magic.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
