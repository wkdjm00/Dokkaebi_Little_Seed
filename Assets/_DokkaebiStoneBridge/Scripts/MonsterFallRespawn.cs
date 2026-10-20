using System.Collections;
using UnityEngine;

namespace DokkaebiStoneBridge
{
    [RequireComponent(typeof(Rigidbody))]
    public class MonsterFallRespawn : MonoBehaviour
    {
        public float fallY = -10f;
        public float respawnDelay = 3f;

        public bool IsIncapacitated { get; private set; }

        private Rigidbody _rb;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Vector3 _homePos;
        private Quaternion _homeRot;
        private Transform _homeParent;
        private bool _respawning;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            _homePos = transform.position;
            _homeRot = transform.rotation;
            _homeParent = transform.parent;
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

            transform.SetParent(null, true);
            SetVisible(false);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            yield return new WaitForSeconds(respawnDelay);

            transform.SetParent(_homeParent, true);
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

        public void ResetToHome()
        {
            StopAllCoroutines();
            _respawning = false;
            IsIncapacitated = false;

            transform.SetParent(_homeParent, true);
            transform.position = _homePos;
            transform.rotation = _homeRot;

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in _renderers) if (r != null) r.enabled = visible;
            foreach (var c in _colliders) if (c != null) c.enabled = visible;
        }
    }
}
