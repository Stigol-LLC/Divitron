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

	private int indexSlide = 0;

	bool slideInCurrentTouch = false;
	// Use this for initialization
	void Start () {
		Application.targetFrameRate = 60;
		TouchProcessor.Instance.AddListener(this,-1);
		ViewManager.Active.GetViewById("GameOver").SetDelegate( "Restart", Restart );
		ViewManager.Active.GetViewById("Game").SetDelegate("ShowPlayer",ShowPlayer);

		count_label = (Label)ViewManager.Active.GetViewById("Game").GetChildById("count");
		ViewManager.Active.GetViewById("ViewStart").SetDelegate("Start",StartGame);
		ViewManager.Active.GetViewById("ViewStart").SetDelegate(UIEditor.ID.DefineActionName.BTN_MUSIC.ToString(),ChangeMusic);
		_player.SetActionGameOver(GameOver);
		moveBackground.Pause = false;
		_playerAnimator = _player.GetComponent<Animator>();
		ViewManager.Active.GetViewById("ViewSpalshScreen").IsVisible = false;
		ViewManager.Active.GetViewById("ViewStart").IsVisible = true;

	}
	
	// Update is called once per frame
	void Update () {
		if(moveBarrier.CurrentMoveObject().CountGen != lastCountGen){
			Count++;
			count_label.MTextMesh.text = Count.ToString();
			lastCountGen = moveBarrier.CurrentMoveObject().CountGen;
			_playerAnimator.speed = Mathf.Abs(moveBarrier.CurrentMoveObject().speed.x)*animationSpeedKoef;
			moveBarrier.CurrentMoveObject().speed.x *= speedUpTimeMult;
			moveBarrier.CurrentMoveObject().speed.x += speedUpTimeAdd;
		}
	}
	void OnDestroy(){
		Debug.Log("Destroy");
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
		yield return new WaitForSeconds(1f);
		ViewManager.Active.GetViewById("GameOver").IsVisible = true;
		if(musicPlay){
			musicMenu.enabled = true;
			musicGame.enabled = false;
		}
		moveBarrier.Reset();
		_player.GetComponent<VisualNode>().IsVisible = false;
		_player.Reset();
	}
	void GameOver(){
		//TouchProcessor.Instance.RemoveListener(this);
		moveBackground.Pause = true;
		moveBarrier.CurrentMoveObject().Pause = true;
		_player.Pause = true;
		Camera.main.animation.Play();
		StartCoroutine("ShowGameOverView");
	}
	void ChangeMusic(ICall bb){
		musicPlay = !musicPlay;
		musicMenu.enabled = musicPlay;
	}
	void StartGame(ICall bb){

		ViewManager.Active.GetViewById("ViewStart").IsVisible = false;
		ViewManager.Active.GetViewById("Game").IsVisible = true;

		PlayGame();
	}
	void PlayGame(){
		moveBarrier.CurrentMoveObject().Pause = false;
		if(musicPlay){
			musicMenu.enabled = false;
			musicGame.enabled = true;
		}
		_player.GetComponent<VisualNode>().IsVisible = true;
		_playerAnimator.Play("HeroCome0");
	}
	void Restart(ICall bb){
		ViewManager.Active.GetViewById("GameOver").IsVisible = false;
		PlayGame();
		_player.Pause = false;
	}
	void ShowPlayer(ICall bb){
		int num = int.Parse(bb.ActionValue);
		ShowPlayer(num);
	}
	void ShowPlayer(int num){
		string playState = "Divide" + currentShow.ToString() + "_" + num.ToString();
		_playerAnimator.Play(playState);
		currentShow = num;
	}

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
		touchBegin = touchPoint;
		_player.Up();
		return true;
	}
	public bool TouchMove(Vector2 touchPoint){
		if(slideInCurrentTouch)
			return false;
		float length = touchBegin.x - touchPoint.x;

		if(length > lenghtMoveTouch){
			indexSlide--;
			if(indexSlide < 0){
				indexSlide = (allowCircleSlide)?arraySlideObject.Length - 1:0;
			}
			slideInCurrentTouch = true;
			ShowPlayer(arraySlideObject[indexSlide]);
		}else if(length < -lenghtMoveTouch){
			indexSlide++;
			if(indexSlide >= arraySlideObject.Length){
				indexSlide = (allowCircleSlide)?0:arraySlideObject.Length - 1;
			}
			slideInCurrentTouch = true;
			ShowPlayer(arraySlideObject[indexSlide]);
		}

		return false;
	}
	public void TouchEnd(Vector2 touchPoint){
		slideInCurrentTouch = false;
		return;
	}
	public void TouchCancel(Vector2 touchPoint){
	}
	#endregion
}
