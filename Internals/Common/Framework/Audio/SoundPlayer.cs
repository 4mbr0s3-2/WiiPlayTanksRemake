using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public static class SoundPlayer
    {

        public static Dictionary<string, OggAudio> SavedSounds = new();
        public static TimeSpan GetLengthOfSound(string filePath)
        {
            byte[] soundData = File.ReadAllBytes(filePath);
            return SoundEffect.GetSampleDuration(soundData.Length, 44100, AudioChannels.Stereo);
        }
        private static float MusicVolume => TankGame.Settings.MusicVolume;
        private static float EffectsVolume => TankGame.Settings.EffectsVolume;
        private static float AmbientVolume => TankGame.Settings.AmbientVolume;
        /// <summary>
        ///     Loads a sound from a file and plays it.
        /// </summary>
        /// <param name="audioPath">
        ///     The path to the audio file.
        /// </param>
        /// <param name="context">
        ///     The type of sound as defined in <see cref="SoundContext"/>.
        /// </param>
        /// <param name="volume">
        ///     The volume the audio will be played as.
        ///     Maximum value is 1. Minimum value is 0.
        /// </param>
        /// <param name="panOverride">
        ///     An override to the sounds panning.
        ///     Maximum value is 1. Minimum value is -1.
        /// </param>
        /// <param name="pitchOverride">
        ///     An override to the sounds pitch.
        ///     Maximum value is 1. Minimum value is -1.
        /// </param>
        /// <param name="gameplaySound">
        ///     Is this sound related to Gameplay?
        /// </param>
        /// <param name="rememberMe">
        ///     Should the sound be kept in memory even if its not used anymore?
        /// </param>
        /// <remarks>
        ///     This method is <b>ONLY</b> able to play <b>.ogg</b> files using the <b>Vorbis</b> codec.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">This exceptions occurs when the given <see cref="SoundContext"/> value is not supported.</exception>
        /// <returns>
        ///     An <see cref="OggAudio"/> instance that can be used to play the sound.
        /// </returns>
        public static OggAudio PlaySoundInstance(string audioPath, SoundContext context, float volume = 1f, float panOverride = 0f, float pitchOverride = 0f, bool gameplaySound = false, bool rememberMe = false) {
            // because ogg is the only good audio format.
            audioPath = Path.Combine(TankGame.Instance.Content.RootDirectory, audioPath);

            switch (context)
            {
                case SoundContext.Music:
                    volume *= MusicVolume;
                    break;
                case SoundContext.Effect:
                    volume *= EffectsVolume;
                    if (gameplaySound && MainMenu.Active)
                        volume *= 0.25f;
                    if (SteamworksUtils.IsOverlayActive)
                        volume *= 0.5f;
                    break;
                case SoundContext.Ambient:
                    volume *= AmbientVolume;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context), context, "Uh oh! Seems like a new sound type was implemented, but I was not given a way to handle it!");
            }

            OggAudio sfx;

            // Verify that the sound exists and we are told to remember it, if so, load the sound from the dictionary.
            // And proceed to cache it.
            var existsInDictionary = SavedSounds.ContainsKey(audioPath) && rememberMe;
            sfx = existsInDictionary ? SavedSounds[audioPath] : new OggAudio(audioPath);
            if (!existsInDictionary && rememberMe)
                SavedSounds.Add(audioPath, sfx);
            sfx.Instance.Pan = MathHelper.Clamp(panOverride, -1f, 1f);
            sfx.Instance.Pitch = MathHelper.Clamp(pitchOverride, -1f, 1f);
            sfx.Instance.Play();
            sfx.Instance.Volume = MathHelper.Clamp(volume, 0f, 1f);

            //GameContent.Systems.ChatSystem.SendMessage($"{nameof(exists)}: {exists}", Color.White);
            //GameContent.Systems.ChatSystem.SendMessage($"new list count: {Sounds.Count}", Color.White);

            /*if (exists)
            {
                var sound = Sounds[Sounds.FindIndex(p => p.Name == soundDef.Name)];// = soundDef;

                if (sound.Sound.IsPlaying())
                    sound.Sound.Instance.Stop();
                sound.Sound.Instance.Play();
                sound.Sound.Instance.Volume = volume;

                Sounds[Sounds.FindIndex(p => p.Name == soundDef.Name)] = soundDef;
            }
            else
            {
                soundDef.Sound.Instance.Volume = volume;
                soundDef.Sound.Instance?.Play();
                Sounds.Add(soundDef);
            }*/

            return sfx;
        }
        /// <summary>
        ///     Play a sound at a given position in a world.
        /// </summary>
        /// <param name="fromSound">
        ///     The <see cref="SoundEffect"/> containing information on the sound to play.
        /// </param>
        /// <param name="context">
        ///     The type of sound as defined in <see cref="SoundContext"/>.
        /// </param>
        /// <param name="position">
        ///     The position of the sound in the world
        /// </param>
        /// <param name="world">
        ///     The world itself.
        /// </param>
        /// <param name="volume">
        ///     The volume of the sound.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">This exceptions occurs when the given <see cref="SoundContext"/> value is not supported.</exception>
        /// <returns>An instance used to play the sound.</returns>
        public static SoundEffectInstance PlaySoundInstance(SoundEffect fromSound, SoundContext context, Vector3 position, Matrix world, float volume = 1f)
        {
            switch (context)
            {
                case SoundContext.Music:
                    volume *= MusicVolume;
                    break;
                case SoundContext.Effect:
                    volume *= EffectsVolume;
                    break;
                case SoundContext.Ambient:
                    volume *= AmbientVolume;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context), context, "Uh oh! Seems like a new sound type was implemented, but I was not given a way to handle it!");
            }

            var pos2d = MatrixUtils.ConvertWorldToScreen(position, world, TankGame.GameView, TankGame.GameProjection);
            var WindowWidthHalfed = WindowUtils.WindowWidth / 2;
            var lerp = MathUtils.ModifiedInverseLerp(-WindowWidthHalfed, WindowUtils.WindowWidth + WindowWidthHalfed, pos2d.X, true);

            var sfx = fromSound.CreateInstance();
            sfx.Volume = volume;

            // System.Diagnostics.Debug.WriteLine(sfx.Pan);
            sfx.Play();
            sfx.Pan = lerp;

            return sfx;
        }
        /// <summary>
        /// Play an error sound effect
        /// </summary>
        /// <returns>An <see cref="OggAudio"/> representing the sound effect used for errors.</returns>
        public static OggAudio SoundError() => PlaySoundInstance("Assets/sounds/menu/menu_error.ogg", SoundContext.Effect, rememberMe: true);
    }
    public enum SoundContext : byte
    {
        Music,
        Effect,
        Ambient
    }
}