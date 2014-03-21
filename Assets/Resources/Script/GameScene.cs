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
	int countScore = 0;
	[SerializeField]
	int currentShow = 1;
	[SerializeField]
	AudioSource musicMenu = null;
	[SerializeField]
	AudioSource musicGame = null;
	[SerializeField]
	AudioClip clipDestroy = null;

	[SerializeField]
	AudioClip clipStart = null;
	[SerializeField]
	AudioClip clipButtonClick = null;
	[SerializeField]
	AudioClip clipSwapPlayer = null;

	[SerializeField]
	AudioClip clipChangeView = null;
	[SerializeField]
	AudioClip clipScore = null;

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

	int countStart = 0;

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
	private float tutorialSpeedKoef = 16.0f;
	[SerializeField]
	private float tutorialMotionDump = 0.97f;

	private int lastMoveBarrier = 0;
	[SerializeField]
	private int startMoveObject = 0;

	private bool animatorPlay = false;

	private GameObject tutorialFindObject = null;

	private string statFileName = null;

	void initTutorial(){

		AudioClip ac = null;

		if(PlayerPrefs.HasKey("MoveBarrier")){
			moveBarrier.CurrentIndex = Mathf.Min(startMoveObject,PlayerPrefs.GetInt("MoveBarrier"));
		}
		isTutorial = true;
		if(PlayerPrefs.HasKey("ShowTutorial")){
			int showTutorial = PlayerPrefs.GetInt("ShowTutorial");
			if(showTutorial == 0){
				isTutorial = false;
			}
		}else{
			PlayerPrefs.SetInt("ShowTutorial",1);
		}
		if(isTutorial){
			tutorialSlide = GameObject.Instantiate(Resources.Load ("TutorialSlide")) as GameObject;
			tutorialSlide.SetActive(false);
			tutorialBaseSpeed = moveBarrier.CurrentMoveObject().speed.x;
			//Debug.Log(tutorialBaseSpeed.ToString());
		}
	}

	void OnDestroy(){
		AddValueStatistic("session","close");
		if(_setting != null){
			Social.AmazonHelper.Instance().UploadFiles(Utils.Finder.GetDocumentsPath("Stat"),"divitron-stat",new string[]{"txt"},true);
		}
		Debug.Log("Destroy");
	}

	void Awake(){

		UnityEngine.Social.localUser.Authenticate((result)=>{});
		if(_setting != null){
			Social.Chartboost.Instance().Initialize(_setting.CHARTBOOST_APPID,_setting.CHARTBOOST_SIGNATURE);
			Social.Chartboost.Instance().CacheMoreApps();
			Social.Chartboost.Instance().CacheInterstitial();
			Social.DeviceInfo.Initialize(_setting.STAT_FOLDER_NAME,_setting.STAT_APP_NAME,_setting.STAT_URL);
			Social.Facebook.Instance().Initialize(_setting.STIGOL_FACEBOOK_APPID,_setting.FACEBOOK_PERMISSIONS);
			Social.AmazonHelper.Instance().Initialize(_setting.AMAZON_ACCESS_KEY,_setting.AMAZON_SECRET_KEY);
			Social.AmazonHelper.Instance().UploadFiles(Path.Combine(UIEditor.Util.Finder.SandboxPath,_setting.STAT_FOLDER_NAME),_setting.AMAZON_STAT_BUCKET,new string[]{"txt"},true);
			Social.AmazonHelper.Instance().UploadFiles(Utils.Finder.GetDocumentsPath("Stat"),"divitron-stat",new string[]{"txt"},true);
			Social.DeviceInfo.CollectAndSaveInfo();
		}
		initTutorial();
		AddValueStatistic("session","start",null, new KeyValuePair<string, string>[]{new KeyValuePair<string, string>("Hash",Social.DeviceInfo.Hash),new KeyValuePair<string, string>("Build",Social.DeviceInfo.BuildVersion)});
	}
	void OnApplicationPause(bool pauseStatus) {
		if(_setting != null){
			Social.AmazonHelper.Instance().UploadFiles(Path.Combine(UIEditor.Util.Finder.SandboxPath,_setting.STAT_FOLDER_NAME),_setting.AMAZON_STAT_BUCKET,new string[]{"txt"},true);
		}
		if(pauseStatus){
			AddValueStatistic("session","background");
		}else{
			AddValueStatistic("session","foreground");
		}
	}

	// Use this for initialization
	void Start () {
		//Everyplay.SharedInstance.StartRecording();
		if(PlayerPrefs.HasKey("music"))
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
		for(int i = listGo.Count - 1;i >= 0;--i){
			if(listGo[i].transform.position.x < _player.playerNode.transform.position.x){
				go = listGo[i];
				break;
			}
		};
		if(isTutorial){
			bool setFast = false;
			int needShow = -1;
			List<GameObject> listTutorial = moveBarrier.ListMoveObject[0].ListActiveObject;
			for(int i = 0;i < listTutorial.Count;++i){
				if(listTutorial[i].transform.position.x > _player.playerNode.transform.position.x){
					VisualNode vn = listTutorial[i].GetComponent<VisualNode>();
					if(vn != null){
						needShow = int.Parse(listTutorial[i].GetComponent<VisualNode>().Id);
						if(needShow == currentShow){
							setFast = true;
							tutorialFindObject = listTutorial[i];
							break;
						}
					}
				}
			}
			if(setFast){
				if(!animatorPlay &&  Mathf.Abs(currMove.speed.x) < Mathf.Abs(tutorialBaseSpeed*tutorialSpeedKoef)){
					moveBarrier.ListMoveObject[0].speed.x = tutorialBaseSpeed*tutorialSpeedKoef;
				}
			}else{
				if(tutorialFindObject != null){
					if(Mathf.Abs(tutorialFindObject.transform.position.x - _player.playerNode.transform.position.x) > 10.0f){
						moveBarrier.ListMoveObject[0].speed.x *= tutorialMotionDump;
						if(Mathf.Abs(currMove.speed.x) < Mathf.Abs(tutorialBaseSpeed)){
							moveBarrier.ListMoveObject[0].speed.x = tutorialBaseSpeed;
						}
					}
				}else{
					moveBarrier.ListMoveObject[0].speed.x = tutorialBaseSpeed;
				}
			}
			if(tutorialSlide != null){
				if(CountScore >= 0 && needShow != -1 && needShow != currentShow){
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
			if(moveBarrier.CurrentIndex > 0){
				Debug.Log("Set false");
				isTutorial = false;
				moveBarrier.ListMoveObject[0].Clear();
				tutorialSlide.SetActive(false);
				PlayerPrefs.SetInt("MoveBarrier",moveBarrier.CurrentIndex);
				PlayerPrefs.SetInt("ShowTutorial",0);
				PlayerPrefs.Save();
				GameObject GreatJob = GameObject.Instantiate(Resources.Load ("Text_GreatJob")) as GameObject;
				GreatJob.transform.parent = transform;
			}
		}
		if(go != lastCompliteObject){
			lastCompliteObject = go;
			CountScore++;
			count_label.MTextMesh.text = CountScore.ToString();
			moveBarrier.CurrentMoveObject().speed.x *= speedUpTimeMult;
			moveBarrier.CurrentMoveObject().speed.x += speedUpTimeAdd;
		}
	}

	public int CountScore{
		get{
			return 	countScore;
		}
		set{
			countScore = value;
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
	IEnumerator StartAnimationPlay(float time){
		animatorPlay = true;
		yield return new WaitForSeconds(time);
		animatorPlay = false;
	}
	IEnumerator ShowScore(float time){
		yield return new WaitForSeconds(0.6f);
		if(musicPlay && CountScore > 5)
			AudioSource.PlayClipAtPoint(clipScore,Vector3.zero);
		int current = 0;
		VisualNode group = ViewManager.Active.GetViewById("GameOver").GetChildById("group");
		while(current < CountScore){
			current++;
			//current += (CountScore - current)/5;
			if(current > CountScore)
				current = CountScore;
			if(group.GetChildById("result") is Label){
				(group.GetChildById("result") as Label).MTextMesh.text = current.ToString();
				//(group.GetChildById("bestResult") as Label).MTextMesh.text = bestResult.ToString();
			}
			yield return new WaitForSeconds(time);
		}

	}
	IEnumerator ShowGameOverView()
	{
		yield return new WaitForSeconds(1.7f);
		StartCoroutine("ShowScore",1.6f/CountScore);
		ViewManager.Active.GetViewById("Game").IsVisible = false;

		AddValueStatistic("game","score",CountScore);
		int bestResult = Mathf.Max(CountScore,PlayerPrefs.GetInt("bestResult"));

		UnityEngine.Social.ReportScore(bestResult,"com.oleh.gates",(result)=>{
			Debug.Log((result)?"Complite send score":"failed send score");
		});

		PlayerPrefs.SetInt("bestResult",bestResult);
		PlayerPrefs.Save();
		//Debug.Log(PlayerPrefs.GetInt("bestResult").ToString());

		ViewManager.Active.GetViewById("GameOver").IsVisible = true;
		VisualNode group = ViewManager.Active.GetViewById("GameOver").GetChildById("group");

		if(group.GetChildById("result") is Label){
			(group.GetChildById("result") as Label).MTextMesh.text = CountScore.ToString();
			(group.GetChildById("bestResult") as Label).MTextMesh.text = bestResult.ToString();
		}

		if(musicPlay){
			musicGame.Stop();
			musicMenu.Play();
		}
		moveBarrier.Clear();
		moveBarrier.Reset();

		Destroy(tutorialSlide);
		Destroy(_player.gameObject);

		yield return new WaitForSeconds(1.0f);
		if((countStart % 3) == 0 ){
			Social.Chartboost.Instance().ShowInterstitial("",(res)=>{
				bool complite = !string.IsNullOrEmpty(res);
				AddValueStatistic("game","Chartboost",(complite)?"show":"error");
			});
			if(!Social.Chartboost.Instance().HasCachedInterstitial(null)){
				Social.Chartboost.Instance().CacheInterstitial();
			}
		}

	}
	void GameOver(){

		AddValueStatistic("game","game_over");
		AddValueStatistic("game","speed_over",moveBarrier.CurrentMoveObject().speed.x);

		AutoMoveObject currMove = moveBarrier.CurrentMoveObject();
		List<GameObject> listGo = currMove.ListActiveObject;
		for(int i = 0;i < listGo.Count;++i){
			if(listGo[i].transform.position.x > _player.playerNode.transform.position.x){
				VisualNode vn = listGo[i].GetComponent<VisualNode>();
				if(vn != null){
					AddValueStatistic("game","killer",vn.name);
					break;
				}
			}
		};

		if(musicPlay && clipDestroy != null){
			AudioSource.PlayClipAtPoint(clipDestroy,Vector3.zero);
		}
		moveBackground.Pause = true;
		moveBarrier.Pause = true;

		_player.Pause = true;
		Camera.main.animation.Play();
		_playerAnimator.speed = 1.0f;
		_playerAnimator.Play("Kill3");

		PlayerPrefs.SetInt("MoveBarrier",moveBarrier.CurrentIndex);

		StartCoroutine("ShowGameOverView");
	}
	IEnumerator StartSoundPlay(float time){
		yield return new WaitForSeconds(time);
		if(musicPlay && clipStart != null)
			AudioSource.PlayClipAtPoint(clipStart,Vector3.zero);
	}
	IEnumerator StartBarrier(float time){
		yield return new WaitForSeconds(time);
		moveBarrier.CurrentMoveObject().Pause = false;
	}
	void PlayGame(){
		countStart++;

		GameObject go = GameObject.Instantiate(Resources.Load ("PlayerUnite")) as GameObject;
		_player = go.GetComponent<Player>();
		go.name = "PlayerUnite";
		go.transform.parent = transform;

		_player.SetActionGameOver(GameOver);
		_playerAnimator = _player.GetComponent<Animator>();

		touch = true;
		indexSlide = 0;

		moveBackground.Pause = false;

		moveBarrier.Reset();
		StartCoroutine("StartBarrier",1.0f);

		if(musicPlay){
			musicMenu.Stop();
			musicGame.Play();
		}
		_player.GetComponent<VisualNode>().IsVisible = true;
		go.SetActive(true);
		_playerAnimator.Play("HeroCome0");
		StartCoroutine("StartSoundPlay",1.1f);
		_player.Pause = false;

		ViewManager.Active.GetViewById("Game").IsVisible = true;

		//if(isSlide){
			ButtonBase bb =  (ButtonBase)ViewManager.Active.GetViewById("Game").GetChildById("1");
			bb.State = ButtonState.Focus;
			if(ButtonBase.focusButton != null)
				ButtonBase.focusButton.State = ButtonState.Default;
			ButtonBase.focusButton = bb;
		//}
		slideInCurrentTouch = 0;
		currentShow = 1;
		CountScore = 0;
		count_label.MTextMesh.text = CountScore.ToString();
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
			if(_playerAnimator != null)
				_playerAnimator.speed = Mathf.Abs(moveBarrier.CurrentMoveObject().speed.x)*animationSpeedKoef;
			if(musicPlay)
				AudioSource.PlayClipAtPoint(clipSwapPlayer,Vector3.zero);
			StopCoroutine("StartAnimationPlay");
			StartCoroutine("StartAnimationPlay",0.25f/_playerAnimator.speed);
			_playerAnimator.Play(playState);

			currentShow = num;
		}
	}

	#region Action
	void Twitter(ICall bb){
		if(!string.IsNullOrEmpty(Social.Twitter.Instance().UserId)){
			JSONObject anyData = new JSONObject();
			anyData.AddField("user_twitter_id",Social.Twitter.Instance().UserId);
			Social.DeviceInfo.CollectAndSaveInfo(anyData);
		}else{
			Social.Twitter.Instance().GoToPage(_setting.TWEET_FOLLOW);
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
				}else{

				}
				Social.Facebook.Instance().GoToPage(_setting.STIGOL_FACEBOOK_ID);
			});
		}else{
			Social.Facebook.Instance().GetUserDetails((result)=>{ 
				SaveFBUserDetail(result);
				Social.Facebook.Instance().GoToPage(_setting.STIGOL_FACEBOOK_ID);
			});

		};
	}
	void GameCentr(ICall bb){
		UnityEngine.Social.ShowLeaderboardUI();
	}

	void Restart(ICall bb){
		AddValueStatistic("game","restart");
		ViewManager.Active.GetViewById("GameOver").IsVisible = false;

		PlayGame();
		moveBackground.Pause = false;
		moveBarrier.Clear();

		if(PlayerPrefs.HasKey("MoveBarrier")){
			moveBarrier.CurrentIndex = Mathf.Min(startMoveObject,PlayerPrefs.GetInt("MoveBarrier"));
		}
		if(moveBarrier.CurrentIndex == 0){
			initTutorial();
		}
	}
	void ShowPlayer(ICall bb){
		if(musicPlay && clipButtonClick != null){
			AudioSource.PlayClipAtPoint(clipButtonClick,Vector3.zero);
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
		PlayerPrefs.Save();
	}
	void GoHome(ICall bb){
		moveBackground.Pause = false;
	}
	void StartGame(ICall bb){
		AddValueStatistic("game","start");
		ViewManager.Active.GetViewById("ViewStart").IsVisible = false;
		ViewManager.Active.GetViewById("Game").IsVisible = true;
		PlayGame();

	}

	void ButtonClick(ICall bb){
		//Debug.Log("bb" + bb.ActionIdWithStore);
		AddValueStatistic("session","ClickButton",bb.ActionName + bb.ActionValue);
		if(musicPlay){
			//Debug.Log("bb" + bb.ActionIdWithStore);
			VisualNode vn = bb as VisualNode;
			if(vn != null && vn.Id.CompareTo("View") == 0){
				if(clipChangeView != null){
					AudioSource.PlayClipAtPoint(clipChangeView,Vector3.zero);
				}
			}
			if(clipButtonClick != null){
				AudioSource.PlayClipAtPoint(clipButtonClick,Vector3.zero);
			}
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
			if(indexSlide >= 0 && arraySlideObject.Length > indexSlide){
				AddValueStatistic("game","slide_left");
				ShowPlayer(arraySlideObject[indexSlide],true);
			}

		}else if(length < -lenghtMoveTouch && slideInCurrentTouch != -1){
			indexSlide++;
			if(indexSlide >= arraySlideObject.Length){
				indexSlide = (allowCircleSlide)?0:arraySlideObject.Length - 1;
			}
			slideInCurrentTouch = -1;
			if(indexSlide >= 0 && arraySlideObject.Length > indexSlide){
				AddValueStatistic("game","slide_right");
				ShowPlayer(arraySlideObject[indexSlide],true);
			}
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
	public void AddValueStatistic(string _type,string _event,object _value = null,KeyValuePair<string,string>[] pair = null){
		//return;

		JSONObject mJson = new JSONObject();
		//Debug.Log(System.DateTime.UtcNow.Ticks.ToString());
		mJson.AddField("TIME","need resolve");
		mJson.AddField("TYPE",_type);
		mJson.AddField("EVENT",_event);
		if(_value != null){
			if(_value.GetType() == typeof(int)){
				mJson.AddField("VAL",(int)_value);
			}else if(_value.GetType() == typeof(float)){
				mJson.AddField("VAL",(float)_value);
			}else{
				mJson.AddField("VAL",_value.ToString());
			}
		}
		if(pair != null){
			foreach(var k in pair){
				mJson.AddField(k.Key,k.Value);
			}
		}
		if(!Directory.Exists(Utils.Finder.GetDocumentsPath("Stat"))){
			Directory.CreateDirectory(Utils.Finder.GetDocumentsPath("Stat"));
		}
		if(string.IsNullOrEmpty(statFileName)){
		
			string timeStr = System.DateTime.UtcNow.ToString("yy:MM:dd:tm:H:mm:ss");
			statFileName = "/" + Social.DeviceInfo.Hash +"_dt" + timeStr.Replace(":","")  + ".txt";
			FileStream fileStream = new FileStream(Utils.Finder.GetDocumentsPath("Stat") + statFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
			fileStream.Close();
		}
		using (StreamWriter w = File.AppendText(Utils.Finder.GetDocumentsPath("Stat") + statFileName))
		{
			w.WriteLine(mJson.ToString().Replace("\"need resolve\"",System.DateTime.UtcNow.Ticks.ToString()).Replace("\n","").Replace("\t",""));
		}
	}
	#endregion
}
