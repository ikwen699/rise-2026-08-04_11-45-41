using System;
using UnityEngine;

namespace Rise.Systems
{
    public class PlayerAppearance : MonoBehaviour
    {
        [Serializable]
        public class Outfit
        {
            public string outfitName;
            public Color shirtColor;
            public Color pantsColor;
            public int price;
            public int minReputation;
        }

        private Renderer _torsoRenderer;
        private Renderer _leftArmRenderer;
        private Renderer _rightArmRenderer;
        private Renderer _leftLegRenderer;
        private Renderer _rightLegRenderer;

        private Material _torsoMat;
        private Material _leftArmMat;
        private Material _rightArmMat;
        private Material _leftLegMat;
        private Material _rightLegMat;

        public int CurrentOutfitIndex { get; private set; }
        public int OutfitCount => Outfits.Length;

        public static readonly Outfit[] Outfits =
        {
            new Outfit { outfitName = "Streetwear", shirtColor = new Color(0.25f, 0.45f, 0.85f), pantsColor = new Color(0.2f, 0.2f, 0.25f), price = 0, minReputation = 0 },
            new Outfit { outfitName = "Classic Tee", shirtColor = new Color(0.85f, 0.40f, 0.45f), pantsColor = new Color(0.3f, 0.3f, 0.35f), price = 25, minReputation = 0 },
            new Outfit { outfitName = "Casual Denim", shirtColor = new Color(0.35f, 0.55f, 0.80f), pantsColor = new Color(0.25f, 0.35f, 0.55f), price = 40, minReputation = 0 },
            new Outfit { outfitName = "Fresh Green", shirtColor = new Color(0.40f, 0.70f, 0.45f), pantsColor = new Color(0.25f, 0.25f, 0.20f), price = 50, minReputation = 0 },
            new Outfit { outfitName = "Sunset Orange", shirtColor = new Color(0.90f, 0.55f, 0.20f), pantsColor = new Color(0.35f, 0.25f, 0.18f), price = 60, minReputation = 0 },
            new Outfit { outfitName = "Urban Black", shirtColor = new Color(0.12f, 0.12f, 0.14f), pantsColor = new Color(0.15f, 0.15f, 0.18f), price = 75, minReputation = 0 },
            new Outfit { outfitName = "Royal Purple", shirtColor = new Color(0.55f, 0.20f, 0.70f), pantsColor = new Color(0.20f, 0.15f, 0.30f), price = 80, minReputation = 0 },
            new Outfit { outfitName = "Designer Suit", shirtColor = new Color(0.15f, 0.15f, 0.18f), pantsColor = new Color(0.10f, 0.10f, 0.12f), price = 200, minReputation = 50 },
            new Outfit { outfitName = "Merci Couture", shirtColor = new Color(0.85f, 0.82f, 0.78f), pantsColor = new Color(0.18f, 0.18f, 0.22f), price = 350, minReputation = 80 },
            new Outfit { outfitName = "Elite Gold", shirtColor = new Color(0.85f, 0.72f, 0.30f), pantsColor = new Color(0.20f, 0.18f, 0.10f), price = 500, minReputation = 100 }
        };

        public void Init()
        {
            FindRenderers();
            if (Outfits.Length > 0) ApplyOutfit(CurrentOutfitIndex);
        }

        public void ApplySaved(int outfitIndex)
        {
            if (outfitIndex >= 0 && outfitIndex < Outfits.Length)
                CurrentOutfitIndex = outfitIndex;
            FindRenderers();
            ApplyOutfit(CurrentOutfitIndex);
        }

        public bool TryBuyOutfit(int index)
        {
            if (index < 0 || index >= Outfits.Length) return false;
            if (index == CurrentOutfitIndex) return false;
            CurrentOutfitIndex = index;
            ApplyOutfit(index);
            return true;
        }

        public void ApplyOutfit(int index)
        {
            if (index < 0 || index >= Outfits.Length) return;
            CurrentOutfitIndex = index;
            Outfit outfit = Outfits[index];
            SetColor(_torsoMat, outfit.shirtColor);
            SetColor(_leftArmMat, outfit.shirtColor);
            SetColor(_rightArmMat, outfit.shirtColor);
            SetColor(_leftLegMat, outfit.pantsColor);
            SetColor(_rightLegMat, outfit.pantsColor);
        }

        private void FindRenderers()
        {
            Transform body = transform.Find("Body");
            if (body == null) body = transform.Find("Humanoid/Body_Torso");
            if (body == null) body = transform.Find("Humanoid")?.Find("Body_Torso");

            Transform leftArm = FindChildRecursive("Arm_L");
            Transform rightArm = FindChildRecursive("Arm_R");
            Transform leftLeg = FindChildRecursive("Leg_L");
            Transform rightLeg = FindChildRecursive("Leg_R");
            Transform torso = FindChildRecursive("Body_Torso");

            if (torso != null) { _torsoRenderer = torso.GetComponent<Renderer>(); _torsoMat = CreateInstanceMat(_torsoRenderer); }
            if (leftArm != null) { _leftArmRenderer = leftArm.GetComponent<Renderer>(); _leftArmMat = CreateInstanceMat(_leftArmRenderer); }
            if (rightArm != null) { _rightArmRenderer = rightArm.GetComponent<Renderer>(); _rightArmMat = CreateInstanceMat(_rightArmRenderer); }
            if (leftLeg != null) { _leftLegRenderer = leftLeg.GetComponent<Renderer>(); _leftLegMat = CreateInstanceMat(_leftLegRenderer); }
            if (rightLeg != null) { _rightLegRenderer = rightLeg.GetComponent<Renderer>(); _rightLegMat = CreateInstanceMat(_rightLegRenderer); }
        }

        private Transform FindChildRecursive(string name)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in children)
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private static Material CreateInstanceMat(Renderer renderer)
        {
            if (renderer == null) return null;
            Material mat = new Material(renderer.sharedMaterial);
            renderer.material = mat;
            return mat;
        }

        private static void SetColor(Material mat, Color color)
        {
            if (mat != null) mat.color = color;
        }
    }
}
