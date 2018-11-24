namespace Unity_2DRoguelike

open System
open System.Collections.Generic
open UnityEngine

[<Serializable>]
type Count =

    val mutable public _min : int
    val mutable public _max : int

    new (min : int, max : int) =
        {
            _min = min
            _max = max
        }

type BoardManager =
    inherit MonoBehaviour 

    val mutable public _columns : int
    val mutable public _rows : int
    val mutable public _wallCount : Count
    val mutable public _foodCount : Count
    
    [<DefaultValue>] val mutable public _exit : GameObject
    [<DefaultValue>] val mutable public _floorTiles : GameObject[]
    [<DefaultValue>] val mutable public _wallTiles : GameObject[]
    [<DefaultValue>] val mutable public _foodTiles : GameObject[]
    [<DefaultValue>] val mutable public _enemyTiles : GameObject[]
    [<DefaultValue>] val mutable public _outerWallTiles : GameObject[]

    [<DefaultValue>] val mutable private _boardHolder : Transform
    val mutable private _gridPositions : List<Vector3>

    new () =
        {
            _columns = 8
            _rows = 8
            _wallCount = new Count(5, 9 )
            _foodCount = new Count(1, 5)

            _gridPositions = new List<Vector3>()
        }

    member private this.InitializeList() =
        this._gridPositions.Clear()

        for x in 1 .. this._columns - 2 do 
            for y in 1 .. this._rows - 2 do
                this._gridPositions.Add(new Vector3((float32 x), (float32 y), 0.0f))

    member private this.BoardSetup() =
        let board = new GameObject("Board")
        this._boardHolder <- board.transform

        for x in -1 .. this._columns do 
            for y in -1 .. this._rows do
                let mutable toInstantiate = this._floorTiles.[UnityEngine.Random.Range(0, this._floorTiles.Length)]

                if x = -1 || x = this._columns || y = -1 || y = this._rows
                    then toInstantiate <- this._outerWallTiles.[UnityEngine.Random.Range(0, this._outerWallTiles.Length)];

                let instance = Object.Instantiate<GameObject>(toInstantiate, new Vector3((float32 x),(float32 y), 0.0f), Quaternion.identity);
                instance.transform.SetParent(this._boardHolder)

    member private this.RandomPosition() =
        let randomIndex = UnityEngine.Random.Range(0, this._gridPositions.Count)
        let randomPostion = this._gridPositions.[randomIndex]
        this._gridPositions.Remove(randomPostion) |> ignore

        randomPostion

    member private this.LayoutObjectAtRandom(tileArray : GameObject[], minimum : int, maximum : int) =
        let objectCount = UnityEngine.Random.Range(minimum, maximum + 1)
        
        for i in 0 .. objectCount do
            let randomPostion = this.RandomPosition()
            let tileChoice = tileArray.[UnityEngine.Random.Range(0, tileArray.Length)]
            Object.Instantiate(tileChoice, randomPostion, Quaternion.identity) |> ignore

    member public this.SetupScene(level : int) = 
        this.BoardSetup()
        this.InitializeList()
        this.LayoutObjectAtRandom(this._wallTiles, this._wallCount._min, this._wallCount._max)
        this.LayoutObjectAtRandom(this._foodTiles, this._foodCount._min, this._foodCount._max)

        let enemyCount = Mathf.Log(float32 level, 2.0f)
        this.LayoutObjectAtRandom(this._enemyTiles, (int enemyCount), (int enemyCount))
        let position = new Vector3(this._columns - 1 |> float32, this._rows - 1 |> float32, 0.0f)
        Object.Instantiate<GameObject>(this._exit, position, Quaternion.identity) |> ignore