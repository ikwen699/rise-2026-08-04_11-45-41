using UnityEngine;

namespace Rise.Systems
{
    public class TownNPC : MonoBehaviour
    {
        public Material bodyMaterial;
        public Color bodyTint = new Color(0.6f, 0.5f, 0.5f);
        public Material skinMaterial;

        [SerializeField] private Vector3[] waypoints = System.Array.Empty<Vector3>();
        public float walkSpeed = 1.2f;
        [SerializeField] private float idleMin = 1f;
        [SerializeField] private float idleMax = 4f;

        private int _index;
        private float _idleTimer;
        private bool _idling;
        private Vector3 _target;

        private void Start()
        {
            Renderer bodyR = transform.Find("Body")?.GetComponent<Renderer>();
            if (bodyR != null && bodyMaterial != null)
            {
                bodyR.material = new Material(bodyMaterial);
                bodyR.material.color = bodyTint;
            }

            if (waypoints.Length > 0)
            {
                _target = waypoints[0];
                _index = 1;
            }
        }

        public void SetRoute(Vector3[] route)
        {
            waypoints = route;
            _index = 0;
            if (waypoints.Length > 0) _target = waypoints[0];
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            if (_idling)
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f) _idling = false;
                return;
            }

            Vector3 to = _target - transform.position;
            to.y = 0f;

            if (to.magnitude <= 0.2f)
            {
                _idling = true;
                _idleTimer = Random.Range(idleMin, idleMax);
                _target = waypoints[_index];
                _index = (_index + 1) % waypoints.Length;
                return;
            }

            transform.position += to.normalized * walkSpeed * Time.deltaTime;
            if (to.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }
        }
    }
}
