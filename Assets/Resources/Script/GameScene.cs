#if UNITY_EDITOR
using UnityEditorInternal;
#endif

using UnityEngine;
using System.Collections.Generic;
using UIEditor.Core;
using UIEditor.Node;
using System.Collections;

public class GameScene : MonoBehaviour,UIEditor.Node.ITouchable {
	[SerializeField]
	StackMoveObject moveBarrier = null;
	[SerializeField]
	AutoMoveObject moveBackground = null;
	[SerializeField]
	Player _player = null;

	Label count_label = null;
	int Count = 0;
	int lastCountGen = 0;
	int currentShow = 1;
	[SerializeField]
	AudioSource musicMenu = null;
	[SerializeField]
	AudioSource musicGame = null;
	[SerializeField]
	AudioSource soundDestroy = null;

	[SerializeField]
	AudioSource soundButtonClick = null;

	[SerializeField]
	float lenghtMoveTouch = 10.0f;
	Vector2 touchBegin = Vector2.zero;

	private Animator _playerAnimator = null;
	[SerializeField]
	private float animationSpeedKoef  = 1.0f;
	[SerializeField]
	private float speedUpTimeMult = 1.0f;
	[SerializeField]
	private float speedUpTimeAdd = 0.0f;

	[SerializeField]
	bool musicPlay = true;
	[SerializeField]
	int[] arraySlideObject = new int[]{1,2,3,4};
	[SerializeField]
	bool allowCircleSlide = true;

	bool touch = false;
	private int indexSlide = 0;

	int slideInCurrentTouch = 0;
	GameObject lastCompliteObject = null;

	[SerializeField]
	SettingProject _setting = null;

	void Awake(){
		UnityEngine.Social.localUser.Authenticate((result)=>{});
	}
	// Use this for initialization
	void Start () {

		musicPlay = (PlayerPrefs.GetInt("music") != 0);
		if(musicPlay){
			ViewManager.Active.GetViewById("ViewStart").GetChildById("musicOff").IsVisible = false;
			ViewManager.Active.GetViewById("ViewStart").GetChildById("musicOn").IsVisible = true;
			musicMenu.Play();
		}else{
			ViewManager.Active.GetViewById("ViewStart").GetChildById("musicOff").IsVisible = true;
			ViewManager.Active.GetViewById("ViewStart").GetChildById("musicOn").IsVisible = false;
		}

		Application.targetFrameRate = 60;
		TouchProcessor.Instance.AddListener(this,-1);
		ViewManager.Active.GetViewById("GameOver").SetDelegate( "Restart", Restart );
		ViewManager.Active.GetViewById("GameOver").SetDelegate( "Home", GoHome );

		ViewManager.Active.GetViewById("GameOver").SetDelegate( "GameCentr", GameCentr );
		ViewManager.Active.GetViewById("GameOver").SetDelegate( "BTN_TWITTER", Twitter );
		ViewManager.Active.GetViewById("GameOver").SetDelegate( "BTN_FACEBOOK", Facebook );

		ViewManager.Active.GetViewById("Game").SetDelegate("ShowPlayer",ShowPlayer);

		count_label = (Label)ViewManager.Active.GetViewById("Game").GetChildById("count");
		ViewManager.Active.GetViewById("ViewStart").SetDelegate("Start",StartGame);
		ViewManager.Active.GetViewById("ViewStart").SetDelegate("GameCentr",GameCentr);


		ViewManager.Active.GetViewById("ViewStart").SetDelegate(UIEditor.ID.DefineActionName.BTN_MUSIC.ToString(),ChangeMusic);

		moveBackground.Pause = false;
		ViewManager.Active.GetViewById("ViewSpalshScreen").IsVisible = false;
		ViewManager.Active.GetViewById("ViewStart").IsVisible = true;
		ViewManager.Active.GetViewById("ViewStart");
		//View v = null;
		//int t = v.getCount;

		ViewManager.Active.GetViewById("ViewStart").SetSingleAction(ButtonClick);
		ViewManager.Active.GetViewById("GameOver").SetSingleAction(ButtonClick);
		ViewManager.Active.GetViewById("Game").SetSingleAction(ButtonClick);
		ViewManager.Active.GetViewById("Info").SetSingleAction(ButtonClick);
	}
	
