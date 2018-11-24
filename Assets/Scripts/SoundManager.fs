namespace Unity_2DRoguelike

open UnityEngine
open System

type SoundManager =
    inherit MonoBehaviour
    
    val mutable public _lowPitchRange : float32
    val mutable public _highPitchRange : float32
    val mutable public _efxSource : AudioSource
    val mutable public _musicSource : AudioSource

    [<DefaultValue>] static val mutable private _soundManager : SoundManager

    private new () =
        {
            _lowPitchRange = 0.95f;
            _highPitchRange = 1.05f;
            _efxSource = null;
            _musicSource = null;
        }

    static member Instance
        with get () = SoundManager._soundManager
        and set (value) = SoundManager._soundManager <- value

    member private this.Awake() =
        if Object.ReferenceEquals(SoundManager.Instance, null)
            then SoundManager.Instance <- this
        else if  SoundManager.Instance <> this
            then Object.Destroy(this.gameObject)

        Object.DontDestroyOnLoad(this.gameObject)

    member public this.PlaySingl(clip : AudioClip) =
        this._efxSource.clip <- clip
        this._efxSource.Play()

    member public this.RandomizeSfx([<ParamArray>] clips : AudioClip[]) =
        let randomIndex = Random.Range(0, clips.Length)
        let randomPitch = Random.Range(this._lowPitchRange, this._highPitchRange)

        this._efxSource.pitch <- randomPitch
        this._efxSource.clip <- clips.[randomIndex]
        this._efxSource.Play()