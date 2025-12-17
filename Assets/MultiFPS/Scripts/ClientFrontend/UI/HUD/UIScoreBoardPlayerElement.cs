using MultiFPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS.UI.HUD
{
    public class UIScoreBoardPlayerElement : MonoBehaviour
    {
        [SerializeField] Text _playerName;
        [SerializeField] Text _kills;
        [SerializeField] Text _deaths;
        [SerializeField] Text _assists;
        [SerializeField] Text _latency;
        [SerializeField] Image _background;
        [SerializeField] Color _localPlayerColor = Color.yellow;

        public void WriteData(PlayerInstance player)
        {
            _playerName.text = player.PlayerInfo.Username;
            _kills.text = player.Kills.ToString();
            _deaths.text = player.Deaths.ToString();
            _assists.text = player.Assists.ToString();
            //assign appropriate color for player in scoreboard depending on team, if player is not in any team, give him white color
            Color teamColor = player.Team == -1 ? Color.white : ClientInterfaceManager.Instance.UIColorSet.TeamColors[player.Team];

            _playerName.color = teamColor;
            _kills.color = teamColor;
            _deaths.color = teamColor;
            _assists.color = teamColor;

            _latency.text = player.BOT ? "BOT" : player.Latency.ToString();

            if (player.Team != -1 && player == ClientFrontend.ClientPlayerInstance)
            {
                Color color = ClientInterfaceManager.Instance.UIColorSet.TeamColors[player.Team];
                _background.color = new Color(color.r, color.g, color.b, 0.1f);
            }
        }
    }
}