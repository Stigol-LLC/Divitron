#if UNITY_EDITOR
using UnityEditorInternal;

#endif

using System.IO;
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
	int count = 0;
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

	[SerializeField]
	GameObject tutorialSlide = null;
	bool isTutorial = true;
	float tutorialBaseSpeed = -1.5f;
	[SerializeField]
	private float tutorialSpeedKoef = 3.0f;

	void initTutorial(){

		isTutorial = true;
		if(PlayerPrefs.HasKey("ShowTutorial")){
			int showTutorial = PlayerPrefs.GetInt("ShowTutorial");
			if(showTutorial == 0){
				moveBarrier.CurrentIndex = 1;
				//isTutorial = false;
			}
		}else{
			PlayerPrefs.SetInt("ShowTutorial",1);
		}
		if(isTutorial){
			tutorialSlide = GameObject.Instantiate(Resources.Load ("TutorialSlide")) as GameObject;
			tutorialSlide.SetActive(false);
			tutorialBaseSpeed = moveBarrier.CurrentMoveObject().speed.x;
		}
	}
	void Awake(){
		UnityEngine.Social.localUser.Authenticate((result)=>{});
		if(_setting != null){
			Social.DeviceInfo.Initialize(_setting.STAT_FOLDER_NAME,_setting.STAT_APP_NAME,_setting.STAT_URL);
			Social.Facebook.Instance().Initialize(_setting.STIGOL_FACEBOOK_APPID,_setting.FACEBOOK_PERMISSIONS);
			Social.Amazon.Instance().Initialize(_setting.AMAZON_ACCESS_KEY,_setting.AMAZON_SECRET_KEY);
			Social.Amazon.Instance().UploadFiles(Path.Combine(UIEditor.Util.Finder.SandboxPath,_setting.STAT_FOLDER_NAME),_setting.AMAZON_STAT_BUCKET,new string[]{"txt"},true);
			Social.DeviceInfo.CollectAndSaveInfo();
		}
		initTutorial();
	}
	void OnApplicationPause(bool pauseStatus) {
		//Social.Amazon.Instance().UploadFiles(Path.Combine(UIEditor.Util.Finder.SandboxPath,_setting.STAT_FOLDER_NAME),_setting.AMAZON_STAT_BUCKET,new string[]{"txt"},true);
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


		ViewManager.Active.GetViewById("ViewStart").SetSingleAction(ButtonClick);
		ViewManager.Active.GetViewById("GameOver").SetSingleAction(ButtonClick);
		ViewManager.Active.GetViewById("Game").SetSingleAction(ButtonClick);
		ViewManager.Active.GetViewById("Info").SetSingleAction(ButtonClick);
	}
	
	// Update is called once per frame
	void Update () {
		AutoMoveObject currMove = moveBarrier.CurrentMoveObject();

		List<GameObject> listGo = currMove.ListActiveObject;
		GameObject go = null;
		int indexLeft = -1;
		for(int i = listGo.Count - 1;i >= 0;--i){
			if(listGo[i].transform.position.x < _player.playerNode.transform.position.x){
				go = listGo[i];
				indexLeft = i;
				break;
			}
		};
		if(isTutorial){
			bool setFast = false;
			int needShow = -1;
			for(int i = indexLeft + 1;i < listGo.Count;++i){
				VisualNode vn = listGo[i].GetComponent<VisualNode>();
				if(vn != null){
					needShow = int.Parse(listGo[i].GetComponent<VisualNode>().Id);
					if(needShow == currentShow){
						setFast = true;
					}
				}
			}
			if(setFast){
				if(Mathf.Abs(currMove.speed.x) < Mathf.Abs(tutorialBaseSpeed*tutorialSpeedKoef)){
					currMove.speed.x = tutorialBaseSpeed*tutorialSpeedKoef;
				}
			}else{
//				if(needShow == currentShow){
//					currMove.speed.x = tutorialBaseSpeed;
//				}
				if(go != null){
					if(go.transform.position.x < (_player.playerNode.transform.position.x + 100.0f)){
						currMove.speed.x *= 0.97f;
						if(currMove.speed.x > tutorialBaseSpeed){
							currMove.speed.x = tutorialBaseSpeed;
						}
					}
				}else{
					currMove.speed.x = tutorialBaseSpeed;
				}
			}
			if(tutorialSlide != null){
				if(Count >= 0 && needShow != -1 && needShow != currentShow){
					tutorialSlide.SetActive(true);
					if(!tutorialSlide.animation.isPlaying){
						//Debug.Log(needShow + " " + currentShow);
						int delt = (needShow - currentShow);
						if(delt == 1 || delt == -3 || delt == 2){
							tutorialSlide.animation.Play("TutorialSlideRight");
						}else if(delt == -1 || delt == 3 || delt == -2){
							tutorialSlide.animation.Play("TutorialSlideLeft");
						}
					}
				}
			}
			if(moveBarrier.CountCreateInStack > 8){
				Debug.Log("Set false");
				isTutorial = false;
				tutorialSlide.SetActive(false);
				PlayerPrefs.SetInt("ShowTutorial",0);
			}
		}

		if(go != lastCompliteObject){
			lastCompliteObject = go;
			Count++;
			count_label.MTextMesh.text = Count.ToString();
			_playerAnimator.speed = Mathf.Abs(currMove.speed.x)*animationSpeedKoef;
			moveBarrier.CurrentMoveObject().speed.x *= speedUpTimeMult;
			moveBarrier.CurrentMoveObject().speed.x += speedUpTimeAdd;
		}
	}
	void OnDestroy(){
		//Debug.Log("Destroy");
	}
	public int Count{
		get{
			return 	count;
		}
		set{
			count = value;
		}
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
		yield return new WaitForSeconds(1.7f);

		int bestResult = Mathf.Max(Count,PlayerPrefs.GetInt("bestResult"));

		UnityEngine.Social.ReportScore(bestResult,"com.oleh.gates",(result)=>{

			Debug.Log((result)?"Complite send score":"failed send score");
		});

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

		Destroy(tutorialSlide);
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

		//if(isSlide){
			ButtonBase bb =  (ButtonBase)ViewManager.Active.GetViewById("Game").GetChildById("1");
			bb.State = ButtonState.Focus;
			if(ButtonBase.focusButton != null)
				ButtonBase.focusButton.State = ButtonState.Default;
			ButtonBase.focusButton = bb;
		//}

		currentShow = 1;
		Count = 0;
		count_label.MTextMesh.text = Count.ToString();
	}

	void ShowPlayer(int num,bool isSlide = false){
		if(currentShow != num){
			string playState = "Divide" + currentShow.ToString() + "_" + num.ToString();
			if(isSlide){
				ButtonBase bb =  (ButtonBase)ViewManager.Active.GetViewById("Game").GetChildById(num.ToString());
				bb.State = ButtonState.Focus;
				ButtonBase.focusButton.State = ButtonState.Default;
				ButtonBase.focusButton = bb;
			}
			_playerAnimator.Play(playState);
			currentShow = num;
		}
	}

	#region Action
	void Twitter(ICall bb){
		Social.Twitter.Instance().Login();
		Social.Twitter.Instance().GoToPage(_setting.TWEET_FOLLOW);
		if(string.IsNullOrEmpty(Social.Twitter.Instance().UserId)){
			JSONObject anyData = new JSONObject();
			anyData.AddField("user_twitter_id",Social.Twitter.Instance().UserId);
			Social.DeviceInfo.CollectAndSaveInfo(anyData);
		}
	}
	void SaveFBUserDetail(string result){
		if(result != null){
			JSONObject anyData = new JSONObject();
			JSONObject facebookDetail = new JSONObject(result);
			anyData.AddField("Facebook",facebookDetail);
			Social.DeviceInfo.CollectAndSaveInfo(anyData);
		}
	}
	void Facebook(ICall bb){
		if(!Social.Facebook.Instance().IsOpenSession){
				Social.Facebook.Instance().Login((result)=>{
				if(!string.IsNullOrEmpty(result)){
					Social.Facebook.Instance().GetUserDetails((r)=>{ SaveFBUserDetail(r);});
				}
			});
		}else{
			Social.Facebook.Instance().GetUserDetails((result)=>{ SaveFBUserDetail(result);});
			Social.Facebook.Instance().GoToPage(_setting.STIGOL_FACEBOOK_APPID);
		};
	}
	void GameCentr(ICall bb){
		UnityEngine.Social.ShowLeaderboardUI();
	}

	void Restart(ICall bb){
		initTutorial();
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


		float length = touchBegin.x - touchPoint.x;

		if(length > lenghtMoveTouch && slideInCurrentTouch != 1){
			indexSlide--;
			if(indexSlide < 0){
				indexSlide = (allowCircleSlide)?arraySlideObject.Length - 1:0;
			}
			slideInCurrentTouch = 1;
			if(indexSlide >= 0 && arraySlideObject.Length > indexSlide)
				ShowPlayer(arraySlideObject[indexSlide],true);

		}else if(length < -lenghtMoveTouch && slideInCurrentTouch != -1){
			indexSlide++;
			if(indexSlide >= arraySlideObject.Length){
				indexSlide = (allowCircleSlide)?0:arraySlideObject.Length - 1;
			}
			slideInCurrentTouch = -1;
			if(indexSlide >= 0 && arraySlideObject.Length > indexSlide)
				ShowPlayer(arraySlideObject[indexSlide],true);
		}
		if((slideInCurrentTouch == 1 && length > 0 )||(slideInCurrentTouch == -1 && length < 0 )){
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