	// Update is called once per frame
	void Update () {

		List<GameObject> listGo = moveBarrier.CurrentMoveObject().ListActiveObject;
		GameObject go = null;
		for(int i = listGo.Count - 1;i >= 0;--i){
			if(listGo[i].transform.position.x < _player.playerNode.transform.position.x){
				go = listGo[i];
				break;
			}
		};


		if(/*moveBarrier.CurrentMoveObject().CountGen != lastCountGen*/ go != lastCompliteObject){
			lastCompliteObject = go;
			Count++;
			count_label.MTextMesh.text = Count.ToString();
			lastCountGen = moveBarrier.CurrentMoveObject().CountGen;
			_playerAnimator.speed = Mathf.Abs(moveBarrier.CurrentMoveObject().speed.x)*animationSpeedKoef;
			moveBarrier.CurrentMoveObject().speed.x *= speedUpTimeMult;
			moveBarrier.CurrentMoveObject().speed.x += speedUpTimeAdd;
		}
	}
	void OnDestroy(){
		//Debug.Log("Destroy");
	}

	public void SortZorder(){
		List<VisualNode> zOrderList = NodeContainer.SortChildrenList(this.transform);
		Debug.Log(zOrderList.Count);
		int i = 0;
		foreach(VisualNode vn in zOrderList){
			vn.transform.position = new Vector3(Mathf.CeilToInt(vn.transform.position.x),Mathf.CeilToInt(vn.transform.position.y),transform.position.z - i*0.1f);
			i++;
		}
	}

	IEnumerator ShowGameOverView()
	{
		yield return new WaitForSeconds(1.5f);

		int bestResult = Mathf.Max(Count,PlayerPrefs.GetInt("bestResult"));
		PlayerPrefs.SetInt("bestResult",bestResult);
		Debug.Log(PlayerPrefs.GetInt("bestResult").ToString());

		ViewManager.Active.GetViewById("GameOver").IsVisible = true;
		VisualNode group = ViewManager.Active.GetViewById("GameOver").GetChildById("group");

		if(group.GetChildById("result") is Label){
			(group.GetChildById("result") as Label).MTextMesh.text = Count.ToString();
			(group.GetChildById("bestResult") as Label).MTextMesh.text = bestResult.ToString();
		}

		if(musicPlay){
			musicMenu.Play();
			musicGame.Stop();
		}
		moveBarrier.Reset();

		//_player.Reset();
		//_player.GetComponent<VisualNode>().IsVisible = false;
		Destroy(_player.gameObject);
	}
	void GameOver(){
		if(musicPlay && soundDestroy != null){
			soundDestroy.Play();
		}
		moveBackground.Pause = true;
		moveBarrier.CurrentMoveObject().Pause = true;
		_player.Pause = true;
		Camera.main.animation.Play();
		_playerAnimator.Play("Kill3");
		StartCoroutine("ShowGameOverView");
	}

	void PlayGame(){
		GameObject go = GameObject.Instantiate(Resources.Load ("PlayerUnite")) as GameObject;
		_player = go.GetComponent<Player>();
		go.name = "PlayerUnite";
		go.transform.parent = transform;

		_player.SetActionGameOver(GameOver);
		_playerAnimator = _player.GetComponent<Animator>();

		touch = true;
		moveBackground.Pause = false;
		moveBarrier.Reset();
		moveBarrier.CurrentMoveObject().Pause = false;
		if(musicPlay){
			musicMenu.Stop();
			musicGame.Play();

		}
		_player.GetComponent<VisualNode>().IsVisible = true;
		_playerAnimator.Play("HeroCome0");
		_player.Pause = false;
		currentShow = 1;
		Count = 0;
		count_label.MTextMesh.text = Count.ToString();
	}

