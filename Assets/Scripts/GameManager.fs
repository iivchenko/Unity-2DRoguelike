namespace Unity_2DRoguelike

open System.Collections
open System.Collections.Generic
open UnityEngine
open UnityEngine.Events
open UnityEngine.SceneManagement
open UnityEngine.UI

type Loader() =
    inherit MonoBehaviour()

    [<DefaultValue>] val mutable public _gameManager : GameObject

    member private this.Awake() =
        if GameManager.Instance = null
            then Object.Instantiate(this._gameManager) |> ignore   

and [<AllowNullLiteral>] GameManager =
    inherit MonoBehaviour

    [<DefaultValue>] static val mutable private _gameManager : GameManager

    [<DefaultValue>] val mutable public _boardScript : BoardManager

    val mutable public _levelStartDelay : float32
    val mutable public _turnDelay : float32    
    val mutable public _playerFoodPoints : int

    [<HideInInspector>] 
    val mutable public _playersTurn : bool

    [<DefaultValueAttribute>] val mutable private _levelText : Text
    [<DefaultValueAttribute>] val mutable private _levelImage : GameObject    
    [<DefaultValueAttribute>] val mutable private _enemies : List<Enemy>
    [<DefaultValueAttribute>] val mutable private _enemiesMoving : bool
    [<DefaultValueAttribute>] val mutable private _doingSetup : bool

    val mutable private _level : int

    private new () = 
        {
            _levelStartDelay = 2.0f
            _turnDelay = 0.1f
            _playerFoodPoints = 100
            _playersTurn = true

            _level = 1
        }

    static member Instance
        with get () = GameManager._gameManager
        and set (value) = GameManager._gameManager <- value
    
    member private this.Awake() =
        if Object.ReferenceEquals(GameManager.Instance, null)
            then GameManager.Instance <- this
        else if GameManager.Instance <> this
            then Object.Destroy(this.gameObject)

        Object.DontDestroyOnLoad(this.gameObject)
        this._enemies <- new List<Enemy>()
        this._boardScript <- this.GetComponent<BoardManager>()

        this.InitGame()

    (*
        this is called only once, and the paramter tell it to be called only after the scene was loaded
        (otherwise, our Scene Load callback would be called the very first load, and we don't want that)
    *)
    [<RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)>]
    static member public CallbackInitialization() =
        //register the callback to be called everytime the scene is loaded
        SceneManager.add_sceneLoaded(new UnityAction<Scene, LoadSceneMode>(fun arg0 arg1 -> GameManager.OnSceneLoaded(arg0, arg1)))

    //This is called each time a scene is loaded.
    static member private OnSceneLoaded(arg0 : Scene, arg1 : LoadSceneMode) =
        GameManager.Instance._level <- GameManager.Instance._level + 1
        GameManager.Instance.InitGame()

    member private this.InitGame() =
        this._doingSetup <- true
        this._levelImage <- GameObject.Find("LevelImage")
        this._levelText <- GameObject.Find("LevelText").GetComponent<Text>()
        this._levelText.text <- "Day " + this._level.ToString()
        this._levelImage.SetActive(true)

        this.Invoke("HideLevelImage", this._levelStartDelay)
        this._enemies.Clear()
        this._boardScript.SetupScene(this._level)

    member private this.HideLevelImage() =
        this._levelImage.SetActive(false)
        this._doingSetup <- false

    member public this.GameOver() =
        this._levelText.text <- "After " + this._level.ToString() + " days, you starved."
        this._levelImage.SetActive(true)
        this.enabled <- false


    member private this.Update() =
        if this._playersTurn || this._enemiesMoving || this._doingSetup
            then ()
        else this.StartCoroutine(this.MoveEnemies()) |> ignore

    member public this.AddEnemyToList(script : Enemy) =
        this._enemies.Add(script)

    member private this.MoveEnemies() : IEnumerator =
        let a = seq{
            this._enemiesMoving <- true

            yield new WaitForSeconds(this._turnDelay)

            if this._enemies.Count = 0
                then yield new WaitForSeconds(this._turnDelay)

            for enemy in this._enemies do
                enemy.MoveEnemy()
                yield new WaitForSeconds(enemy._moveTime);

            this._playersTurn <- true
            this._enemiesMoving <- false
        }

        a.GetEnumerator() :> IEnumerator

and Enemy = 
    inherit MovingObject

    [<DefaultValue>] val mutable public _playerDamage : int
    [<DefaultValue>] val mutable public _attackSound1 : AudioClip
    [<DefaultValue>] val mutable public _attackSound2 : AudioClip

    [<DefaultValue>] val mutable private _animator : Animator
    [<DefaultValue>] val mutable private _target : Transform
    [<DefaultValue>] val mutable private _skipMove : bool

    member private this.Awake() =
        this._animator <- this.GetComponent<Animator>()

    override this.Start() =
        base.Start()
        GameManager.Instance.AddEnemyToList(this)
        this._target <- GameObject.FindGameObjectWithTag("Player").transform

    override this.AttemptMove<'a when 'a :> Component and 'a : equality and 'a : null>(xDir : int, yDir : int) =
        if this._skipMove
            then
                this._skipMove <- false
            else
                base.AttemptMove<'a>(xDir, yDir)
                this._skipMove <- true

    member public this.MoveEnemy() =
        let mutable xDir = 0
        let mutable yDir = 0

        if Mathf.Abs(this._target.position.x - this.transform.position.x) < System.Single.Epsilon
        then 
            match this._target.position.y > this.transform.position.y with
            | true -> yDir <- 1
            | false -> yDir <- -1
       
        else
            match this._target.position.x > this.transform.position.x with
            | true -> xDir <- 1
            | false -> xDir <- -1

        this.AttemptMove<Player>(xDir, yDir)

    override this.OnCantMove(c : Component) =
        let hitPlayer = c :?> Player

        this._animator.SetTrigger("enemyAttack");

        hitPlayer.LoseFood(this._playerDamage)

        SoundManager.Instance.RandomizeSfx(this._attackSound1, this._attackSound2)

