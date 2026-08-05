using UnityEngine;

namespace Rise.Systems
{
    public class WalkAnimation : MonoBehaviour
    {
        [SerializeField] private float swingAngleLegs = 30f;
        [SerializeField] private float swingAngleArms = 20f;
        [SerializeField] private float frequency = 5f;
        [SerializeField] private float lerpSpeed = 8f;

        private Transform _legPivotL;
        private Transform _legPivotR;
        private Transform _armPivotL;
        private Transform _armPivotR;
        private float _phase;
        private float _currentSpeed;
        private float _legLAngle;
        private float _legRAngle;
        private float _armLAngle;
        private float _armRAngle;

        public void SetSpeed(float speed) => _currentSpeed = speed;

        private void Start()
        {
            _legPivotL = FindPart("LegPivot_L");
            _legPivotR = FindPart("LegPivot_R");
            _armPivotL = FindPart("ArmPivot_L");
            _armPivotR = FindPart("ArmPivot_R");
        }

        private void Update()
        {
            if (_currentSpeed > 0.1f)
                _phase += _currentSpeed * Time.deltaTime * frequency;

            float t = lerpSpeed * Time.deltaTime;
            float legLTarget = (_currentSpeed > 0.1f) ? Mathf.Sin(_phase) * swingAngleLegs : 0f;
            float legRTarget = (_currentSpeed > 0.1f) ? Mathf.Sin(_phase + Mathf.PI) * swingAngleLegs : 0f;
            float armLTarget = (_currentSpeed > 0.1f) ? Mathf.Sin(_phase + Mathf.PI) * swingAngleArms : 0f;
            float armRTarget = (_currentSpeed > 0.1f) ? Mathf.Sin(_phase) * swingAngleArms : 0f;

            _legLAngle = Mathf.Lerp(_legLAngle, legLTarget, t);
            _legRAngle = Mathf.Lerp(_legRAngle, legRTarget, t);
            _armLAngle = Mathf.Lerp(_armLAngle, armLTarget, t);
            _armRAngle = Mathf.Lerp(_armRAngle, armRTarget, t);

            if (_legPivotL) _legPivotL.localRotation = Quaternion.Euler(_legLAngle, 0f, 0f);
            if (_legPivotR) _legPivotR.localRotation = Quaternion.Euler(_legRAngle, 0f, 0f);
            if (_armPivotL) _armPivotL.localRotation = Quaternion.Euler(_armLAngle, 0f, 0f);
            if (_armPivotR) _armPivotR.localRotation = Quaternion.Euler(_armRAngle, 0f, 0f);
        }

        private Transform FindPart(string partName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == partName) return t;
            }
            return null;
        }
    }
}
