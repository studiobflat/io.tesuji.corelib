using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationPlayer : MonoBehaviour
{
    [Serializable] public class AnimState
    {
        public string id;
        public AnimationClip[] clips;

        public AnimationClip GetRandom()
        {
            var length = clips.Length;
            return length switch
            {
                0 => null,
                1 => clips[0],
                _ => clips[Random.Range(0, 10000) % length]
            };
        }
    }
    
    [Range(1f, 5f)] public float blendSpeed = 1f;
    public List<AnimState> animations = new List<AnimState>();

    private PlayableGraph graph;
    private PlayableOutput output;
    private AnimationMixerPlayable mixer;
    
    private int _mixIndex;
    [SerializeField] private string _animId;
    private const int MAX_INPUT_COUNT = 3;
    
    private void Awake()
    {
        var animator = gameObject.GetComponent<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        
        graph = PlayableGraph.Create();
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
        mixer = AnimationMixerPlayable.Create(graph, MAX_INPUT_COUNT, true);
        mixer.SetInputCount(MAX_INPUT_COUNT);
        mixer.SetOutputCount(1);
        
        output = AnimationPlayableOutput.Create(graph, "AnimationPlayer", animator);
        output.SetSourcePlayable(mixer);
        graph.Play();
        
        _mixIndex = -1;
        if (!string.IsNullOrEmpty(_animId)) Play(_animId, true);
    }
    
    public void Play(string animId, bool force = false)
    {
        if (force == false && _animId == animId) // TODO : choose a different clip?
        {
            // Debug.LogWarning($"Same animId: {animId}");
            return;
        }
        
        AnimState state = animations.FirstOrDefault(s => s.id == animId);
        if (state == null)
        {
            Debug.LogWarning($"AnimState not found: {animId}");
            return;
        }

        AnimationClip clip = state.GetRandom();
        if (clip == null)
        {
            Debug.LogWarning("Clip is null!");
            return;
        }
        
        _animId = animId;
        
        var playable = AnimationClipPlayable.Create(graph, clip);
        var firstTime = _mixIndex == -1;
        
        _mixIndex = (_mixIndex +1) % MAX_INPUT_COUNT;
        mixer.DisconnectInput(_mixIndex);
        mixer.ConnectInput(_mixIndex, playable, 0, firstTime ? 1 : 0);
    }

    void Update()
    {
        for (var i = 0; i < MAX_INPUT_COUNT; i++)
        {
            var w = mixer.GetInputWeight(i);
            var d = (i == _mixIndex) ? 1f : -1f;
            mixer.SetInputWeight(i, Mathf.Clamp01(w + d * blendSpeed * Time.deltaTime));
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AnimationPlayer))]
public class AnimationPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ap = (AnimationPlayer) target;
        if (ap == null) return;
        if (!Application.isPlaying) return;
        
        List<AnimationPlayer.AnimState> anims = ap.animations;
        var animCount = anims.Count;
        if (animCount == 0) return;
        
        for (var i = 0; i < animCount; i++)
        {
            AnimationPlayer.AnimState anim = anims[i];
            if (GUILayout.Button(anim.id))
            {
                ap.Play(anim.id);
            }
        }
    }
}
#endif
