using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Klak.Ndi;
using ARKitStream;

public class RecordController : MonoBehaviour
{
    [System.NonSerialized]
    public bool IsRecord = false;

    [System.NonSerialized]
    public string RecordTime = "";

    [SerializeField] Image _recordImage;

    public void Record()
    {
        IsRecord = !IsRecord;
        RecordTime = $"{DateTime.Now.Year}{DateTime.Now.Day.ToString("D2")}{DateTime.Now.Hour.ToString("D2")}{DateTime.Now.Minute.ToString("D2")}";

        _recordImage.transform.localScale = Vector3.one * (IsRecord? 1.8f : 1f);

        var sender = GameObject.Find(string.Format("ARKit Stream")).GetComponent<NdiSender>();
        sender?.gameObject.SetActive(!IsRecord);
    }
}