	void ShowPlayer(int num){
		if(currentShow != num){
			string playState = "Divide" + currentShow.ToString() + "_" + num.ToString();
			//Debug.Log("playState " + playState);
			_playerAnimator.Play(playState);
			currentShow = num;
		}
	}

	#region Action
	void Twitter(ICall bb){
		Social.Twitter.Instance().GoToPage(_setting.TWEET_FOLLOW);
	}
	void Facebook(ICall bb){
		Social.Facebook.Instance().GoToPage(_setting.STIGOL_FACEBOOK_APPID);
	}
	void GameCentr(ICall bb){
		UnityEngine.Social.ShowLeaderboardUI();
	}

	void Restart(ICall bb){
		ViewManager.Active.GetViewById("GameOver").IsVisible = false;
		PlayGame();
		moveBackground.Pause = false;
	}
	void ShowPlayer(ICall bb){
		if(musicPlay && soundButtonClick != null){
			soundButtonClick.Play();
		}
		int num = int.Parse(bb.ActionValue);
		ShowPlayer(num);
	}
	void ChangeMusic(ICall bb){
		musicPlay = !musicPlay;
		if(musicPlay){
			musicMenu.Play();
		}else{
			musicMenu.Stop();
		}
		PlayerPrefs.SetInt("music",(musicPlay)?1:0);
	}
	void GoHome(ICall bb){
		moveBackground.Pause = false;
	}
	void StartGame(ICall bb){
		ViewManager.Active.GetViewById("ViewStart").IsVisible = false;
		ViewManager.Active.GetViewById("Game").IsVisible = true;
		PlayGame();
	}
	void ButtonClick(ICall bb){
		if(musicPlay && soundButtonClick != null){
			soundButtonClick.Play();
		}
		//Debug.Log("ButtonClick");
		//moveBackground.Pause = false;
	}
	#endregion


	#region Touch
	public Rect GetTouchableBound(){
		return new Rect(0,0,0,0);
	}
	public bool IsPointInBound(Vector2 point){
		return true;
	}
	public bool IsTouchable
	{
		set{
		}
		get{
			return true;
		}
	}
	
	public bool TouchBegan(Vector2 touchPoint){
		if(!touch)
			return false;
		touchBegin = touchPoint;
		_player.Up();
		//Debug.Log("TouchBegan");
		return true;
	}
	public bool TouchMove(Vector2 touchPoint){
		if(!touch)
			return false;
		//if(slideInCurrentTouch)
		//	return false;

		float length = touchBegin.x - touchPoint.x;

		if(length > lenghtMoveTouch && slideInCurrentTouch != 1){
			indexSlide--;
			if(indexSlide < 0){
				indexSlide = (allowCircleSlide)?arraySlideObject.Length - 1:0;
			}
			slideInCurrentTouch = 1;
			if(indexSlide >= 0 && arraySlideObject.Length > indexSlide)
				ShowPlayer(arraySlideObject[indexSlide]);
			touchBegin = touchPoint;
		}else if(length < -lenghtMoveTouch && slideInCurrentTouch != -1){
			indexSlide++;
			if(indexSlide >= arraySlideObject.Length){
				indexSlide = (allowCircleSlide)?0:arraySlideObject.Length - 1;
			}
			slideInCurrentTouch = -1;
			if(indexSlide >= 0 && arraySlideObject.Length > indexSlide)
				ShowPlayer(arraySlideObject[indexSlide]);
			touchBegin = touchPoint;
		}

		return false;
	}
	public void TouchEnd(Vector2 touchPoint){
		slideInCurrentTouch = 0;
		return;
	}
	public void TouchCancel(Vector2 touchPoint){
		slideInCurrentTouch = 0;
	}
	#endregion
}
