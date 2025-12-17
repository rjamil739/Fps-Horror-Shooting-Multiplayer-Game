using System;
using UnityEngine;
using UnityEngine.UI;

public class UIServerPicker : MonoBehaviour
{
    [SerializeField] Text _currentStateRenderer;
    [SerializeField] Button _showRegionsButton;
    [SerializeField] Button _closeMenuButton;
    [SerializeField] GameObject _regionButtonPrefab;
    [SerializeField] GameObject _menu;

    private void Start()
    {


        DNRegionManager regionManager = DNRegionManager.Sigleton;

        regionManager.OnRegionFound += OnRegionFound;

        if (regionManager == null)
            return;

        if (regionManager.SelectedRegion == null) 
        {
            regionManager.GetBestRegion(regionManager.SelectRegion);
            ShowMenu();
        }

        for (int i = 0; i < regionManager.Regions.Length-1; i++) 
        {
            UIServerPickerElement regionButton = Instantiate(_regionButtonPrefab, _regionButtonPrefab.transform.parent).
                GetComponent<UIServerPickerElement>();

            regionButton.Setup(this, regionManager.Regions[i], i);
        }

        UIServerPickerElement lastregionButton = _regionButtonPrefab.
            GetComponent<UIServerPickerElement>();

        lastregionButton.Setup(this, regionManager.Regions[regionManager.Regions.Length-1], regionManager.Regions.Length - 1);


        //after drawing ui hook events
        regionManager.OnRegionSelected += OnRegionSelected;

        OnRegionSelected(regionManager.SelectedRegion);

        _showRegionsButton.GetComponent<Button>().onClick.AddListener(ButtonShowMenu);
        _closeMenuButton.GetComponent<Button>().onClick.AddListener(CloseMenu);

        //this will place cancel button underneath every region button
        _closeMenuButton.transform.parent.SetAsLastSibling();
    }

    private void OnRegionFound(DNRegion arg0)
    {
        if(!_closeMenuButton.gameObject.activeSelf)
            _closeMenuButton.gameObject.SetActive(true);
    }

    public void ButtonShowMenu() 
    {
        ShowMenu();
        DNRegionManager.Sigleton.PingAllRegions();
    }

    public void ShowMenu() 
    {
        _closeMenuButton.gameObject.SetActive(false);
        _menu.SetActive(true);
    }

    public void CloseMenu() 
    {
        _menu.SetActive(false);
    }

    private void OnDestroy()
    {
        DNRegionManager regionManager = DNRegionManager.Sigleton;

        if (regionManager == null)
            return;

        regionManager.OnRegionSelected -= OnRegionSelected;
    }

    void OnRegionSelected(DNRegion region) 
    {
        if (region == null)
        {
            _currentStateRenderer.text = "Waiting for region...";
            return;
        }

        _menu.gameObject.SetActive(false);
        _currentStateRenderer.text = region.Name;
    }

    internal void OnServerStatusUpdated()
    {
        _closeMenuButton.transform.parent.SetAsLastSibling();
    }
}
