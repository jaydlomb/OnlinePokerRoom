using UnityEngine;
using TMPro;

namespace Poker.UI
{
    public class PlayerSlotUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text chipCountText;
        [SerializeField] private GameObject activeTurnIndicator;
        [SerializeField] private GameObject foldedOverlay;

        public string AssignedUserID { get; private set; }

        public void Assign(string userID, string username, int chips)
        {
            AssignedUserID = userID;
            usernameText.text = username;
            chipCountText.text = $"{chips} chips";
            SetFolded(false);
            SetActiveTurn(false);
        }

        public void UpdateChips(int chips)
        {
            chipCountText.text = $"{chips} chips";
        }

        public void SetActiveTurn(bool isActive)
        {
            if (activeTurnIndicator != null)
                activeTurnIndicator.SetActive(isActive);
        }

        public void SetFolded(bool folded)
        {
            if (foldedOverlay != null)
                foldedOverlay.SetActive(folded);
        }

        public void Clear()
        {
            AssignedUserID = null;
            usernameText.text = "Empty";
            chipCountText.text = "";
            SetActiveTurn(false);
            SetFolded(false);
        }
    }
}