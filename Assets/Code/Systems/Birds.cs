using UnityEngine;

namespace Rise.Systems
{
    public class Birds : MonoBehaviour
    {
        public Material material;

        [SerializeField] private int birdCount = 6;
        [SerializeField] private float radiusMin = 12f;
        [SerializeField] private float radiusMax = 28f;
        [SerializeField] private float altitudeMin = 6f;
        [SerializeField] private float altitudeMax = 12f;

        private Transform[] _birds;
        private Transform[] _leftWing;
        private Transform[] _rightWing;
        private float[] _angle;
        private float[] _radius;
        private float[] _speed;
        private float[] _altitude;
        private float[] _phase;

        private void Start()
        {
            if (material == null)
            {
                Material fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                fallback.color = new Color(0.09f, 0.09f, 0.12f);
                material = fallback;
            }

            _birds = new Transform[birdCount];
            _leftWing = new Transform[birdCount];
            _rightWing = new Transform[birdCount];
            _angle = new float[birdCount];
            _radius = new float[birdCount];
            _speed = new float[birdCount];
            _altitude = new float[birdCount];
            _phase = new float[birdCount];

            for (int i = 0; i < birdCount; i++)
            {
                GameObject bird = new GameObject("Bird_" + i);
                bird.transform.SetParent(transform);
                BuildBody(bird.transform, i);
                _birds[i] = bird.transform;
                _radius[i] = Random.Range(radiusMin, radiusMax);
                _speed[i] = Random.Range(0.12f, 0.3f);
                _altitude[i] = Random.Range(altitudeMin, altitudeMax);
                _angle[i] = Random.Range(0f, 360f);
                _phase[i] = Random.Range(0f, 10f);
            }
        }

        private void BuildBody(Transform bird, int index)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(bird);
            body.transform.localScale = new Vector3(0.18f, 0.16f, 0.4f);
            Object.Destroy(body.GetComponent<Collider>());
            SetRenderer(body, material);

            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "LeftWing";
            left.transform.SetParent(bird);
            left.transform.localPosition = new Vector3(-0.32f, 0.06f, 0f);
            left.transform.localScale = new Vector3(0.55f, 0.03f, 0.45f);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            Object.Destroy(left.GetComponent<Collider>());
            SetRenderer(left, material);
            _leftWing[index] = left.transform;

            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "RightWing";
            right.transform.SetParent(bird);
            right.transform.localPosition = new Vector3(0.32f, 0.06f, 0f);
            right.transform.localScale = new Vector3(0.55f, 0.03f, 0.45f);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Object.Destroy(right.GetComponent<Collider>());
            SetRenderer(right, material);
            _rightWing[index] = right.transform;
        }

        private static void SetRenderer(GameObject go, Material mat)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private void Update()
        {
            if (_birds == null) return;

            for (int i = 0; i < _birds.Length; i++)
            {
                _angle[i] += _speed[i] * Time.deltaTime;
                float bob = Mathf.Sin(Time.time * 1.5f + _phase[i]) * 0.5f;
                Vector3 pos = transform.position + new Vector3(
                    Mathf.Cos(_angle[i]) * _radius[i], bob, Mathf.Sin(_angle[i]) * _radius[i]);
                _birds[i].position = pos;
                _birds[i].rotation = Quaternion.LookRotation(
                    new Vector3(-Mathf.Sin(_angle[i]), 0f, Mathf.Cos(_angle[i])));

                float flap = Mathf.Sin(Time.time * 8f + _phase[i]) * 25f;
                _leftWing[i].localRotation = Quaternion.Euler(0f, 0f, 22f + flap);
                _rightWing[i].localRotation = Quaternion.Euler(0f, 0f, -22f - flap);
            }
        }
    }
}
