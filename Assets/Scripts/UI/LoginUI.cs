using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Poker.Networking;

namespace Poker.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField usernameField;
        [SerializeField] private TMP_InputField passwordField;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button signUpButton;
        [SerializeField] private TMP_Text errorText;

        private void Start()
        {
            loginButton.onClick.AddListener(OnLogin);
            signUpButton.onClick.AddListener(OnSignUp);
            errorText.text = "";
        }

        private void OnLogin()
        {
            StartCoroutine(AuthManager.Instance.Login(
                usernameField.text,
                passwordField.text,
                (success, error) =>
                {
                    if (success)
                    {
                        Debug.Log("Logged in!");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Matchmaking");
                    }
                    else
                    {
                        errorText.text = error;
                    }
                }
            ));
        }

        private void OnSignUp()
        {
            StartCoroutine(AuthManager.Instance.Register(
                usernameField.text,
                passwordField.text,
                (success, error) =>
                {
                    if (success)
                        errorText.text = "Account created! Please log in.";
                    else
                        errorText.text = error;
                }
            ));
        }
    }
}