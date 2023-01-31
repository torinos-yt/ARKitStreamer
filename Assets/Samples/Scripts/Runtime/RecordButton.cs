using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecordButton : MonoBehaviour
{
    [SerializeField] Image _image;
    [SerializeField] ARKitStream.ARKitRecorder _recorder;

    private void Awake()
    {
        if (Application.isEditor)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Record()
    {
        _recorder.ToggleRecord();

        _image.transform.localScale = _recorder.IsRecording? Vector3.one*1.7f : Vector3.one;
    }

}