and [<AllowNullLiteral>] Player =
    inherit MovingObject

    val mutable public _wallDamage : int
    val mutable public _pointsPerFood : int
    val mutable public _pointsPerSoda : int
    val mutable public _restartLevelDelay : float32

    [<DefaultValue>] val mutable public _foodText : Text

    [<DefaultValue>] val mutable public _moveSound1 : AudioClip
    [<DefaultValue>] val mutable public _moveSound2 : AudioClip
    [<DefaultValue>] val mutable public _eatSound1 : AudioClip
    [<DefaultValue>] val mutable public _eatSound2 : AudioClip
    [<DefaultValue>] val mutable public _drinkSound1 : AudioClip
    [<DefaultValue>] val mutable public _drinkSound2 : AudioClip
    [<DefaultValue>] val mutable public _gameOverSound : AudioClip

    [<DefaultValue>] val mutable private _animator : Animator
    [<DefaultValue>] val mutable private _food : int
    val mutable private _touchOrigin : Vector2

    new () =
        {
            _wallDamage = 1
            _pointsPerFood = 10
            _pointsPerSoda = 20
            _restartLevelDelay = 1.0f
            _touchOrigin = -Vector2.one
        }

    override this.Start() =
        base.Start()

        this._animator <- this.GetComponent<Animator>()

        this._food <- GameManager.Instance._playerFoodPoints

        this._foodText.text <- "Food: " + this._food.ToString()

    member private this.Update() =
        if GameManager.Instance._playersTurn = false
            then 
                ()
            else 
                let mutable horizontal = 0
                let mutable vertical = 0

//#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
                horizontal <- int <| Input.GetAxisRaw("Horizontal")
                vertical <- int <| Input.GetAxisRaw("Vertical")

                if horizontal <> 0
                    then vertical <- 0
//#else
//        if (Input.touchCount > 0)
//        {
//            Touch myTouch = Input.touches[0];

//            if (myTouch.phase == TouchPhase.Began)
//            {
//                touchOrigin = myTouch.position;
//            }

//            else if (myTouch.phase == TouchPhase.Ended && touchOrigin.x >= 0)
//            {
//                Vector2 touchEnd = myTouch.position;

//                float x = touchEnd.x - touchOrigin.x;
//                float y = touchEnd.y - touchOrigin.y;
//                touchOrigin.x = -1;

//                if (Mathf.Abs(x) > Mathf.Abs(y))
//                {
//                    horizontal = x > 0 ? 1 : -1;
//                }
//                else
//                {
//                    vertical = y > 0 ? 1 : -1;
//                }


//            }
//        }
//#endif    

                if horizontal <> 0 || vertical <> 0
                    then this.AttemptMove<Wall>(horizontal, vertical)

    member public this.LoseFood(loss : int) =
        this._animator.SetTrigger("playerHit")

        this._food <- this._food - loss

        this._foodText.text <- "-" + loss.ToString() + " Food: " + this._food.ToString()

        this.CheckIfGameOver()

    member private this.OnTriggerEnter2D(other : Collider2D) =
        if other.tag = "Exit"
            then
                this.Invoke("Restart", this._restartLevelDelay)
                this.enabled <- false
        else if other.tag = "Food"
            then 
                this._food <- this._food + this._pointsPerFood
                this._foodText.text <- "+" + this._pointsPerFood.ToString() + " Food: " + this._food.ToString()
                SoundManager.Instance.RandomizeSfx(this._eatSound1, this._eatSound2)
                other.gameObject.SetActive(false)
        else if other.tag = "Soda"
            then 
                this._food <- this._food + this._pointsPerSoda
                this._foodText.text <- "+" + this._pointsPerSoda.ToString() + " Food: " + this._food.ToString()
                SoundManager.Instance.RandomizeSfx(this._drinkSound1, this._drinkSound2)
                other.gameObject.SetActive(false)

    member private this.OnDisable() =
        GameManager.Instance._playerFoodPoints <- this._food

    member private this.CheckIfGameOver() =
        if this._food < 1
            then
                SoundManager.Instance.PlaySingl(this._gameOverSound)
                SoundManager.Instance._musicSource.Stop()
                GameManager.Instance.GameOver()

    override this.AttemptMove<'a when 'a :> Component and 'a : equality and 'a : null>(xDir : int, yDir : int) =
        this._food <- this._food - 1

        this._foodText.text <- "Food: " + this._food.ToString()

        base.AttemptMove<'a>(xDir, yDir)

        let hit = this.Move(xDir, yDir)

        if (box hit <> null)
            then SoundManager.Instance.RandomizeSfx(this._moveSound1, this._moveSound2)

        this.CheckIfGameOver();

        GameManager.Instance._playersTurn <- false

    override this.OnCantMove(c : Component) =     
        let hitWall = c :?> Wall

        hitWall.DamageWall(this._wallDamage)

        this._animator.SetTrigger("playerChop")

    member private this.Restart() =
        SceneManager.LoadScene(0)
