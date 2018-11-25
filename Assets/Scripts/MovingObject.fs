namespace Unity_2DRoguelike

open UnityEngine
open System.Collections
open System.Collections.Generic

[<AbstractClass>]
[<AllowNullLiteral>]
type MovingObject =
    inherit MonoBehaviour

    val mutable public _moveTime : float32

    [<DefaultValue>] val mutable public _blockingLayer : LayerMask

    [<DefaultValue>] val mutable private _boxCollider : BoxCollider2D
    [<DefaultValue>] val mutable private  _rb2d : Rigidbody2D
    [<DefaultValue>] val mutable private _inverseMoveTime : float32

    new () =
        {
            _moveTime = 0.1f
        }

    abstract member Start : unit -> unit

    default this.Start () =
        this._boxCollider <- this.GetComponent<BoxCollider2D>()
        this._rb2d <- this.GetComponent<Rigidbody2D>()

        this._inverseMoveTime <- 1.0f / this._moveTime

    member public this.Move (xDir : int, yDir : int) =

        let theStart = this.transform.position |> Vector2.op_Implicit
        let theEnd = theStart + new Vector2(float32 xDir, float32 yDir) 

        this._boxCollider.enabled <- false
        let hit = Physics2D.Linecast(theStart, theEnd, this._blockingLayer |> LayerMask.op_Implicit)
        this._boxCollider.enabled <- true

        if hit.transform = null
            then 
                this.StartCoroutine("SmoothMove", theEnd |> Vector2.op_Implicit) |> ignore
                true, hit
            else
                false, hit

    member public this.SmoothMove(theEnd : Vector3) =
        let s = seq{
            let mutable sqrRemainingDistance = (this.transform.position - theEnd).sqrMagnitude;

            while sqrRemainingDistance > System.Single.Epsilon do
                let newPosition = Vector3.MoveTowards(this._rb2d.position |> Vector2.op_Implicit, theEnd, this._inverseMoveTime * Time.deltaTime);
                this._rb2d.MovePosition(newPosition |> Vector2.op_Implicit);

                sqrRemainingDistance <- (this.transform.position - theEnd).sqrMagnitude

                yield null
        }

        s.GetEnumerator() :> IEnumerator

    abstract member AttemptMove<'a when 'a :> Component and 'a : equality and 'a : null> : int * int -> unit

    default this.AttemptMove<'a when 'a :> Component and 'a : equality and 'a : null>(xDir : int, yDir : int) =

        let can, hit = this.Move(xDir, yDir)

        if hit.transform = null 
            then ()
        else 
            let hitComponent = hit.transform.GetComponent<'a>();

            if can = false && hitComponent <> null 
                then this.OnCantMove(hitComponent)

    abstract member OnCantMove : Component -> unit