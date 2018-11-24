namespace Unity_2DRoguelike

open UnityEngine

type Wall =
    inherit MonoBehaviour

    val mutable public _hp : int
    val mutable public _dmgSprite : Sprite    
    val mutable public _chopSound1 : AudioClip 
    val mutable public _chopSound2 : AudioClip

    [<DefaultValue>] val mutable private _spriteRenderer : SpriteRenderer
    
    new () = 
        {
            _hp = 4;
            _dmgSprite = null;
            _chopSound1 = null;
            _chopSound2 = null;
        }
        
    member private this.Awake() =
        this._spriteRenderer <- this.GetComponent<SpriteRenderer>()

    member public this.DamageWall (loss : int) =    
        this._spriteRenderer.sprite <- this._dmgSprite
        this._hp <- this._hp - loss

        SoundManager.Instance.RandomizeSfx(this._chopSound1, this._chopSound2);

        if (this._hp < 1)
            then this.gameObject.SetActive(false)