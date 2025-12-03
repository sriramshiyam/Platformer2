using System.Collections.Generic;
using Godot;

namespace Platformer2;

public partial class SoundManager : Node
{
    public static SoundManager I { get; private set; }
    public string ATTACK_SOUND { get; private set; }
    public string ARROW_ATTACK_SOUND { get; private set; }
    public string JUMP_SOUND { get; private set; }
    Dictionary<string, AudioStream> SOUNDS;

    public override void _Ready()
    {
        I = this;

        ATTACK_SOUND = "attack";
        ARROW_ATTACK_SOUND = "arrow_attack";
        JUMP_SOUND = "jump";

        SOUNDS = new Dictionary<string, AudioStream>()
        {
            {ATTACK_SOUND, ResourceLoader.Load<AudioStream>("res://assets/sound/player/attack.wav")},
            {ARROW_ATTACK_SOUND, ResourceLoader.Load<AudioStream>("res://assets/sound/player/arrow_attack.wav")},
            {JUMP_SOUND, ResourceLoader.Load<AudioStream>("res://assets/sound/player/jump.wav")}
        };
    }

    public void PlaySound(AudioStreamPlayer2D player, string key)
    {
        if (SOUNDS.TryGetValue(key, out AudioStream value))
        {
            player.Stop();
            player.Stream = value;
            player.Play();
        }
    }

    public void PlaySound(AudioStreamPlayer player, string key)
    {
        if (SOUNDS.TryGetValue(key, out AudioStream value))
        {
            player.Stop();
            player.Stream = value;
            player.Play();
        }
    }
}