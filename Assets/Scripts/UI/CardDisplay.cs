using UnityEngine;
using UnityEngine.UI;

namespace Poker.UI
{
    public class CardDisplay : MonoBehaviour
    {
        [SerializeField] private Image cardImage;
        private Sprite _faceDownSprite;

        private void Awake()
        {
            _faceDownSprite = Resources.Load<Sprite>("Cards/card_back");
        }

        public void SetCard(int rank, string suit)
        {
            string rankName = GetRankName(rank);
            string suitLower = suit.ToLower();
            string suitFolder = char.ToUpper(suitLower[0]) + suitLower.Substring(1).TrimEnd('s');
            string path = $"Cards/{suitFolder}/{rankName}_of_{suitLower}";
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
                cardImage.sprite = sprite;
            else
                Debug.LogError($"Card sprite not found: {path}");
        }

        public void SetFaceDown()
        {
            if (_faceDownSprite != null)
                cardImage.sprite = _faceDownSprite;
        }

        private string GetRankName(int rank)
        {
            return rank switch
            {
                1 => "ace",
                11 => "jack",
                12 => "queen",
                13 => "king",
                14 => "ace",
                _ => rank.ToString()
            };
        }
    }
}