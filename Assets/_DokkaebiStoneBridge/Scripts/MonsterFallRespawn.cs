using System.Collections;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 감시자(몬스터)가 사라지는 바닥이나, 눌렀을 때 가라앉는 바닥에 실려서 떨어지면
    // 몇 초간 사라졌다가(=더 이상 방해 못함) 원래 자리로 되돌아오는 컴포넌트.
    // WatcherVision이 IsIncapacitated를 확인해서, 떨어져 있는 동안은 순찰/시야 감지를 멈춘다.
    [RequireComponent(typeof(Rigidbody))]
    public class MonsterFallRespawn : MonoBehaviour
    {
        public float fallY = -10f;
        [Tooltip("떨어진 뒤 다시 나타나기까지 걸리는 시간(초)")]
        public float respawnDelay = 3f;

        public bool IsIncapacitated { get; private set; }

        private Rigidbody _rb;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Vector3 _homePos;
        private Quaternion _homeRot;
        private bool _respawning;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        private void Start()
        {
            _homePos = transform.position;
            _homeRot = transform.rotation;
        }

        private void Update()
        {
            if (!_respawning && !IsIncapacitated && transform.position.y < fallY)
            {
                StartCoroutine(FallAndRespawn());
            }
        }

        private IEnumerator FallAndRespawn()
        {
            _respawning = true;
            IsIncapacitated = true;

            transform.SetParent(null, true); // 타고 있던 바닥에서 분리
            SetVisible(false);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            yield return new WaitForSeconds(respawnDelay);

            transform.position = _homePos;
            transform.rotation = _homeRot;
            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            SetVisible(true);

            IsIncapacitated = false;
            _respawning = false;
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in _renderers) if (r != null) r.enabled = visible;
            foreach (var c in _colliders) if (c != null) c.enabled = visible;
        }
    }
}
