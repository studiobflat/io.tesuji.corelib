using System;
using Tesuji;
using UnityEngine;

public class AvatarURL : MonoBehaviour
{
    public Texture2D defaultImage;
    public SkinnedMeshRenderer target;
    
    [SerializeField] private string _url;
    [NonSerialized] private Material _material;
    
    void Start()
    {
        if (_material == null) InitMaterial();
    }

    void InitMaterial()
    {
        _material = new Material(target.sharedMaterial);
        target.sharedMaterial = _material;
        if (!string.IsNullOrEmpty(_url)) Refresh();
    }

    [Button(ButtonMode.EnabledInPlayMode)] public void Refresh()
    {
        if (_material == null) InitMaterial();
        _material.mainTexture = defaultImage;
        if (string.IsNullOrEmpty(_url))
        {
            // Debug.LogWarning($"Image URL is null or empty : {_url}");
            return;
        }
        
        var imageURL = _url;
        TesujiImageLoader.Load(imageURL, (tex) =>
        {
            if (_url != imageURL) return; // change to a different URL
            _material.mainTexture = tex;
        });
    }
    
    public void SetURL(string url)
    {
        if (url == _url)
        {
            // Debug.LogWarning($"Same URL : {url}");
            return;
        }

        _url = url;
        Refresh();
    }
}