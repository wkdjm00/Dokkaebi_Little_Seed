using System.Collections;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 일정 주기로 나타났다 사라졌다 하는 바닥. 사라지는 순간 그 위에 있던 플레이어나
    // 몬스터는(둘 다 중력이 적용되는 Rigidbody이므로) 바로 아래로 떨어진다.
    // 몬스터는 MonsterFallRespawn이 있으면 떨어진 뒤 알아서 원위치로 돌아온다.
    public class VanishingBlock : MonoBehaviour
    {
        [Tooltip("보이는(밟을 수 있는) 상태로 유지되는 시간(초)")]
        public float visibleDuration = 2.5f;
        [Tooltip("사라져 있는 상태로 유지되는 시간(초)")]
        public float hiddenDuration = 1.5f;
        [Tooltip("시작하자마자 바로 사라지지 않도록 하는 지연시간(초)")]
        public float startDelay = 0f;
        [Tooltip("true면 처음에 사라진 상태로 시작")]
        public bool startHidden = false;

        public StandSurface standSurface;

        private Renderer[] _renderers;
        private Collider[] _colliders;

        public bool IsSolid { get; private set; } = true;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        private void Start()
        {
            StartCoroutine(Loop());
        }

        private IEnumerator Loop()
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            SetSolid(!startHidden);
            yield return new WaitForSeconds(startHidden ? hiddenDuration : visibleDuration);

            while (true)
            {
                SetSolid(false);
                yield return new WaitForSeconds(hiddenDuration);
                SetSolid(true);
                yield return new WaitForSeconds(visibleDuration);
            }
        }

        private void SetSolid(bool solid)
        {
            IsSolid = solid;

            if (!solid && standSurface != null)
            {
                // 콜라이더를 끄기 전에, 올라타 있던 플레이어/몬스터를 먼저 풀어준다.
                // (콜라이더를 비활성화하는 순간 OnTriggerExit가 항상 확실히 불리는 것은 아니라서
                //  명시적으로 풀어주는 편이 안전하다.)
                standSurface.ForceReleaseAll();
            }

            foreach (var r in _renderers) if (r != null) r.enabled = solid;
            foreach (var c in _colliders) if (c != null) c.enabled = solid;
        }
    }
}
