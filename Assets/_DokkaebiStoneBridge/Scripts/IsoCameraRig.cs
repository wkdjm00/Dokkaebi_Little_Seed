using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 모뉴먼트 밸리 느낌의 고정 아이소메트릭 시점.
    // followTarget이 설정되면 플레이 중 그 대상을 부드럽게 따라간다
    // (스테이지가 바뀌면 StageLayoutManager가 followTarget/lookAtPoint를 즉시 재설정한다).
    [ExecuteAlways]
    public class IsoCameraRig : MonoBehaviour
    {
        public Vector3 lookAtPoint = Vector3.zero;
        public float distance = 12f;
        public float orthoSize = 5f;

        [Header("추적 (선택)")]
        [Tooltip("설정하면 플레이 중 이 대상을 부드럽게 따라간다")]
        public Transform followTarget;
        public float followLerpSpeed = 3f;

        [System.NonSerialized] public Vector3 shakeOffset = Vector3.zero;

        private void OnEnable() { Apply(); }
        private void OnValidate() { Apply(); }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (followTarget != null)
            {
                lookAtPoint = Vector3.Lerp(lookAtPoint, followTarget.position, followLerpSpeed * Time.deltaTime);
            }
            Apply();
        }

        public void Apply()
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            transform.position = lookAtPoint - transform.forward * distance + shakeOffset;
        }
    }
}
