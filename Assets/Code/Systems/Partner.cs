using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public class Partner : MonoBehaviour
    {
        [Header("Partner")]
        [SerializeField] private string partnerName = "Maya";
        [SerializeField] private float interactRadius = 3f;
        [SerializeField] private float talkAffection = 2f;
        [SerializeField] private float giftFlowerAffection = 15f;
        [SerializeField] private float giftChocolateAffection = 10f;
        [SerializeField] private float giftRingAffection = 40f;
        [SerializeField] private int daysUntilChild = 2;

        public Material skinMaterial;

        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _isOpen;
        private bool _playerInRange;
        private int _lastTalkDay = -1;
        private string _message = "";
        private float _messageTimer;

        public float Affection { get; private set; }
        public bool Married { get; private set; }
        public int MarriageDay { get; private set; }
        public bool ChildSpawned { get; private set; }

        public bool IsOpen => _isOpen;
        public bool IsPlayerInRange => _playerInRange;

        public string Stage
        {
            get
            {
                if (Married) return "Wife";
                if (Affection >= 80f) return "Girlfriend";
                if (Affection >= 40f) return "Friend";
                return "Stranger";
            }
        }

        public string StatusText => Married
            ? partnerName + " - Wife (married)"
            : partnerName + " - " + Stage + " " + Mathf.RoundToInt(Affection) + "/100";

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        public void ApplySaved(float affection, bool married, int marriageDay, bool childSpawned)
        {
            Affection = affection;
            Married = married;
            MarriageDay = marriageDay;
            ChildSpawned = childSpawned;
            if (ChildSpawned) BuildChild();
        }

        private void Update()
        {
            if (_gameManager == null || _player == null) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

            if (_isOpen && !_playerInRange)
            {
                Close();
                return;
            }

            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (ePressed && _playerInRange)
            {
                if (_isOpen) Close();
                else Open();
            }

            if (_isOpen) HandleGiftKeys();

            if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;

            TrySpawnChild();
        }

        private void Open()
        {
            _isOpen = true;
            _gameManager.ActiveShop = null;
            if (_lastTalkDay != _gameManager.Clock.Day)
            {
                _lastTalkDay = _gameManager.Clock.Day;
                AddAffection(talkAffection, "Talked with " + partnerName + ". (+" + talkAffection + ")");
            }
            else
            {
                _message = "You already talked today.";
                _messageTimer = 2.5f;
            }
        }

        private void Close()
        {
            _isOpen = false;
            _message = "";
        }

        private void HandleGiftKeys()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                GiveGift(ShopItemType.GiftFlower, "flowers", giftFlowerAffection);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                GiveGift(ShopItemType.GiftChocolate, "chocolate", giftChocolateAffection);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                GiveGift(ShopItemType.GiftRing, "a ring", giftRingAffection);
        }

        private void GiveGift(ShopItemType type, string giftName, float value)
        {
            if (_gameManager.Needs == null || !_gameManager.Needs.TryConsumeGift(type))
            {
                SetMessage("You don't have " + giftName + " to give.");
                return;
            }
            AddAffection(value, "Gave " + partnerName + " " + giftName + "! (+" + value + ")");
            _gameManager.Rep?.AddReputation(3);
        }

        private void AddAffection(float amount, string message)
        {
            Affection = Mathf.Min(100f, Affection + amount);
            SetMessage(message);
            if (!Married && Affection >= 100f)
            {
                Married = true;
                MarriageDay = _gameManager.Clock.Day;
                _gameManager.Rep?.AddReputation(10);
                _message = "You proposed and " + partnerName + " said YES! You are married!";
            }
        }

        private void TrySpawnChild()
        {
            if (!Married || ChildSpawned) return;
            if (_gameManager.Clock.Day < MarriageDay + daysUntilChild) return;
            ChildSpawned = true;
            BuildChild();
            _message = "Your child was born!";
            _messageTimer = 4f;
        }

        private void BuildChild()
        {
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            child.name = "Child";
            child.transform.position = transform.position + new Vector3(2f, 0f, 0f);
            child.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            Object.Destroy(child.GetComponent<Collider>());
            Renderer r = child.GetComponent<Renderer>();
            if (r != null)
            {
                if (skinMaterial != null) r.sharedMaterial = skinMaterial;
                else
                {
                    Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.color = new Color(0.93f, 0.82f, 0.72f);
                    r.sharedMaterial = m;
                }
            }
        }

        private void SetMessage(string text)
        {
            _message = text;
            _messageTimer = 3f;
        }

        public string GetMenuText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- " + partnerName + " --");
            sb.AppendLine(Stage + "  " + Mathf.RoundToInt(Affection) + "/100");
            sb.AppendLine("Gifts you carry:");
            sb.AppendLine("  1 Flowers x" + (_gameManager.Needs != null ? _gameManager.Needs.GiftFlowers : 0));
            sb.AppendLine("  2 Chocolate x" + (_gameManager.Needs != null ? _gameManager.Needs.GiftChocolate : 0));
            sb.AppendLine("  3 Ring x" + (_gameManager.Needs != null ? _gameManager.Needs.GiftRings : 0));
            sb.Append("Give with 1-3, E to close");
            if (_messageTimer > 0f)
            {
                sb.AppendLine();
                sb.Append(_message);
            }
            return sb.ToString();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.7f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
