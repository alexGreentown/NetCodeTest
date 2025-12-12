using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetCodeTest
{   
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _lobbySceneName = "Lobby";

        private void Start()
        {
            SceneManager.LoadScene(_lobbySceneName);
        } 
    } 
}
