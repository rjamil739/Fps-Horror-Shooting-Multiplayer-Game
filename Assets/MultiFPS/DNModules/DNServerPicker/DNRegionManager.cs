using DNWebRequest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#if UNITY_EDITOR
[System.Serializable]
#endif
public class DNRegion
{
    public short Ping; //-1 stands for region being not available
    public string Url;
    public string Name;
    public Stopwatch _tripTimeStopwatch;

    public Action<short> OnStatusChanged;

    public DNRegion() 
    {
        _tripTimeStopwatch = new Stopwatch();
    }
    public void StartMeasuringTripTime() 
    {
        Ping = -1;
        OnStatusChanged?.Invoke(Ping);
        _tripTimeStopwatch.Start();
    }
    public void CalculateTripTime() 
    {
        _tripTimeStopwatch.Stop();
        double nanoSecs = _tripTimeStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
        Ping = (short)(nanoSecs);
        _tripTimeStopwatch.Reset();

        OnStatusChanged?.Invoke(Ping);
    }
}

public class DNRegionManager : MonoBehaviour
{
    public static DNRegionManager Sigleton;

    public DNRegion[] Regions { private set; get; }
    bool _lookingForBestRegion;

    /// <summary>
    /// returns region with based latency, if no region is returned, all servers are unreachable
    /// </summary>
    public UnityAction<DNRegion> OnRegionSelected;
    UnityAction<DNRegion> _onBestRegionFound;
    public UnityAction<DNRegion> OnRegionFound;
    int _regionsChecked;

    public DNRegion SelectedRegion { private set; get; }

    
    void Awake() 
    {
        RegionsContainer[] containers = Resources.LoadAll<RegionsContainer>("");

        List<RegionDefinition> regionsList = new();
        for (int i = 0; i < containers.Length; i++)
            regionsList.AddRange(containers[i].Regions);

        if (Sigleton) 
        {
            UnityEngine.Debug.LogError("There is already instance of RegionManager spawned!");
            return;
        }
        Sigleton = this;
        Regions = new DNRegion [regionsList.Count];

        //map regions definition to state holders, state holders contain additon info if regions is available for example, and its latency
        for (int i = 0; i < regionsList.Count; i++) 
        {
            RegionDefinition regionDefinition = regionsList[i];
            DNRegion region = new () 
            {
                Ping = -1,
                Name = regionDefinition.RegionName,
                Url = regionDefinition.Url,
            };
            Regions[i] = region;
        }
    }

    private void OnDestroy()
    {
        if (Sigleton == this)
            Sigleton = null;
    }

    public void GetBestRegion(UnityAction<DNRegion> bufferedOnRegionFound) 
    {
        _regionsChecked = 0;
        _onBestRegionFound = bufferedOnRegionFound;
        _lookingForBestRegion = true;

        PingAllRegions();
    }

    public void PingAllRegions() 
    {
        for (int i = 0; i < Regions.Length; i++)
            StartCoroutine(PingRegion(Regions[i]));
    }

    public void SelectRegion(DNRegion region) 
    {
        SelectedRegion = region;
        OnRegionSelected?.Invoke(SelectedRegion);
    }

    public void SelectRegion(int regionID)
    {
        SelectedRegion = Regions[regionID];
        OnRegionSelected?.Invoke(SelectedRegion);
    }

    IEnumerator PingRegion(DNRegion region) 
    {
        UnityWebRequest unityWebRequest = DNWebRequests.CreateWebRequest($"{region.Url}/matchmaking/ping", DNRequestType.GET);
        region.StartMeasuringTripTime();
        yield return unityWebRequest.SendWebRequest();

        if(_lookingForBestRegion)
            _regionsChecked++;

        if (unityWebRequest.result != UnityWebRequest.Result.Success)
        {
            if(_lookingForBestRegion && _regionsChecked >= Regions.Length)
                _onBestRegionFound(null);

            region.Ping = -1;
            yield break;
        }

        //calculating only if success
        region.CalculateTripTime();//(short)Mathf.RoundToInt((float)(Time.timeAsDouble - pingStart) * 1000);

        OnRegionFound?.Invoke(region);

        if (_lookingForBestRegion) 
        {
            _onBestRegionFound.Invoke(region);
            _onBestRegionFound = null;
            _lookingForBestRegion = false;
        }
    }
}
