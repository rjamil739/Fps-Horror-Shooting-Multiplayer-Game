using System;
using UnityEngine;
using UnityEngine.UI;

public class UIServerPickerElement : MonoBehaviour
{
    [SerializeField] Text _regionNameRenderer;
    [SerializeField] Text _latencyRenderer;

    UIServerPicker _serverPicker;
    DNRegion _myRegion;
    int _regionIndex;

    public void Setup(UIServerPicker serverPicker, DNRegion region, int regionID) 
    {
        GetComponent<Button>().onClick.AddListener(OnClick);

        _regionIndex = regionID;

        _serverPicker = serverPicker;
        _regionNameRenderer.text = region.Name;

        _myRegion = region;
        _myRegion.OnStatusChanged += UpdateStatus;

        UpdateStatus(region.Ping);
    }

    private void OnClick()
    {
        DNRegionManager.Sigleton.SelectRegion(_regionIndex);
        _serverPicker.CloseMenu();
    }

    private void OnDestroy()
    {
        if(_myRegion != null)
            _myRegion.OnStatusChanged -= UpdateStatus;
    }

    void UpdateStatus(short ping) 
    {
        transform.SetAsLastSibling();
        _serverPicker.OnServerStatusUpdated();

        gameObject.SetActive(ping != -1);

        if (ping > -1)
        {
            _latencyRenderer.text = ping.ToString();
            _latencyRenderer.color =
                ping < 70 ? Color.green :
                ping < 120 ? Color.yellow :
                Color.red;
        }
    }
}
