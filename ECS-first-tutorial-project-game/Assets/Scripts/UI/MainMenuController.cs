using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMG.Survivors
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayButton);
            _quitButton.onClick.AddListener(OnQuitButton);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();
        }

        private void OnPlayButton()
        {
            SceneManager.LoadScene(1);
        }

        private void OnQuitButton()
        {
            Application.Quit();
        }
    }
}